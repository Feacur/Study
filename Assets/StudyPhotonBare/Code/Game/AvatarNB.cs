using Fusion;
using UnityEngine;


namespace StudyPhotonBare
{

[RequireComponent(typeof(NetworkObject))]
public class AvatarNB : NetworkBehaviour
{
	public override void Spawned()
	{
		name = $"{nameof(AvatarNB)} {Object.InputAuthority}";
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		name += " [despawned]";
	}
}

}
