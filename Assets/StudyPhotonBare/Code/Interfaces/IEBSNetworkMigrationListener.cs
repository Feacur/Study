using Study.Interfaces;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSNetworkMigrationListener : IEventBusSubscriber
{
	void OnHostMigrated();
}

}
