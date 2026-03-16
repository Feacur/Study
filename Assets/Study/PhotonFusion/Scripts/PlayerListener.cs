using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Study.PhotonFusion
{
	[RequireComponent(typeof(NetworkObject))]
	public class PlayerListener : NetworkBehaviour
		, IPlayerJoined
		, IPlayerLeft
	{
		// @fixme account for scripts reloading, find object and cache
		public static PlayerListener Instance { get; private set; }

		[SerializeField] Player _playerPrefab;

		private readonly Dictionary<int, NetworkObject> _players = new Dictionary<int, NetworkObject>();

		void Awake()
		{
			Instance = this;
		}

		void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		private readonly static Type[] AutoMigratedNetworkBehaviours = {
			typeof(Player),
			typeof(PlayerInput),
			typeof(PlayerController),
			typeof(Fusion.NetworkTRSP),
			typeof(Fusion.Addons.SimpleKCC.KCC),
		};

		public void HostMigrationResume(NetworkRunner runner)
		{
			Debug.Log($"[Study] {nameof(PlayerListener)}.{nameof(HostMigrationResume)}");
			foreach (var previous in runner.GetResumeSnapshotNetworkObjects())
			{
				if (!previous.NetworkTypeId.IsPrefab) continue;
				// if (!previous.GetComponent<Player>()) continue;

				var token = GetToken(runner, previous.InputAuthority);
				if (token == 0) continue;

				var position = Vector3.zero;
				var rotation = Quaternion.identity;
				if (previous.TryGetBehaviour(out NetworkTRSP prevTRSP))
				{
					position = prevTRSP.Data.Position;
					rotation = prevTRSP.Data.Rotation;
				}

				runner.Spawn(previous,
					inputAuthority: previous.InputAuthority,
					position: position, rotation: rotation,
					onBeforeSpawned: (runner, networkObject) => {
						Runner.SetPlayerObject(previous.InputAuthority, networkObject);
						_players.Add(token, networkObject);

						networkObject.CopyStateFrom(previous);
						foreach (var migration in AutoMigratedNetworkBehaviours)
						{
							var instNB = (NetworkBehaviour)networkObject.GetComponent(migration);
							var prevNB = (NetworkBehaviour)previous.GetComponent(migration);
							if (instNB && prevNB) instNB.CopyStateFrom(prevNB);
						}
					}
				);
			}
		}

		void IPlayerJoined.PlayerJoined(PlayerRef player)
		{
			Debug.Log($"[Study] {nameof(IPlayerJoined)}.{nameof(IPlayerJoined.PlayerJoined)} {player}");
			if (HasStateAuthority)
			{
				var token = GetToken(Runner, player);
				if (!_players.TryGetValue(token, out var existing))
				{
					var position = Vector3.zero;
					var rotation = Quaternion.identity;
					Runner.Spawn(_playerPrefab,
						inputAuthority: player,
						position: position, rotation: rotation,
						onBeforeSpawned: (runner, networkObject) => {
							Runner.SetPlayerObject(player, networkObject);
							_players.Add(token, networkObject);
						}
					);
					Network.PushSnapshot();
				}
			}
		}

		void IPlayerLeft.PlayerLeft(PlayerRef player)
		{
			Debug.Log($"[Study] {nameof(IPlayerLeft)}.{nameof(IPlayerLeft.PlayerLeft)} {player}");
			if (HasStateAuthority)
			{
				var token = GetToken(Runner, player);
				// var existing = Runner.GetPlayerObject(player);
				if (_players.TryGetValue(token, out var existing))
				{
					_players.Remove(token);
					Runner.Despawn(existing);
					Network.PushSnapshot();
				}
			}
		}

		private static int GetToken(NetworkRunner runner, PlayerRef player)
		{
			if (runner.LocalPlayer == player)
				return Network.Token.GetHashCode();

			var token = runner.GetPlayerConnectionToken(player);
			if (token != null) return token.GetHashCode();

			return 0;
		}
	}
}
