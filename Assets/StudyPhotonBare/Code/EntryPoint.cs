using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using StudyPhotonBare.Game;
using StudyPhotonBare.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace StudyPhotonBare
{

public class EntryPoint : MonoBehaviour
{
	public static EntryPoint Instance { get; private set; }
	public static readonly byte[] Token = System.Guid.NewGuid().ToByteArray();

	[Header("Systems")]
	[SerializeField] AvatarsManagerNB _avatarManagerNBPrefab; // @note network behaviours are destroyed by default, but can be pooled
	[SerializeField] NetworkRunner _networkRunnerPrefab; // @note network runner should not be reused

	[Header("Visuals (external)")]
	[SerializeField] Button _networkButton;
	[SerializeField] TMP_Text _networkText;

	[Header("Private")]
	private NetworkRunner _networkRunner;

	[Header("Accessors")]
	private GameCursor Cursor => GameCursor.Instance;

	void Awake()
	{
		Instance = this;
		_networkButton.onClick.AddListener(NetworkToggle);
		_networkButton.gameObject.SetActive(true);
		_networkText.text = "network";

		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;
	}

	void OnDestroy()
	{
		_networkButton.onClick.RemoveListener(NetworkToggle);
		if (Instance == this)
			Instance = null;
	}

	void Update()
	{
		var keyboard = Keyboard.current;
		if (_networkRunner && keyboard.escapeKey.wasPressedThisFrame) {
			SetMenuVisible(!IsMenuVisible);
		}
	}

	private void NetworkToggle()
	{
		var ct = destroyCancellationToken;
		NetworkToggleAsync().Forget();
		async UniTaskVoid NetworkToggleAsync()
		{
			_networkButton.interactable = false;
			_networkText.text = "...";

			if (_networkRunner)
			{
				await _networkRunner.Shutdown();
				ct.ThrowIfCancellationRequested();
				_networkRunner = null;
			}
			else
			{
				var activeScene = SceneManager.GetActiveScene();
				var instance = CreateRunner();
				var sceneManager = instance.GetComponent<INetworkSceneManager>();
				_ = Instantiate(_avatarManagerNBPrefab);
				var result = await instance.StartGame(new StartGameArgs {
					GameMode = GameMode.AutoHostOrClient, ConnectionToken = Token,
					SceneManager = sceneManager, Scene = SceneRef.FromIndex(activeScene.buildIndex),
					OnGameStarted = runner => { _networkRunner = runner; },
				});

				if (result.Ok) await NetworkFinalize(ct);
			}

			var nextMenuState = !_networkRunner;
			SetMenuVisible(nextMenuState);

			_networkText.text = _networkRunner ? "shutdown" : "start";
			_networkButton.interactable = true;
		}
	}

	private void NetworkMigrate(NetworkRunner prevRunner, HostMigrationToken hostMigrationToken)
	{
		// @note see "network project config -> host migration update delay" for the granularity
		// of synchronizations. it is expected to lose some progress on an uncontrolled migration
		var ct = destroyCancellationToken;
		NetworkMigrateAsync().Forget();
		async UniTaskVoid NetworkMigrateAsync()
		{
			await prevRunner.PushHostMigrationSnapshot(); // @note experiments showed it will likely fail
			await prevRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

			var activeScene = SceneManager.GetActiveScene();
			var instance = CreateRunner();
			var sceneManager = instance.GetComponent<INetworkSceneManager>();
			var manager = Instantiate(_avatarManagerNBPrefab);
			var result = await instance.StartGame(new StartGameArgs {
				HostMigrationToken = hostMigrationToken, ConnectionToken = Token,
				SceneManager = sceneManager, Scene = SceneRef.FromIndex(activeScene.buildIndex),
				OnGameStarted = runner => { _networkRunner = runner; },
				HostMigrationResume = manager.NetworkResume,
			});

			if (result.Ok)
			{
				await instance.PushHostMigrationSnapshot(); // @note experiments showed it will likely fail
				await NetworkFinalize(ct);
			}
		}
	}

	public bool IsMenuVisible => _networkButton.gameObject.activeSelf;
	private void SetMenuVisible(bool state)
	{
		_networkButton.gameObject.SetActive(state);
		Cursor.SetConfined(!state);
	}

	private NetworkRunner CreateRunner()
	{
		var instance = Instantiate(_networkRunnerPrefab);
		instance.ProvideInput = true;
		
		if (instance.gameObject.GetComponent<INetworkRunnerCallbacks>() == null)
			instance.gameObject.AddComponent<NetworkEvents>();
		
		if (instance.gameObject.GetComponent<INetworkSceneManager>() == null)
			instance.gameObject.AddComponent<NetworkSceneManagerDefault>();

		return instance;
	}

	private async UniTask NetworkFinalize(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (!_networkRunner) await UniTask.WaitUntil(() => _networkRunner, cancellationToken: ct);
		var events = _networkRunner.GetComponent<NetworkEvents>();
		events.OnHostMigration.AddListener(NetworkMigrate);
	}
}

}
