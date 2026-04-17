using Study.Interfaces;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSNetworkMigrator : IEventBusSubscriber
{
	void MigrateHost();
}

}
