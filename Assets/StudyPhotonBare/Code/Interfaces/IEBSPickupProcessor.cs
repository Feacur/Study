using PlayerID = System.Int32;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSPickupProcessor : IEventBusSubscriber
{
	void ProcessPickup(PlayerID playerID, int id);
}

}
