using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ArrowNB : NetworkBehaviour
{
	[Networked] int NWInitTick { get; set; }
	[Networked] Vector3 NWInitPosition { get; set; }
	[Networked] Vector3 NWInitDirection { get; set; }

	public void Init(Vector3 position, Vector3 direction)
	{
		// @todo as a next step, operate projectiles in a similar to particle systems manner
		// store pure data in an array, draw them batched; i.e. instead of `ArrowNB` code will
		// queue shots into `ArrowsNB`
		NWInitTick = Runner.Tick;
		NWInitPosition = position;
		NWInitDirection = direction;
	}

	public override void FixedUpdateNetwork()
	{
		// GetPosition(Runner.Tick, out var positionCurr);
		// GetPosition(Runner.Tick + 1, out var positionNext);
		// @todo check collision
		var elapsed = Runner.Tick - NWInitTick;
		if (elapsed > Runner.TickRate)
			Runner.Despawn(Object);
	}

	public override void Render()
	{
		var time = HasStateAuthority
			? Runner.LocalRenderTime
			: Runner.RemoteRenderTime;
		var elapsed = time - NWInitTick * Runner.DeltaTime;
		GetPosition(elapsed, out var position);
		transform.position = position;
	}

	private void GetPosition(int tick, out Vector3 position)
	{
		var time = tick >= NWInitTick
			? (tick - NWInitTick) * Runner.DeltaTime
			: 0;
		GetPosition(time, out position);
	}

	private void GetPosition(float elapsed, out Vector3 position)
		=> position = elapsed > 0
			? NWInitPosition + NWInitDirection * (elapsed * 10)
			: NWInitPosition;
}
