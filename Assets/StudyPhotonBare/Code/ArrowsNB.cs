using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class ArrowsNB : NetworkBehaviour
{
	// @note technically this can be a shared managing object,
	// but then in shared topology we either need to chunk the
	// arrows array per player or give on of them complete
	// authority over the projectile - and then it's a mix
	// of complete p2p and host/client variant. shady
	private const int ARROWS_LIMIT = 4;

	[Header("Logics")]
	[SerializeField] int _arrowLifeSeconds = 1;
	[SerializeField] float _arrowSpeed = 20;
	[SerializeField] ContactFilter2D contactFilter;

	[Header("Visuals")]
	[SerializeField] GameObject _arrowPrefab;

	[Header("Networked")]
	[Networked] int NWArrowsCooldown { get; set; }
	[Networked, Capacity(ARROWS_LIMIT)] NetworkArray<Arrow> NWArrows { get; }
	[Networked] int NWArrowsWrite { get; set; }

	[Header("Private")]
	private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>();
	private readonly Instance[] _instances = new Instance[ARROWS_LIMIT];

	public void SASpawn(Vector3 position, Vector3 direction)
	{
		if (NWArrowsCooldown > Runner.Tick) return;
		NWArrowsCooldown = 1 + Mathf.Max(0, Runner.Tick + _arrowLifeSeconds * Runner.TickRate / ARROWS_LIMIT);

		NWArrows.Set(NWArrowsWrite, new Arrow {
			InitTick = Runner.Tick,
			InitPosition = position,
			InitDirection = direction,
		});
		NWArrowsWrite = (NWArrowsWrite + 1) % NWArrows.Length;
	}

	void Awake()
	{
		// @todo instantiate on demand, pool
		for (int i = 0; i < _instances.Length; i++)
		{
			var go = Instantiate(_arrowPrefab); go.SetActive(false);
			_instances[i] = new Instance {GO = go};
		}
	}

	public override void FixedUpdateNetwork()
	{
		// @todo compact alive set or have read-write pointers
		var scene = Runner.GetPhysicsScene2D();
		for (int i = 0; i < NWArrows.Length; i++)
		{
			var arrow = NWArrows[i];
			if (!arrow.IsAlive) continue;

			var elapsed = Runner.Tick - arrow.InitTick;
			if (elapsed < 1) continue;

			GetPositionTicks(in arrow, elapsed - 1, out var positionCurr, out var _);
			GetPositionTicks(in arrow, elapsed,     out var positionNext, out var _);
			var direction = positionNext - positionCurr;

			var hitSomething = false;
			var hitsCount = scene.Raycast(positionCurr, direction, direction.magnitude, contactFilter, _hits);
			for (int hitIndex = 0; hitIndex < hitsCount; hitIndex++)
			{
				var hit = _hits[hitIndex];
				var avatar = hit.collider
					? hit.collider.GetComponentInParent<AvatarNB>()
					: null;
				if (avatar && avatar.Object.InputAuthority != Object.InputAuthority)
				{
					avatar.SAHit();
					hitSomething = true;
				}
			}

			if (hitSomething || elapsed > _arrowLifeSeconds * Runner.TickRate)
				NWArrows.Set(i, default);
		}
	}

	public override void Render()
	{
		for (int i = 0; i < _instances.Length; i++)
		{
			var inst = _instances[i];
			var arrow = NWArrows[i];
			if (arrow.IsAlive)
			{
				var time = HasStateAuthority
					? Runner.LocalRenderTime
					: Runner.RemoteRenderTime;
				var elapsed = time - arrow.InitTick * Runner.DeltaTime;
				GetPositionTime(in arrow, elapsed, out var position, out var rotation);

				inst.GO.SetActive(elapsed >= 0); // @note: should be visible only with valid time
				inst.GO.transform.SetPositionAndRotation(position, rotation);
			}
			else inst.GO.SetActive(false);
		}
	}

	private void GetPositionTicks(in Arrow arrow, int elapsed, out Vector3 position, out Quaternion rotation)
	{
		var time = elapsed * Runner.DeltaTime;
		GetPositionTime(arrow, time, out position, out rotation);
	}

	private void GetPositionTime(in Arrow arrow, float elapsed, out Vector3 position, out Quaternion rotation)
	{
		position = arrow.InitPosition + (Vector3)arrow.InitDirection * (_arrowSpeed * elapsed);
		rotation = Quaternion.FromToRotation(Vector3.right, arrow.InitDirection);
	}

	private struct Arrow : INetworkStruct
	{
		public int InitTick;
		public Vector3Compressed InitPosition;
		public Vector3Compressed InitDirection;

		public bool IsAlive => InitTick > 0;
	}

	private struct Instance
	{
		public GameObject GO;
	}
}
