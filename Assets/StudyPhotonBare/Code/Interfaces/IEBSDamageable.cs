using Study.Interfaces;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSDamageable : IEventBusSubscriber
{
	void TakeDamage(int damage);
}

}
