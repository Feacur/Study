using Fusion;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Tools;
using UnityEngine;


namespace StudyPhotonBare
{

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class AvatarNB : NetworkBehaviour
	, IEBSInitializeable
	, IEBSHitpointsListener
{
	[Header("Private")]
	private NetworkTransform _networkTransform;

	[Header("Accessors")]
	private NetworkObject Tag => GetComponent<NetworkObject>(); // need this ref before spawn

	void Awake()
	{
		_networkTransform = GetComponent<NetworkTransform>();
	}

	void OnEnable() => EventBus.Subscribe(this, tag: Tag);
	void OnDisable() => EventBus.Unsubscribe(this, tag: Tag);

	public override void Spawned()
	{
		name = $"{nameof(AvatarNB)} {Object.InputAuthority}";
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		name += " [despawned]";
	}

	void IEBSInitializeable.Initialize()
	{
		EventBus.Unsubscribe<IEBSInitializeable>(this, tag: Tag); // unsubscribe only for the initialization inteface
		Respawn();
	}

	void IEBSHitpointsListener.OnHitpointsChanged(int current)
	{
		if (current <= 0) Respawn();
	}

	private void Respawn()
	{
		EventBus.Raise<IEBSResetable>(it => { it.Reset(); }, tag: Object);
		_networkTransform.Teleport(Utils.Translate2D(
			Random.insideUnitCircle * 2
		));
	}
}

}
