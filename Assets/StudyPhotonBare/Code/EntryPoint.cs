using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntryPoint : MonoBehaviour
{
	public static EntryPoint Instance;
	public static readonly byte[] Token = System.Guid.NewGuid().ToByteArray();

	[SerializeField] AvatarManager _avatarManagerPrefab;
	[SerializeField] NetworkRunner _networkRunnerPrefab;
	[SerializeField] Button _networkButton;
	[SerializeField] TMP_Text _networkText;

	private NetworkRunner _networkRunner;

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
			var nextMenuState = !_networkButton.gameObject.activeSelf;
			_networkButton.gameObject.SetActive(nextMenuState);
			GameCursor.Instance.SetState(nextMenuState);
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
				var instance = Instantiate(_networkRunnerPrefab);
				var manager = Instantiate(_avatarManagerPrefab);
				var result = await instance.StartGame(new StartGameArgs {
					GameMode = GameMode.AutoHostOrClient, ConnectionToken = Token,
					Scene = SceneRef.FromIndex(activeScene.buildIndex),
					OnGameStarted = runner => { _networkRunner = runner; },
				});
				if (result.Ok) await NetworkFinalize(ct);
			}

			var nextMenuState = !_networkRunner;
			_networkButton.gameObject.SetActive(nextMenuState);
			GameCursor.Instance.SetState(nextMenuState);

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
			var instance = Instantiate(_networkRunnerPrefab);
			var manager = Instantiate(_avatarManagerPrefab);
			var result = await instance.StartGame(new StartGameArgs {
				HostMigrationToken = hostMigrationToken, ConnectionToken = Token,
				Scene = SceneRef.FromIndex(activeScene.buildIndex),
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

	private async UniTask NetworkFinalize(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (!_networkRunner) await UniTask.WaitUntil(() => _networkRunner, cancellationToken: ct);
		var events = _networkRunner.GetComponent<NetworkEvents>();
		events.OnHostMigration.AddListener(NetworkMigrate);
	}
}
