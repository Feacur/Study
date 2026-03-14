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
		public static Network Instance { get; private set; }

		public NetworkRunner Runner { get; private set; }
		public NetworkEvents Events { get; private set; }

		void Awake()
		{
			Instance = this;
			Runner = GetComponent<NetworkRunner>();
			Events = GetComponent<NetworkEvents>();
		}

		void OnDestroy()
		{
			Instance = null;
		}

		public async UniTask StartGame()
		{
			var activeScene = SceneManager.GetActiveScene(); // alternatively `gameObject.scene`
			var sceneManager = GetComponent<INetworkSceneManager>();
			sceneManager ??= gameObject.AddComponent<NetworkSceneManagerDefault>();
			var startGameResult = await Runner.StartGame(new StartGameArgs {
				// Address = NetAddress.Any(),
				GameMode = GameMode.AutoHostOrClient,
				Scene = SceneRef.FromIndex(activeScene.buildIndex),
				SceneManager = sceneManager,
				PlayerCount = 2,
				OnGameStarted = _ => {
					Debug.Log($"[Study] {nameof(Network)}.{nameof(StartGame)} -> {nameof(StartGameArgs)}.{nameof(StartGameArgs.OnGameStarted)}");
				},
			});
			
			if (startGameResult.Ok)
			{
				Debug.Log($"[Study] {nameof(Network)}.{nameof(StartGame)} -> {ShutdownReason.Ok}");
			}
			else
			{
				Debug.LogError($"[Study] {nameof(Network)}.{nameof(StartGame)} -> {startGameResult.ShutdownReason}\n* Error: {startGameResult.ErrorMessage}\n* Trace: {startGameResult.StackTrace}");
			}
		}

		public async UniTask Shutdown()
		{
			Debug.Log($"[Study] {nameof(Network)}.{nameof(Shutdown)}");
			await Runner.Shutdown();
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
