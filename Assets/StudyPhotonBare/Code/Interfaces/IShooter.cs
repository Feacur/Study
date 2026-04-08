using UnityEngine;

namespace StudyPhotonBare.Interfaces
{

public interface IShooter : IEventBusSubscriber
{
	void Shoot(Vector3 position, Vector3 direction);
}

}
