using Study.Interfaces;
using PlayerID = System.Int32;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSPickupVerifier : IEventBusSubscriber
{
	void VerifyPickup(PlayerID playerID, int id);
}

}
