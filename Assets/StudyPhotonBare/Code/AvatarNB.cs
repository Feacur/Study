using Fusion;
using TMPro;
using UnityEngine;


namespace StudyPhotonBare
{

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class AvatarNB : NetworkBehaviour
{
	private const int HITPOINTS_MAX = 3;
	private const int HITPOINTS_DMG = 1;

	[Header("Visuals")]
	[SerializeField] TMP_Text _lifetimeLabel;
	[SerializeField] TMP_Text _hitpointsLabel;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWLifetimeCR))] int NWLifetime { get; set; }
	[Networked, OnChangedRender(nameof(NWHitpointsCR))] int NWHitpoints { get; set; }

	[Header("Private")]
	private NetworkTransform _networkTransform;

	public void SAInit()
	{
		SARespawn();
	}

	public void SARespawn()
	{
		NWLifetime = 0;
		NWHitpoints = HITPOINTS_MAX;
		_networkTransform.Teleport(Utils.Translate2D(
			Random.insideUnitCircle * 2
		));
	}

	public void SATakeDamage()
	{
		if (NWHitpoints > HITPOINTS_DMG)
			NWHitpoints -= HITPOINTS_DMG;
		else SARespawn();
	}

	void Awake()
	{
		_networkTransform = GetComponent<NetworkTransform>();
	}

	public override void Spawned()
	{
		// _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
		name = $"Avatar {Object.InputAuthority}";
		NWLifetimeCR();
		NWHitpointsCR();
	}

	public override void FixedUpdateNetwork()
	{
		if (HasStateAuthority)
		{
			NWLifetime += 1;
		}
	}

	// public override void Render()
	// {
	// 	// var interpolator = new NetworkBehaviourBufferInterpolator(this);
	// 	foreach (var change in _changeDetector.DetectChanges(this))
	// 	{
	// 		switch (change)
	// 		{
	// 			case nameof(NWLifetime): NWLifetimeCR(); break;
	// 			case nameof(NWHitpoints): NWHitpointsCR(); break;
	// 		}
	// 	}
	// }

	private void NWLifetimeCR() => 
		_lifetimeLabel.text = (NWLifetime / 10).ToString();

	private void NWHitpointsCR() => 
		_hitpointsLabel.text = NWHitpoints.ToString();
}

}
