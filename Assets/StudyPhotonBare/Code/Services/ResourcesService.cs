using Fusion;
using StudyPhotonBare.Game;
using StudyPhotonBare.Interfaces;
using UnityEngine;

namespace StudyPhotonBare.Services
{

public class ResourcesService : IService
{
	public readonly AvatarsManagerNB AvatarManagerNBPrefab; // @note network behaviours are destroyed by default, but can be pooled
	public readonly NetworkRunner NetworkRunnerPrefab; // @note network runner should not be reused

	[Header("Accessors")]
	private static readonly string Path = nameof(ResourcesService);

	public ResourcesService()
	{
		AvatarManagerNBPrefab = Resources.Load<AvatarsManagerNB>($"{Path}/{nameof(AvatarsManagerNB)}");
		NetworkRunnerPrefab = Resources.Load<NetworkRunner>($"{Path}/{nameof(NetworkRunner)}");
	}
}

}
