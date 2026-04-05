using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class ArrowNB : NetworkBehaviour
{
	[Networked] TickTimer Life { get; set; }

	public void Init()
	{
		Life = TickTimer.CreateFromSeconds(Runner, 1);
	}

	public override void FixedUpdateNetwork()
	{
		var direction = transform.rotation * Vector3.right;
		var deltaMove = direction * (10 * Runner.DeltaTime);
		transform.position += deltaMove;
		if (Life.Expired(Runner))
			Runner.Despawn(Object);
	}
}
