using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class AvatarManager : NetworkBehaviour
	, IPlayerJoined
	, IPlayerLeft
{
	public static AvatarManager Instance;

	[SerializeField] Avatar _avatarPrefab;

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
		var instance = Runner.Spawn(
			_avatarPrefab, inputAuthority: player,
			onBeforeSpawned: (runner, instanceObject) => {
				Runner.SetPlayerObject(player, instanceObject);
			}
		);
	}

	void IPlayerLeft.PlayerLeft(PlayerRef player)
	{
		if (!HasStateAuthority) return;
		var instanceObject = Runner.GetPlayerObject(player);
		if (instanceObject == null) return;
		Runner.Despawn(instanceObject);
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
}
