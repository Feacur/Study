using System;
using System.Collections.Generic;
using Fusion;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Root;
using StudyPhotonBare.Services;
using StudyPhotonBare.Tools;
using UnityEngine;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class ArrowsControllerNB : NetworkBehaviour
	, IEBSShooter
	, IEBSPickupListener
	, IEBSPlayerIDListener
	, IEBSNetworkMigrationListener
{
	// @note technically this can be a shared managing object,
	// but then in shared topology we either need to chunk the
	// arrows array per player or give on of them complete
	// authority over the projectile - and then it's a mix
	// of complete p2p and host/client variant. shady

	[Header("Logics")] // @todo CMS
	[SerializeField] float _lifeSeconds = 0.5f;
	[SerializeField] float _speed = 20;
	[SerializeField] ContactFilter2D _contactFilter;
	[SerializeField] int _damage = 1;

	[Header("Visuals")]
	[SerializeField] GameObject _prefab; // @todo CMS

	[Header("Networked")]
	[Networked] int NWArrowsCooldown { get; set; }
	[Networked, Capacity(Constants.ArrowsPerAvatar), OnChangedRender(nameof(NWArrowsCR))] NetworkArray<NSArrow> NWArrows { get; }

	[Header("Private")]
	private PlayerID _playerID { get; set; }
	private readonly HashSet<int> _activeIDXs = new();
	private readonly List<Instance> _instances = new();
	private readonly List<RaycastHit2D> _hits = new();

	[Header("Accessors")]
	private NetworkObject Tag => GetComponent<NetworkObject>(); // need this ref before spawn
	private PoolOfGOService PoolOfGO => ServiceLocator.Get<PoolOfGOService>(); // @todo cache on spawn ?
	// @note an official video tutorial used `HasStateAuthority`, but it's illogical in hindsight;
	// another one, in text, suggests using `Object.IsProxy` for remote render time.
	// `HasInputAuthority` might be a good fit too, as per my experiments;
	// besides, "proxy" loosely means "~input & ~state"
	private int LifeTicks => (int)(_lifeSeconds * Runner.TickRate);

	void OnEnable() => EventBus.Subscribe(this, tag: Tag);
	void OnDisable() => EventBus.Unsubscribe(this, tag: Tag);

	public override void Spawned()
	{
		var count = NWArrows.Length * Runner.SessionInfo.PlayerCount;
		PoolOfGO.Warmup(_prefab, count);
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		var poolOfGO = PoolOfGO;
		Action<GameObject> Clear = poolOfGO != null
			? poolOfGO.Release
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
		for (int idx = 0; idx < NWArrows.Length; idx++)
		{
			var arrow = NWArrows[idx];
			if (!arrow.IsAlive) continue;

			var elapsed = Runner.Tick - arrow.InitTick;
			if (elapsed < 1) continue;

			GetPositionTicks(in arrow, elapsed - 1, out var positionPrev, out var _);
			GetPositionTicks(in arrow, elapsed,     out var positionCurr, out var _);
			var direction = positionCurr - positionPrev;

			var hitSomething = false;
			var hitsCount = scene.Raycast(positionPrev, direction, direction.magnitude, _contactFilter, _hits);
			for (int hitIndex = 0; hitIndex < hitsCount; hitIndex++)
			{
				var hit = _hits[hitIndex];
				var entity = hit.collider
					? hit.collider.GetComponentInParent<NetworkObject>()
					: null;
				if (entity && entity != damageSource)
				{
					// @note to broadcast the damage fact, it should be done separately, because
					// tagged events are designed for interactions with known entities or sets.
					// otherwise all non-targets would require manual checks 
					EventBus.Raise<IEBSDamageable>(it => { it.TakeDamage(_damage); }, tag: entity);
					hitSomething = true;
				}
			}

			if (hitSomething || elapsed > LifeTicks)
			{
				EventBus.Raise<IEBSDropListener>(it => it.OnDropped(_playerID, positionCurr));
				arrow.IsAlive = false; NWArrows.Set(idx, arrow);
			}
		}
	}

	public override void Render()
	{
		var time = Object.IsProxy ? Runner.RemoteRenderTime : Runner.LocalRenderTime;
		for (int i = 0; i < _instances.Count; i++)
		{
			var inst = _instances[i];
			var arrow = NWArrows[inst.ID];
			var elapsed =  time - arrow.InitTick * Runner.DeltaTime;
			GetPositionTime(in arrow, elapsed, out var position, out var rotation);
			inst.GO.transform.SetPositionAndRotation(position, rotation);
		}
	}

	void IEBSShooter.Shoot(Vector2 position, Vector2 direction)
	{
		var index = NWArrows.FindLastIndex(it => it.IsShootable);
		if (index < 0) return;

		if (NWArrowsCooldown > Runner.Tick) return;
		NWArrowsCooldown = Runner.Tick + Mathf.Max(1, LifeTicks / NWArrows.Length);

		NWArrows.Set(index, new NSArrow {
			IsAlive = true,
			InitTick = Runner.Tick,
			InitPosition = position,
			InitDirection = direction,
		});
	}

	void IEBSPickupListener.OnPickup(PlayerID playerID, int id)
	{
		var index = NWArrows.FindLastIndex(it => it.IsPickable);
		if (index <= 0) return;
		EventBus.Raise<IEBSPickupListener>(it => { it.OnPickup(_playerID, id); });
		NWArrows.Set(index, default);
	}

	void IEBSPlayerIDListener.OnPlayerID(PlayerID playerID)
	{
		EventBus.Unsubscribe<IEBSPlayerIDListener>(this, tag: Tag);
		_playerID = playerID;
	}

	void IEBSNetworkMigrationListener.OnHostMigrated()
	{
		// @note this object is already initialized, remove subs
		EventBus.Unsubscribe<IEBSNetworkMigrationListener>(this, tag: Tag);
		EventBus.Unsubscribe<IEBSPlayerIDListener>(this, tag: Tag);
		for (int idx = 0; idx < NWArrows.Length; idx++)
			NWArrows.Set(idx, default);
	}

	private void GetPositionTicks(in NSArrow arrow, int elapsed, out Vector2 position, out Quaternion rotation)
	{
		var time = elapsed * Runner.DeltaTime;
		GetPositionTime(arrow, time, out position, out rotation);
	}

	private void GetPositionTime(in NSArrow arrow, float elapsed, out Vector2 position, out Quaternion rotation)
	{
		position = arrow.InitPosition + (Vector2)arrow.InitDirection * (_speed * elapsed);
		rotation = Quaternion.FromToRotation(Vector2.right, (Vector2)arrow.InitDirection);
	}

	private void NWArrowsCR()
	{
		for (int i = _instances.Count - 1; i >= 0; i--)
		{
			var instance = _instances[i];
			if (_activeIDXs.Contains(instance.ID))
			{
				var arrow = NWArrows[instance.ID];
				if (!arrow.IsAlive)
				{
					PoolOfGO.Release(instance.GO);
					_activeIDXs.Remove(instance.ID);
					_instances.RemoveAt(i);
				}
			}
		}

		for (int idx = 0; idx < NWArrows.Length; idx++)
		{
			if (!_activeIDXs.Contains(idx))
			{
				var arrow = NWArrows[idx];
				if (arrow.IsAlive)
				{
					var instanceGO = PoolOfGO.Fetch(_prefab);

					_activeIDXs.Add(idx);
					_instances.Add(new Instance {
						ID = idx,
						GO = instanceGO,
					});
				}
			}
		}
	}

	private struct NSArrow : INetworkStruct
	{
		public int InitTick;
		public NetworkBool IsAlive;
		public Vector2Compressed InitPosition;
		public Vector2Compressed InitDirection;

		public bool IsShootable => InitTick == 0;
		public bool IsPickable => InitTick > 0 && !IsAlive;
	}

	private struct Instance
	{
		public int ID;
		public GameObject GO;
	}
}

}
