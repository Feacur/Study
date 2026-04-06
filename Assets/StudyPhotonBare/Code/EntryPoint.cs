using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntryPoint : MonoBehaviour
{
	public static EntryPoint Instance { get; private set; }
	public static readonly byte[] Token = System.Guid.NewGuid().ToByteArray();

	[Header("Systems")]
	[SerializeField] AvatarManagerNB _avatarManagerPrefab; // @note network behaviours are destroyed by default, but can be pooled
	[SerializeField] NetworkRunner _networkRunnerPrefab; // @note network runner should not be reused

	[Header("Visuals")]
	[SerializeField] Button _networkButton;
	[SerializeField] TMP_Text _networkText;

	[Header("Private")]
	private INetworkSceneManager _sceneManager;
	private NetworkRunner _networkRunner;

	void Awake()
	{
		Instance = this;
		_networkButton.onClick.AddListener(NetworkToggle);
		_networkButton.gameObject.SetActive(true);
		_networkText.text = "network";

		_sceneManager = GetComponent<INetworkSceneManager>();
		if (_sceneManager == null) _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

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
		Utils.RunSafeUniTask(NetworkToggleAsync()).Forget();
		async UniTask NetworkToggleAsync()
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
				var manager = Instantiate(_avatarManagerPrefab);
				var result = await instance.StartGame(new StartGameArgs {
					GameMode = GameMode.AutoHostOrClient, ConnectionToken = Token,
					SceneManager = _sceneManager, Scene = SceneRef.FromIndex(activeScene.buildIndex),
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
		Utils.RunSafeUniTask(NetworkMigrateAsync()).Forget();
		async UniTask NetworkMigrateAsync()
		{
			await prevRunner.PushHostMigrationSnapshot(); // @note experiments showed it will likely fail
			await prevRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

			var activeScene = SceneManager.GetActiveScene();
			var instance = CreateRunner();
			var manager = Instantiate(_avatarManagerPrefab);
			var result = await instance.StartGame(new StartGameArgs {
				HostMigrationToken = hostMigrationToken, ConnectionToken = Token,
				SceneManager = _sceneManager, Scene = SceneRef.FromIndex(activeScene.buildIndex),
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
		GameCursor.Instance.SetConfined(!state);
	}

	private NetworkRunner CreateRunner()
	{
		var instance = Instantiate(_networkRunnerPrefab);
		instance.ProvideInput = true;
		
		if (instance.gameObject.GetComponent<INetworkRunnerCallbacks>() == null)
			instance.gameObject.AddComponent<NetworkEvents>();

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
