using StudyPhotonBare.Enums;

namespace StudyPhotonBare.Interfaces
{

public interface INetworkStatusListener : IEventBusSubscriber
{
	void OnNetworkStatus(NetworkStatus status);
}

}
