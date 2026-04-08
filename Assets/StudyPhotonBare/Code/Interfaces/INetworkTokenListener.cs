namespace StudyPhotonBare.Interfaces
{

public interface INetworkTokenListener : IEventBusSubscriber
{
	void OnNetworkToken(byte[] token);
}

}
