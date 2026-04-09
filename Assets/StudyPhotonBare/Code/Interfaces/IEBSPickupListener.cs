using PlayerID = System.Int32;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSPickupListener : IEventBusSubscriber
{
	void OnPickup(PlayerID playerID, int id);
}

}
