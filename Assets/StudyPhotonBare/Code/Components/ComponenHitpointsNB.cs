using Fusion;
using StudyPhotonBare.Interfaces;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare.Components
{

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class ComponentHitpointsNB : NetworkBehaviour
	, IEBSResetable
	, IEBSDamageable
{
	[Header("Logics")] // @todo CMS
	[SerializeField] int _max = 3;

	[Header("Visuals")]
	[SerializeField] TMP_Text _hitpointsLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWHitpointsCR))] int NWHitpoints { get; set; }

	[Header("Accessors")]
	private NetworkObject Tag => GetComponent<NetworkObject>(); // need this ref before spawn

	void OnEnable() => EventBus.Subscribe(this, tag: Tag);
	void OnDisable() => EventBus.Unsubscribe(this, tag: Tag);

	public override void Spawned()
	{
		NWHitpointsCR();
	}

	void IEBSResetable.Reset()
	{
		NWHitpoints = _max;
	}

	void IEBSDamageable.TakeDamage(int damage)
	{
		NWHitpoints -= damage;
		if (NWHitpoints <= 0)
			EventBus.Raise<IEBSRespawnable>(it => { it.Respawn(); }, tag: Object);
	}

	private void NWHitpointsCR() => 
		_hitpointsLabel.text = NWHitpoints.ToString();
}

}
