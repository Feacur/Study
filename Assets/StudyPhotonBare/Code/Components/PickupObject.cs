using Fusion;
using StudyPhotonBare.Interfaces;
using UnityEngine;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Components
{

public class PickupObject : MonoBehaviour
{
	public PlayerID PlayerID { get; set; }
	public int Id { get; set; }

	void OnTriggerEnter2D(Collider2D collision)
	{
		if (Id <= 0) return;
		var entity = collision.attachedRigidbody
			? collision.attachedRigidbody.GetComponentInParent<NetworkObject>()
			: null;
		if (entity)
			EventBus.Raise<IEBSPickupListener>(it => { it.OnPickup(PlayerID, Id); }, tag: entity);
	}
}

}
