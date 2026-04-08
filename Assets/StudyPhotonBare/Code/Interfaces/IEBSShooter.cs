using UnityEngine;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSShooter : IEventBusSubscriber
{
	void Shoot(Vector3 position, Vector3 direction);
}

}
