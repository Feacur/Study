using Fusion;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare.Components
{

[RequireComponent(typeof(NetworkObject))]
public class ComponentLifetimeNB : NetworkBehaviour
{
	[Header("Visuals")]
	[SerializeField] TMP_Text _lifetimeLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWLifetimeCR))] int NWLifetime { get; set; }

	public void SAReset()
	{
		NWLifetime = 0;
	}

	public override void Spawned()
	{
		NWLifetimeCR();
	}

	public override void FixedUpdateNetwork()
	{
		if (HasStateAuthority)
			NWLifetime += 1;
	}

	private void NWLifetimeCR() => 
		_lifetimeLabel.text = (NWLifetime / 10).ToString();
}

}
