using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Study.PhotonFusion
{
	[RequireComponent(typeof(NetworkRunner), typeof(NetworkEvents))]
	public class Network : MonoBehaviour
		, INetworkRunnerUpdater
	{
		// @fixme account for scripts reloading, find object and cache
		public static Network Instance { get; private set; }

		public NetworkRunner Runner { get; private set; }
		public NetworkEvents Events { get; private set; }

		public static readonly byte[] Token = System.Guid.NewGuid().ToByteArray();

		void Awake()
		{
			Instance = this;
			Runner = GetComponent<NetworkRunner>();
			Events = GetComponent<NetworkEvents>();
		}

		void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		public static void PushSnapshot()
		{
			if (!Instance) return;
			PushSnapshotAsync().Forget();
			async UniTaskVoid PushSnapshotAsync()
			{
				await Instance.Runner.PushHostMigrationSnapshot();
			}
		}

		public static async UniTask StartGame()
		{
			EntryPoint.Instance.SpawnDefaultObjects();
			var startGameResult = await Instance.Runner.StartGame(new StartGameArgs {
				GameMode = GameMode.AutoHostOrClient,
				ConnectionToken = Token,
				//
				Scene = Instance.PrepareRunnerScene(),
				SceneManager = Instance.PrepareRunnerSceneManager(),
				//
				OnGameStarted = runner => {
					Debug.Log($"[Study] {nameof(Network)}.{nameof(StartGame)} -> {nameof(StartGameArgs)}.{nameof(StartGameArgs.OnGameStarted)}");
					EntryPoint.Instance.UpdatePushSnapshotButton(runner.IsServer);
				},
			});
			
			if (startGameResult.Ok)
			{
				Debug.Log($"[Study] {nameof(Network)}.{nameof(StartGame)} -> {ShutdownReason.Ok}");
				Instance.Events.OnHostMigration.AddListener(Migrate);
			}
			else
			{
				Debug.LogError($"[Study] {nameof(Network)}.{nameof(StartGame)} -> {startGameResult.ShutdownReason}\n* Error: {startGameResult.ErrorMessage}\n* Trace: {startGameResult.StackTrace}");
			}
		}

		public static async UniTask Shutdown()
		{
			Debug.Log($"[Study] {nameof(Network)}.{nameof(Shutdown)}");
			await Instance.Runner.Shutdown();
		}

		private static void Migrate(NetworkRunner previousRunner, HostMigrationToken hostMigrationToken)
		{
			MigrateAsync().Forget();
			async UniTaskVoid MigrateAsync()
			{
				await previousRunner.Shutdown(shutdownReason: ShutdownReason.HostMigration);
				await Instance.Runner.PushHostMigrationSnapshot();
				EntryPoint.Instance.SpawnDefaultObjects();
				var startGameResult = await Instance.Runner.StartGame(new StartGameArgs {
					HostMigrationToken = hostMigrationToken,
					ConnectionToken = Token,
					//
					Scene = Instance.PrepareRunnerScene(),
					SceneManager = Instance.PrepareRunnerSceneManager(),
					//
					HostMigrationResume = PlayerListener.Instance.HostMigrationResume,
					OnGameStarted = runner => {
						Debug.Log($"[Study] {nameof(Network)}.{nameof(Migrate)} -> {nameof(StartGameArgs)}.{nameof(StartGameArgs.OnGameStarted)}");
						EntryPoint.Instance.UpdatePushSnapshotButton(runner.IsServer);
					},
				});
			
				if (startGameResult.Ok)
				{
					Debug.Log($"[Study] {nameof(Network)}.{nameof(Migrate)} -> {ShutdownReason.Ok}");
					Instance.Events.OnHostMigration.AddListener(Migrate);
				}
				else
				{
					Debug.LogError($"[Study] {nameof(Network)}.{nameof(Migrate)} -> {startGameResult.ShutdownReason}\n* Error: {startGameResult.ErrorMessage}\n* Trace: {startGameResult.StackTrace}");
				}
			}
		}

		private SceneRef PrepareRunnerScene()
		{
			// alternatively `gameObject.scene`
			var activeScene = SceneManager.GetActiveScene();
			var ret = SceneRef.FromIndex(activeScene.buildIndex);
			return ret;
		}

		private INetworkSceneManager PrepareRunnerSceneManager()
		{
			var ret = GetComponent<INetworkSceneManager>();
			ret ??= gameObject.AddComponent<NetworkSceneManagerDefault>();
			return ret;
		}

		void INetworkRunnerUpdater.Initialize(NetworkRunner runner)
		{
			Debug.Log($"[Study] {nameof(INetworkRunnerUpdater)}.{nameof(INetworkRunnerUpdater.Initialize)} {runner}");
		}

		void INetworkRunnerUpdater.Shutdown(NetworkRunner runner)
		{
			Debug.Log($"[Study] {nameof(INetworkRunnerUpdater)}.{nameof(INetworkRunnerUpdater.Shutdown)} {runner}");
		}
	}
}
