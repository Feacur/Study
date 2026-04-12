using Fusion;
using StudyPhotonBare.Interfaces;
using UnityEngine;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Components
{

public class PickupMarker : MonoBehaviour
{
	public PlayerID PlayerID { get; set; }
	public int ID { get; set; }

	void OnTriggerEnter2D(Collider2D collision)
	{
		if (ID <= 0) return;
		var entity = collision.attachedRigidbody
			? collision.attachedRigidbody.GetComponentInParent<NetworkObject>()
			: null;
		if (entity)
			EventBus.Raise<IEBSPickupVerifier>(it => { it.VerifyPickup(PlayerID, ID); }, tag: entity);
	}
}

}
