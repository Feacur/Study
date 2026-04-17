using Study.Interfaces;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSNetworkToggler : IEventBusSubscriber
{
	void ToggleNetwork();
}

}
