using Study.Interfaces;
using UnityEngine;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSShooter : IEventBusSubscriber
{
	void Shoot(Vector2 position, Vector2 direction);
}

}
