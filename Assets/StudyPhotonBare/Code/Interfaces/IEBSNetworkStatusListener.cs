using StudyPhotonBare.Enums;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSNetworkStatusListener : IEventBusSubscriber
{
	void OnNetworkStatus(NetworkStatus status);
}

}
