namespace StudyPhotonBare.Interfaces
{

public interface INetworkServiceEvents : ISubscriber
{
	void ToggleStatus();
}

public interface INetworkListenerEvents : ISubscriber
{
	void OnStatusChanged(bool status);
	void OnLocalToken(byte[] token);
}

}
