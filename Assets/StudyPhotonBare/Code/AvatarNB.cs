using Fusion;
using StudyPhotonBare.Components;
using StudyPhotonBare.Tools;
using UnityEngine;


namespace StudyPhotonBare
{

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class AvatarNB : NetworkBehaviour
{
	[Header("Systems")]
	[SerializeField] ComponentLifetimeNB _lifetimeNB;
	[SerializeField] ComponentHitpointsNB _hitpointsNB;

	[Header("Private")]
	private NetworkTransform _networkTransform;

	public void SAReset()
	{
		_lifetimeNB.SAReset();
		_hitpointsNB.SAReset();
		_networkTransform.Teleport(Utils.Translate2D(
			Random.insideUnitCircle * 2
		));
	}

	void Awake()
	{
		_networkTransform = GetComponent<NetworkTransform>();
	}

	public override void Spawned()
	{
		name = $"Avatar {Object.InputAuthority}";
	}
}

}
