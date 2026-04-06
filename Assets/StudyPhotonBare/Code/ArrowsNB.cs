using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ArrowsNB : NetworkBehaviour
{
	private const int ARROWS_LIMIT = 4;

	[SerializeField] GameObject _arrowPrefab;
	[SerializeField] int _arrowLifeSeconds = 1;
	[SerializeField] float _arrowSpeed = 20;

	[Networked] int NWArrowsCooldown { get; set; }
	[Networked, Capacity(ARROWS_LIMIT)] NetworkArray<Arrow> NWArrows { get; }
	[Networked] int NWArrowsWrite { get; set; }

	private readonly Instance[] _instances = new Instance[ARROWS_LIMIT];

	void Awake()
	{
		// @todo instantiate on demand, pool
		for (int i = 0; i < _instances.Length; i++)
		{
			var go = Instantiate(_arrowPrefab); go.SetActive(false);
			_instances[i] = new Instance {GO = go};
		}
	}

	public void Spawn(Vector3 position, Vector3 direction)
	{
		if (NWArrowsCooldown > Runner.Tick) return;
		NWArrowsCooldown = 1 + Mathf.Max(0, Runner.Tick + _arrowLifeSeconds * Runner.TickRate / ARROWS_LIMIT);

		NWArrows.Set(NWArrowsWrite, new Arrow {
			InitTick = Runner.Tick,
			InitPosition = position,
			InitDirection = direction,
			IsAlive = true,
		});
		NWArrowsWrite = (NWArrowsWrite + 1) % NWArrows.Length;
	}

	public override void FixedUpdateNetwork()
	{
		// GetPosition(Runner.Tick, out var positionCurr);
		// GetPosition(Runner.Tick + 1, out var positionNext);
		// @todo check collision
		// @todo compact alive set or have read-write pointers
		for (int i = 0; i < NWArrows.Length; i++)
		{
			var arrow = NWArrows[i];
			if (!arrow.IsAlive) continue;
			var elapsed = Runner.Tick - arrow.InitTick;
			if (elapsed > _arrowLifeSeconds * Runner.TickRate)
				NWArrows.Set(i, default);
		}
	}

	public override void Render()
	{
		for (int i = 0; i < _instances.Length; i++)
		{
			var inst = _instances[i];
			var arrow = NWArrows[i];
			inst.GO.SetActive(arrow.IsAlive);
			if (arrow.IsAlive)
			{
				var time = HasStateAuthority
					? Runner.LocalRenderTime
					: Runner.RemoteRenderTime;
				var elapsed = time - arrow.InitTick * Runner.DeltaTime;
				GetPosition(in arrow, elapsed, out var position, out var rotation);
				inst.GO.transform.SetPositionAndRotation(position, rotation);
			}
		}
	}

	private void GetPosition(in Arrow arrow, int tick, out Vector3 position, out Quaternion rotation)
	{
		var time = tick >= arrow.InitTick
			? (tick - arrow.InitTick) * Runner.DeltaTime
			: 0;
		GetPosition(arrow, time, out position, out rotation);
	}

	private void GetPosition(in Arrow arrow, float elapsed, out Vector3 position, out Quaternion rotation)
	{
		position = elapsed > 0
			? arrow.InitPosition + (Vector3)arrow.InitDirection * (_arrowSpeed * elapsed)
			: arrow.InitPosition;
		rotation = Quaternion.FromToRotation(Vector3.right, arrow.InitDirection);
	}

	private struct Arrow : INetworkStruct
	{
		public int InitTick;
		public Vector3Compressed InitPosition;
		public Vector3Compressed InitDirection;
		public NetworkBool IsAlive; // or an end tick if it can vary
	}

	private struct Instance
	{
		public GameObject GO;
	}
}
