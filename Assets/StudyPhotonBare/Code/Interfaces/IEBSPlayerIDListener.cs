
using Study.Interfaces;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Interfaces
{

public interface IEBSPlayerIDListener : IEventBusSubscriber
{
	void OnPlayerID(PlayerID playerID);
}

}
