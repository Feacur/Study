using Fusion;
using StudyPhotonBare.Game;
using StudyPhotonBare.Interfaces;
using UnityEngine;


namespace StudyPhotonBare.Services
{

public sealed class ResourcesService : IService
	, IEBSInitializeable
{
	public AvatarsManagerNB AvatarManagerNBPrefab { get; private set; } // @note network behaviours are destroyed by default, but can be pooled
	public NetworkRunner NetworkRunnerPrefab { get; private set; } // @note network runner should not be reused

	[Header("Accessors")]
	private static readonly string Path = nameof(ResourcesService);

	public ResourcesService() => EventBus.Subscribe(this);

	void IEBSInitializeable.Initialize()
	{
		EventBus.Unsubscribe<IEBSInitializeable>(this); // unsubscribe only for the initialization inteface
		AvatarManagerNBPrefab = Load<AvatarsManagerNB>();
		NetworkRunnerPrefab = Load<NetworkRunner>();
	}

	private static T Load<T>() where T : Object
	{
		var path = $"{Path}/{typeof(T).Name}";
		var ret = Resources.Load<T>(path);
		return ret;
	}
}

}
