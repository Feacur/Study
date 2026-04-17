using Study.Interfaces;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSResetable : IEventBusSubscriber
{
	void Reset();
}

}
