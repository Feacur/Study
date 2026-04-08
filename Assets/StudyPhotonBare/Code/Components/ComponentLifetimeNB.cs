using Fusion;
using StudyPhotonBare.Interfaces;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare.Components
{

[RequireComponent(typeof(NetworkObject))]
public class ComponentLifetimeNB : NetworkBehaviour
	, IEBSResetable
{
	[Header("Visuals")]
	[SerializeField] TMP_Text _lifetimeLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWLifetimeCR))] int NWLifetime { get; set; }

	[Header("Accessors")]
	private NetworkObject NetworkObject => GetComponent<NetworkObject>(); // need this ref before spawn

	void OnEnable() => EventBus.Subscribe(this, tag: NetworkObject);
	void OnDisable() => EventBus.Unsubscribe(this, tag: NetworkObject);

	public override void Spawned()
	{
		NWLifetimeCR();
	}

	public override void FixedUpdateNetwork()
	{
		if (HasStateAuthority)
			NWLifetime += 1;
	}

	void IEBSResetable.Reset()
	{
		NWLifetime = 0;
	}

	private void NWLifetimeCR() => 
		_lifetimeLabel.text = (NWLifetime / 10).ToString();
}

}
