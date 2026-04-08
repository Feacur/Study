namespace StudyPhotonBare.Interfaces
{

public interface INetworkListener : IEventBusSubscriber
{
	void OnStatusChanged(bool status);
	void OnLocalToken(byte[] token);
}

}
