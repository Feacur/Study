using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class AvatarManager : NetworkBehaviour
	, IPlayerJoined
	, IPlayerLeft
{
	public static AvatarManager Instance;

	[SerializeField] Avatar _avatarPrefab;

	private readonly Dictionary<int, NetworkObject> _instances = new Dictionary<int, NetworkObject>();

	void Awake()
	{
		Instance = this;
	}

	void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	void IPlayerJoined.PlayerJoined(PlayerRef player)
	{
		if (!HasStateAuthority) return;

		var token = GetToken(Runner, player);
		if (_instances.ContainsKey(token)) return;

		Runner.Spawn(
			_avatarPrefab, inputAuthority: player,
			onBeforeSpawned: (runner, instanceObject) => {
				Runner.SetPlayerObject(player, instanceObject);
				_instances.Add(token, instanceObject);
			}
		);
		Runner.PushHostMigrationSnapshot();
	}

	void IPlayerLeft.PlayerLeft(PlayerRef player)
	{
		if (!HasStateAuthority) return;

		var token = GetToken(Runner, player);
		if (_instances.TryGetValue(token, out var instanceObject))
			_instances.Remove(token);

		if (!instanceObject)
		{
			instanceObject = Runner.GetPlayerObject(player);
			if (instanceObject == null) return;
		}

		Runner.Despawn(instanceObject);
		Runner.PushHostMigrationSnapshot();
	}

	private int GetToken(NetworkRunner runner, PlayerRef player)
	{
		if (runner.LocalPlayer == player)
			return EntryPoint.Token.GetHashCode();

		if (HasStateAuthority)
		{
			var token = runner.GetPlayerConnectionToken(player);
			if (token != null) return token.GetHashCode();
		}

		return 0;
	}

	public void NetworkResume(NetworkRunner runner)
	{
		foreach (var prevObject in runner.GetResumeSnapshotNetworkObjects())
		{
			if (!prevObject.NetworkTypeId.IsPrefab) continue;

			var token = GetToken(runner, prevObject.InputAuthority);
			if (token == 0) continue;

			_instances.Add(token, null);

			var position = Vector3.zero;
			var rotation = Quaternion.identity;
			if (prevObject.TryGetBehaviour(out NetworkTRSP prevTRSP))
			{
				position = prevTRSP.Data.Position;
				rotation = prevTRSP.Data.Rotation;
			}

			runner.Spawn(prevObject,
				inputAuthority: prevObject.InputAuthority,
				position: position, rotation: rotation,
				onBeforeSpawned: (runner, instanceObject) => {
					runner.SetPlayerObject(prevObject.InputAuthority, instanceObject);
					_instances[token] = instanceObject;

					instanceObject.CopyStateFrom(prevObject);

					var prevNBs = prevObject.GetComponents<NetworkBehaviour>();
					foreach (var prevNB in prevNBs)
					{
						var instNB = (NetworkBehaviour)instanceObject.GetComponent(prevNB.GetType());
						if (instNB) instNB.CopyStateFrom(prevNB);
					}
				}
			);
		}
	}
}
