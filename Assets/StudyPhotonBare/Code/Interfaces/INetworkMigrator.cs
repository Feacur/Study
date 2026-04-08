namespace StudyPhotonBare.Interfaces
{

public interface INetworkMigrator : IEventBusSubscriber
{
	void MigrateHost();
}

}
