
using Fusion;
using Study.Interfaces;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Interfaces
{

public interface IEBSPlayerLeftListener : IEventBusSubscriber
{
	void OnPlayerLeft(PlayerID playerID);
}

}
