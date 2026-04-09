using System;
using System.Collections.Generic;
using Fusion;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Services;
using StudyPhotonBare.Tools;
using UnityEngine;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class ArrowsControllerNB : NetworkBehaviour
	, IEBSShooter
{
	// @note technically this can be a shared managing object,
	// but then in shared topology we either need to chunk the
	// arrows array per player or give on of them complete
	// authority over the projectile - and then it's a mix
	// of complete p2p and host/client variant. shady

	[Header("Logics")] // @todo CMS
	[SerializeField] int _lifeSeconds = 1;
	[SerializeField] float _speed = 20;
	[SerializeField] ContactFilter2D _contactFilter;
	[SerializeField] int _damage = 1;

	[Header("Visuals")]
	[SerializeField] GameObject _prefab;

	[Header("Networked")]
	[Networked] int NWArrowsCooldown { get; set; }
	[Networked] int NWArrowsWrite { get; set; }
	[Networked, Capacity(4), OnChangedRender(nameof(NWArrowsCR))] NetworkArray<NSArrow> NWArrows { get; }

	[Header("Private")]
	private readonly HashSet<int> _activeIDs = new HashSet<int>();
	private readonly List<Instance> _instances = new List<Instance>();
	private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>();

	[Header("Accessors")]
	private NetworkObject NetworkObject => GetComponent<NetworkObject>(); // need this ref before spawn
	private PoolOfGOService PoolOfGO => ServiceLocator.Get<PoolOfGOService>(); // @todo cache on spawn ?
	// @note an official video tutorial used `HasStateAuthority`, but it's illogical in hindsight;
	// another one, in text, suggests using `Object.IsProxy` for remote render time.
	// `HasInputAuthority` might be a good fit too, as per my experiments;
	// besides, "proxy" loosely means "~input & ~state"
	private float Time => Object.IsProxy ? Runner.RemoteRenderTime : Runner.LocalRenderTime;
	private float GetElapsed(in NSArrow arrow, float time) => time - arrow.InitTick * Runner.DeltaTime;
	private bool IsVisible(in NSArrow arrow, float time) => arrow.IsAlive && (GetElapsed(in arrow, time) >= 0);
	private int LifeTicks => _lifeSeconds * Runner.TickRate;

	void OnEnable() => EventBus.Subscribe(this, tag: NetworkObject);
	void OnDisable() => EventBus.Unsubscribe(this, tag: NetworkObject);

	void IEBSShooter.Shoot(Vector3 position, Vector3 direction)
	{
		if (NWArrowsCooldown > Runner.Tick) return;
		NWArrowsCooldown = Runner.Tick + Mathf.Max(1, LifeTicks / NWArrows.Length);

		NWArrows.Set(NWArrowsWrite, new NSArrow {
			InitTick = Runner.Tick,
			InitPosition = position,
			InitDirection = direction,
		});
		NWArrowsWrite = (NWArrowsWrite + 1) % NWArrows.Length;
	}

	public override void Spawned()
	{
		var count = NWArrows.Length * Runner.SessionInfo.PlayerCount;
		PoolOfGO.Warmup(_prefab, count);
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		var poolOfGO = PoolOfGO;
		Action<GameObject> Clear = poolOfGO != null
			? poolOfGO.Ret
			: Destroy;
		foreach (var it in _instances)
			Clear(it.GO);
		_instances.Clear();
	}

	public override void FixedUpdateNetwork()
	{
		// @todo compact alive set or have read-write pointers
		var damageSource = Object;
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
			var hitsCount = scene.Raycast(positionCurr, direction, direction.magnitude, _contactFilter, _hits);
			for (int hitIndex = 0; hitIndex < hitsCount; hitIndex++)
			{
				var hit = _hits[hitIndex];
				var entity = hit.collider
					? hit.collider.GetComponentInParent<NetworkObject>()
					: null;
				if (entity && entity != damageSource)
				{
					EventBus.Raise<IEBSDamageable>(it => { it.TakeDamage(_damage); }, tag: entity);
					hitSomething = true;
				}
			}

			if (hitSomething || elapsed > LifeTicks)
				NWArrows.Set(i, default);
		}
	}

	public override void Render()
	{
		var time = Time;
		for (int i = 0; i < _instances.Count; i++)
		{
			var inst = _instances[i];
			var arrow = NWArrows[inst.ID];
			if (!arrow.IsAlive) continue; // @note ok, this can be avoided with a dedicated `IsAlive` field
			var elapsed = GetElapsed(in arrow, time);
			GetPositionTime(in arrow, elapsed, out var position, out var rotation);
			inst.GO.transform.SetPositionAndRotation(position, rotation);
		}
	}

	private void GetPositionTicks(in NSArrow arrow, int elapsed, out Vector3 position, out Quaternion rotation)
	{
		var time = elapsed * Runner.DeltaTime;
		GetPositionTime(arrow, time, out position, out rotation);
	}

	private void GetPositionTime(in NSArrow arrow, float elapsed, out Vector3 position, out Quaternion rotation)
	{
		position = arrow.InitPosition + (Vector3)arrow.InitDirection * (_speed * elapsed);
		rotation = Quaternion.FromToRotation(Vector3.right, arrow.InitDirection);
	}

	private void NWArrowsCR()
	{
		var time = Time;

		for (int i = _instances.Count - 1; i >= 0; i--)
		{
			var inst = _instances[i];
			if (_activeIDs.Contains(inst.ID))
			{
				var arrow = NWArrows[inst.ID];
				if (!IsVisible(in arrow, time))
				{
					PoolOfGO.Ret(inst.GO);
					_activeIDs.Remove(inst.ID);
					_instances.RemoveAt(i);
				}
			}
		}

		for (int id = 0; id < NWArrows.Length; id++)
		{
			if (!_activeIDs.Contains(id))
			{
				var arrow = NWArrows[id];
				if (IsVisible(in arrow, time))
				{
					_activeIDs.Add(id);
					_instances.Add(new Instance {
						ID = id,
						GO = PoolOfGO.Get(_prefab),
					});
				}
			}
		}
	}

	private struct NSArrow : INetworkStruct
	{
		public int InitTick;
		public Vector3Compressed InitPosition;
		public Vector3Compressed InitDirection;

		public bool IsAlive => InitTick > 0;
	}

	private struct Instance
	{
		public int ID;
		public GameObject GO;
	}
}

}
