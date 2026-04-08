using Fusion;
using StudyPhotonBare.Interfaces;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare.Components
{

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class ComponentHitpointsNB : NetworkBehaviour
	, IResetable
	, IDamageable
{
	private const int HITPOINTS_MAX = 3;
	private const int HITPOINTS_DMG = 1;

	[SerializeField] TMP_Text _hitpointsLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWHitpointsCR))] int NWHitpoints { get; set; }

	[Header("Accessors")]
	private NetworkObject NetworkObject => GetComponent<NetworkObject>(); // need this ref before spawn

	void OnEnable() => EventBus.Subscribe(this, tag: NetworkObject);
	void OnDisable() => EventBus.Unsubscribe(this, tag: NetworkObject);

	public override void Spawned()
	{
		NWHitpointsCR();
	}

	void IResetable.Reset()
	{
		NWHitpoints = HITPOINTS_MAX;
	}

	void IDamageable.TakeDamage()
	{
		NWHitpoints -= HITPOINTS_DMG;
		if (NWHitpoints <= 0)
			EventBus.Raise<IRespawnable>(it => { it.Respawn(); }, tag: Object);
	}

	private void NWHitpointsCR() => 
		_hitpointsLabel.text = NWHitpoints.ToString();
}

}
