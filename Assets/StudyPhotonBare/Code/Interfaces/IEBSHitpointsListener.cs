namespace StudyPhotonBare.Interfaces
{

public interface IEBSHitpointsListener : IEventBusSubscriber
{
	void OnHitpointsChanged(int current);
}

}
