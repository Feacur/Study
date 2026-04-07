using Fusion;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare.Components
{

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(AvatarNB))]
public class ComponentHitpointsNB : NetworkBehaviour
{
	private const int HITPOINTS_MAX = 3;
	private const int HITPOINTS_DMG = 1;

	[SerializeField] TMP_Text _hitpointsLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWHitpointsCR))] int NWHitpoints { get; set; }

	public void SAReset()
	{
		NWHitpoints = HITPOINTS_MAX;
	}

	public void SATakeDamage()
	{
		if (NWHitpoints > HITPOINTS_DMG)
			NWHitpoints -= HITPOINTS_DMG;
		else
		{
			// @todo decouple
			var avatarNB = GetComponent<AvatarNB>();
			avatarNB.SAReset();
		}
	}

	public override void Spawned()
	{
		NWHitpointsCR();
	}

	private void NWHitpointsCR() => 
		_hitpointsLabel.text = NWHitpoints.ToString();
}

}
