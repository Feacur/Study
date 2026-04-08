namespace StudyPhotonBare.Interfaces
{

public interface IEBSNetworkTokenListener : IEventBusSubscriber
{
	void OnNetworkToken(byte[] token);
}

}
