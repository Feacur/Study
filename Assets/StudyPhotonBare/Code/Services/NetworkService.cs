using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StudyPhotonBare.Services
{

public class NetworkService : IService
	, INetworkServiceEvents
{
	public readonly byte[] Token = System.Guid.NewGuid().ToByteArray();
	public bool Status => _networkRunner && _networkRunner.State == NetworkRunner.States.Running;

	[Header("Private")]
	private NetworkRunner _networkRunner;

	[Header("Accessors")]
	private ResourcesService ResourcesService => ServiceLocator.Get<ResourcesService>();

	public NetworkService()
	{
		EventBus.Subscribe(this);
	}

	void INetworkServiceEvents.ToggleStatus()
	{
		var ct = Application.exitCancellationToken;
		NetworkToggleAsync().Forget();
		async UniTaskVoid NetworkToggleAsync()
		{
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
				_ = Object.Instantiate(ResourcesService.AvatarManagerNBPrefab);
				var result = await instance.StartGame(new StartGameArgs {
					GameMode = GameMode.AutoHostOrClient, ConnectionToken = Token,
					SceneManager = sceneManager, Scene = SceneRef.FromIndex(activeScene.buildIndex),
					OnGameStarted = runner => { _networkRunner = runner; },
				});

				if (result.Ok) await NetworkFinalize(ct);
			}

			EventBus.Raise<INetworkListenerEvents>(it => it.OnStatusChanged(Status));
		}
	}

	private void NetworkMigrate(NetworkRunner prevRunner, HostMigrationToken hostMigrationToken)
	{
		// @note see "network project config -> host migration update delay" for the granularity
		// of synchronizations. it is expected to lose some progress on an uncontrolled migration
		var ct = Application.exitCancellationToken;
		NetworkMigrateAsync().Forget();
		async UniTaskVoid NetworkMigrateAsync()
		{
			await prevRunner.PushHostMigrationSnapshot(); // @note experiments showed it will likely fail
			await prevRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

			var activeScene = SceneManager.GetActiveScene();
			var instance = CreateRunner();
			var sceneManager = instance.GetComponent<INetworkSceneManager>();
			var manager = Object.Instantiate(ResourcesService.AvatarManagerNBPrefab);
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

	private NetworkRunner CreateRunner()
	{
		var instance = Object.Instantiate(ResourcesService.NetworkRunnerPrefab);
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
