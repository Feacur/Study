using Fusion;
using Study.Interfaces;
using StudyPhotonBare.Interfaces;
using UnityEngine;
using PlayerID = System.Int32;


namespace StudyPhotonBare
{

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class AvatarNB : NetworkBehaviour
	, IEBSInitializeable
	, IEBSHitpointsListener
	, IEBSPlayerIDListener
	, IEBSNetworkMigrationListener
{
	public PlayerID PlayerID => NWPlayerID;

	[Header("Networked")]
	[Networked] PlayerID NWPlayerID { get; set; } // @todo store in the `GameManagerNB` ? needs spawning fixes

	[Header("Private")]
	private NetworkTransform _nwTransform;

	[Header("Accessors")]
	private NetworkObject Tag => GetComponent<NetworkObject>(); // need this ref before spawn

	void OnEnable() => EventBus.Subscribe(this, tag: Tag);
	void OnDisable() => EventBus.Unsubscribe(this, tag: Tag);

	void Awake()
	{
		_nwTransform = GetComponent<NetworkTransform>();
	}

	void IEBSInitializeable.Initialize()
	{
		EventBus.Unsubscribe<IEBSInitializeable>(this, tag: Tag);
		Respawn();
	}

	void IEBSHitpointsListener.OnHitpointsChanged(int current)
	{
		if (current <= 0) Respawn();
	}

	void IEBSPlayerIDListener.OnPlayerID(PlayerID playerID)
	{
		EventBus.Unsubscribe<IEBSPlayerIDListener>(this, tag: Tag);
		NWPlayerID = playerID;
	}

	void IEBSNetworkMigrationListener.OnHostMigrated()
	{
		// @note this object is already initialized, remove subs
		EventBus.Unsubscribe<IEBSNetworkMigrationListener>(this, tag: Tag);
		EventBus.Unsubscribe<IEBSInitializeable>(this, tag: Tag);
		EventBus.Unsubscribe<IEBSPlayerIDListener>(this, tag: Tag);
	}

	private void Respawn()
	{
		EventBus.Raise<IEBSResetable>(it => { it.Reset(); }, tag: Object);
		var targetPosition = Random.insideUnitCircle * 2;
		_nwTransform.Teleport(targetPosition);
	}
}

}
