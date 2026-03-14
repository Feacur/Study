using Fusion;
using UnityEngine;

namespace Study.PhotonFusion
{
	[RequireComponent(typeof(NetworkObject))]
	public class PlayerListener : NetworkBehaviour
		, IPlayerJoined
		, IPlayerLeft
	{
		public static PlayerListener Instance { get; private set; }

		[SerializeField] Player _playerPrefab;

		void Awake()
		{
			Instance = this;
		}

		void OnDestroy()
		{
			Instance = null;
		}

		public override void Spawned()
		{
			Debug.Log($"[Study] {nameof(PlayerListener)}.{nameof(Spawned)} with {(HasStateAuthority ? "server" : "client")} authority");
			// Instance = this;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			Debug.Log($"[Study] {nameof(PlayerListener)}.{nameof(Despawned)} with {(HasStateAuthority ? "server" : "client")} authority");
			// Instance = null;
		}

		void IPlayerJoined.PlayerJoined(PlayerRef player)
		{
			Debug.Log($"[Study] {nameof(IPlayerJoined)}.{nameof(IPlayerJoined.PlayerJoined)} {player}");
			if (HasStateAuthority)
			{
				var playerInstance = Runner.Spawn(_playerPrefab, inputAuthority: player);
				Runner.SetPlayerObject(player, playerInstance.Object);
			}
		}

		void IPlayerLeft.PlayerLeft(PlayerRef player)
		{
			Debug.Log($"[Study] {nameof(IPlayerLeft)}.{nameof(IPlayerLeft.PlayerLeft)} {player}");
			if (HasStateAuthority)
			{
				var playerInstanceObject = Runner.GetPlayerObject(player);
				if (playerInstanceObject != null)
				{
					Runner.Despawn(playerInstanceObject);
				}
			}
		}
	}
}
