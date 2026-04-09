using Fusion;
using StudyPhotonBare.Game;
using StudyPhotonBare.Interfaces;
using UnityEngine;


namespace StudyPhotonBare.Services
{

public sealed class ResourcesService : IService
	, IEBSInitializeable
{
	// @note network behaviours are destroyed by default, but can be pooled
	public GameManagerNB AvatarManagerNBPrefab;
	public PickupManagerNB PickupManagerNBPrefab;
	// @note network runner should not be reused
	public NetworkRunner NetworkRunnerPrefab;

	[Header("Accessors")]
	private static readonly string Path = nameof(ResourcesService);

	public ResourcesService() => EventBus.Subscribe(this);

	void IEBSInitializeable.Initialize()
	{
		EventBus.Unsubscribe<IEBSInitializeable>(this);
		Load(out AvatarManagerNBPrefab);
		Load(out PickupManagerNBPrefab);
		Load(out NetworkRunnerPrefab);
	}

	private static void Load<T>(out T instance) where T : Object
	{
		var path = $"{Path}/{typeof(T).Name}";
		instance = Resources.Load<T>(path);
	}
}

}
