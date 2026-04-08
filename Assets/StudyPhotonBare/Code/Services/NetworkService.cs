using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using StudyPhotonBare.Enums;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace StudyPhotonBare.Services
{

public sealed class NetworkService : IService
	, IEBSNetworkToggler
{
	[Header("Private")]
	private readonly byte[] Token = System.Guid.NewGuid().ToByteArray();
	private NetworkRunner _networkRunner;

	[Header("Accessors")]
	private ResourcesService ResourcesService => ServiceLocator.Get<ResourcesService>();
	private bool IsOff => !_networkRunner || _networkRunner.State == NetworkRunner.States.Shutdown;

	public NetworkService() => EventBus.Subscribe(this);

	void IEBSNetworkToggler.ToggleNetwork()
	{
		var ct = Application.exitCancellationToken;
		NetworkToggleAsync().Forget();
		async UniTaskVoid NetworkToggleAsync()
		{
			if (_networkRunner)
			{
				EventBus.Raise<IEBSNetworkStatusListener>(it => it.OnNetworkStatus(NetworkStatus.Shutting));
				await _networkRunner.Shutdown();
				ct.ThrowIfCancellationRequested();
				_networkRunner = null;
				EventBus.Raise<IEBSNetworkStatusListener>(it => it.OnNetworkStatus(NetworkStatus.None));
			}
			else
			{
				EventBus.Raise<IEBSNetworkStatusListener>(it => it.OnNetworkStatus(NetworkStatus.Starting));
				var instance = CreateRunner();
				_ = Object.Instantiate(ResourcesService.AvatarManagerNBPrefab);

				EventBus.Raise<IEBSNetworkTokenListener>(it => it.OnNetworkToken(Token));
				var result = await instance.StartGame(new StartGameArgs {
					GameMode = GameMode.AutoHostOrClient, ConnectionToken = Token,
					SceneManager = instance.GetComponent<INetworkSceneManager>(),
					Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
					OnGameStarted = runner => { _networkRunner = runner; },
				});

				if (result.Ok) await NetworkFinalize(ct);

				var status = IsOff ? NetworkStatus.None : NetworkStatus.Running;
				EventBus.Raise<IEBSNetworkStatusListener>(it => it.OnNetworkStatus(status));
			}
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
			EventBus.Raise<IEBSNetworkStatusListener>(it => it.OnNetworkStatus(NetworkStatus.Migrating));
			await prevRunner.PushHostMigrationSnapshot(); // @note experiments showed it will likely fail
			await prevRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

			var instance = CreateRunner();
			var manager = Object.Instantiate(ResourcesService.AvatarManagerNBPrefab);

			EventBus.Raise<IEBSNetworkTokenListener>(it => it.OnNetworkToken(Token));
			var result = await instance.StartGame(new StartGameArgs {
				HostMigrationToken = hostMigrationToken, ConnectionToken = Token,
				SceneManager = instance.GetComponent<INetworkSceneManager>(),
				Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
				OnGameStarted = runner => { _networkRunner = runner; },
				HostMigrationResume = runner => { EventBus.Raise<IEBSNetworkMigrator>(it => it.MigrateHost()); },
			});

			if (result.Ok)
			{
				await instance.PushHostMigrationSnapshot(); // @note experiments showed it will likely fail
				await NetworkFinalize(ct);
			}

			var status = IsOff ? NetworkStatus.None : NetworkStatus.Running;
			EventBus.Raise<IEBSNetworkStatusListener>(it => it.OnNetworkStatus(status));
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
