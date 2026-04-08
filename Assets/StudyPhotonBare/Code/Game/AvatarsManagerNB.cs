using System.Collections.Generic;
using Fusion;
using StudyPhotonBare.Services;
using StudyPhotonBare.Tools;
using UnityEngine;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class AvatarsManagerNB : NetworkBehaviour
	, IPlayerJoined
	, IPlayerLeft
{
	public static AvatarsManagerNB Instance { get; private set; }

	[Header("Systems")]
	[SerializeField] AvatarNB _avatarPrefab;

	[Header("Private")]
	private readonly Dictionary<int, NetworkObject> _instances = new Dictionary<int, NetworkObject>();

	[Header("Accessors")]
	private NetworkService NetworkService => ServiceLocator.Get<NetworkService>();

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
		// @note the trick is that this manager belongs to the local player,
		// and in the shared topology we want to spawn only our avatar,
		// while leaving the others to be replicated by the framework
		if (!Utils.CanSpawn(Runner, player)) return;

		var token = GetToken(Runner, player);
		if (_instances.ContainsKey(token)) return;

		Runner.Spawn(
			_avatarPrefab, inputAuthority: player,
			onBeforeSpawned: (runner, instanceObject) => {
				Runner.SetPlayerObject(player, instanceObject);
				_instances.Add(token, instanceObject);

				var avatarNB = instanceObject.GetComponent<AvatarNB>();
				avatarNB.SAReset();
			}
		);
		Runner.PushHostMigrationSnapshot();
	}

	void IPlayerLeft.PlayerLeft(PlayerRef player)
	{
		var token = GetToken(Runner, player);
		if (_instances.TryGetValue(token, out var instanceObject))
			_instances.Remove(token);

		if (!instanceObject)
		{
			instanceObject = Runner.GetPlayerObject(player);
			if (instanceObject == null) return;
		}

		if (instanceObject)
		{
			Runner.Despawn(instanceObject);
			Runner.PushHostMigrationSnapshot();
		}
	}

	private int GetToken(NetworkRunner runner, PlayerRef player)
	{
		if (runner.LocalPlayer == player)
			return NetworkService.Token.GetHashCode();

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

					var avatarNB = instanceObject.GetComponent<AvatarNB>();
					avatarNB.SAReset();

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

}
