using Fusion;
using UnityEngine;

namespace Study.PhotonFusion
{
	[RequireComponent(typeof(NetworkObject))]
	public class Player : NetworkBehaviour
	{
		// @note effectively can be deduced via `Runner.GetPlayerObject(Runner.LocalPlayer)`
		public static Player Local { get; private set; }

		public override void Spawned()
		{
			Debug.Log($"[Study] {nameof(Player)}.{nameof(Spawned)} {(HasInputAuthority ? "local" : "remote")} {Object.InputAuthority}");
			if (HasInputAuthority)
			{
				Local = this;
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			Debug.Log($"[Study] {nameof(Player)}.{nameof(Despawned)} {(HasInputAuthority ? "local" : "remote")} {Object.InputAuthority}");
			if (HasInputAuthority)
			{
				Local = null;
			}
		}
	}
}
