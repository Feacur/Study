using System.Collections.Generic;
using Fusion;
using StudyPhotonBare.Enums;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Tools;
using UnityEngine;
using PlayerID = System.Int32;
using DevAssert = UnityEngine.Assertions.Assert;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class GameManagerNB : NetworkBehaviour
	, IEBSPlayerIDListener
	, IEBSNetworkMigrator
	, IPlayerJoined
	, IPlayerLeft
{
	[Header("Systems")]
	[SerializeField] AvatarNB _avatarPrefab;

	[Header("Private")]
	private readonly Dictionary<PlayerID, (AvatarNB instance, AvatarStatus status)> _avatars = new();
	private PlayerID _localPlayerID;

	void Awake() => EventBus.Subscribe(this);
	void OnDestroy() => EventBus.Unsubscribe(this);

	void IEBSPlayerIDListener.OnPlayerID(PlayerID playerID)
	{
		EventBus.Unsubscribe<IEBSPlayerIDListener>(this);
		_localPlayerID = playerID;
	}

	void IEBSNetworkMigrator.MigrateHost()
	{
		foreach (var prevObject in Runner.GetResumeSnapshotNetworkObjects())
		{
			if (!prevObject.NetworkTypeId.IsPrefab) continue;

			var prevAvatarNB = prevObject.GetComponent<AvatarNB>();
			if (prevAvatarNB && prevAvatarNB.PlayerID == 0) continue;

			var position = Vector3.zero;
			var rotation = Quaternion.identity;
			if (prevObject.TryGetBehaviour(out NetworkTRSP prevTRSP))
			{
				position = prevTRSP.Data.Position;
				rotation = prevTRSP.Data.Rotation;
			}

			Runner.Spawn(prevObject,
				inputAuthority: prevObject.InputAuthority,
				position: position, rotation: rotation,
				onBeforeSpawned: (runner, instanceObject) => {
					if (prevAvatarNB)
					{
						runner.SetPlayerObject(prevObject.InputAuthority, instanceObject);
						_avatars.Add(prevAvatarNB.PlayerID, (instanceObject.GetComponent<AvatarNB>(), AvatarStatus.Migrated));
						EventBus.Raise<IEBSNetworkMigrationListener>(it => it.OnHostMigrated(), tag: instanceObject);
					}

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

	void IPlayerJoined.PlayerJoined(PlayerRef player)
	{
		// @note the trick is that this manager belongs to the local player,
		// and in the shared topology we want to spawn only our avatar,
		// while leaving the others to be replicated by the framework
		if (!Utils.CanManagePlayer(Runner, player)) return;

		var playerID = GetPlayerID();
		if (_avatars.TryGetValue(playerID, out var existing))
		{
			DevAssert.IsTrue(existing.status == AvatarStatus.Migrated);
			Runner.SetPlayerObject(player, existing.instance.Object);
			_avatars[playerID] = (existing.instance, AvatarStatus.Joined);
			existing.instance.Object.AssignInputAuthority(player);
		}
		else
		{
			Runner.Spawn(
				_avatarPrefab, inputAuthority: player,
				onBeforeSpawned: (runner, instanceObject) => {
					Runner.SetPlayerObject(player, instanceObject);
					_avatars.Add(playerID, (instanceObject.GetComponent<AvatarNB>(), AvatarStatus.Joined));
					EventBus.Raise<IEBSInitializeable>(it => { it.Initialize(); }, tag: instanceObject);
					EventBus.Raise<IEBSPlayerIDListener>(it => { it.OnPlayerID(playerID); }, tag: instanceObject);
				}
			);
		}
		Runner.PushHostMigrationSnapshot();

		PlayerID GetPlayerID()
		{
			if (Runner.LocalPlayer == player)
				return _localPlayerID;

			if (HasStateAuthority)
			{
				var playerToken = Runner.GetPlayerConnectionToken(player);
				if (playerToken != null) return Utils.GetPlayerID(playerToken);
			}

			return 0;
		}
	}

	void IPlayerLeft.PlayerLeft(PlayerRef player)
	{
		var instanceObject = Runner.GetPlayerObject(player);
		if (!instanceObject) return;

		var avatarNB = instanceObject.GetComponent<AvatarNB>();
		if (Utils.CanManagePlayer(Runner, player))
			EventBus.Raise<IEBSPlayerLeftListener>(it => it.OnPlayerLeft(avatarNB.PlayerID));

		_avatars.Remove(avatarNB.PlayerID);
		Runner.Despawn(instanceObject);
		Runner.PushHostMigrationSnapshot();
	}
}

}
