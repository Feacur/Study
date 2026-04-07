using Fusion;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class AvatarNB : NetworkBehaviour
{
	private const int HITPOINTS_MAX = 3;

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
		NWHitpoints = HITPOINTS_MAX;
	}

	public void SAHit()
	{
		var nextHitpoints = NWHitpoints - 1;
		if (nextHitpoints <= 0)
		{ // @todo respawn
			nextHitpoints = HITPOINTS_MAX;
			NWLifetime = 0;
			_networkTransform.Teleport(Vector3.zero);
		}
		NWHitpoints = nextHitpoints;
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
