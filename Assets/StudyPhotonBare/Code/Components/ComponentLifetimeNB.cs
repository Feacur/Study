using Fusion;
using StudyPhotonBare.Interfaces;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare.Components
{

[RequireComponent(typeof(NetworkObject))]
public class ComponentLifetimeNB : NetworkBehaviour
	, IResetable
{
	[Header("Visuals")]
	[SerializeField] TMP_Text _lifetimeLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWLifetimeCR))] int NWLifetime { get; set; }

	[Header("Accessors")]
	private NetworkObject NetworkObject => GetComponent<NetworkObject>(); // need this ref before spawn

	void OnEnable() => EventBus.SubscribeTagged(NetworkObject, this);
	void OnDisable() => EventBus.UnsubscribeTagged(NetworkObject, this);

	public override void Spawned()
	{
		NWLifetimeCR();
	}

	public override void FixedUpdateNetwork()
	{
		if (HasStateAuthority)
			NWLifetime += 1;
	}

	void IResetable.Reset()
	{
		NWLifetime = 0;
	}

	private void NWLifetimeCR() => 
		_lifetimeLabel.text = (NWLifetime / 10).ToString();
}

}
