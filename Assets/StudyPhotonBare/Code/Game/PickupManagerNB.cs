using System;
using System.Collections.Generic;
using Fusion;
using StudyPhotonBare.Components;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Services;
using StudyPhotonBare.Tools;
using UnityEngine;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class PickupManagerNB : NetworkBehaviour
	, IEBSPlayerLeftListener
	, IEBSDropListener
	, IEBSPickupListener
{
	[Header("Logics")] // @todo CMS
	[SerializeField] ContactFilter2D _contactFilter;

	[Header("Visuals")]
	[SerializeField] GameObject _prefab; // @todo CMS

	[Header("Networked")]
	[Networked, Capacity(64), OnChangedRender(nameof(NWArrowsCR))] NetworkArray<NSArrow> NWArrows { get; }

	[Header("Private")]
	private readonly HashSet<int> _activeIDs = new();
	private readonly List<Instance> _instances = new();

	[Header("Accessors")]
	private PoolOfGOService PoolOfGO => ServiceLocator.Get<PoolOfGOService>(); // @todo cache on spawn ?

	void OnEnable() => EventBus.Subscribe(this);
	void OnDisable() => EventBus.Unsubscribe(this);

	public override void Spawned()
	{
		var count = NWArrows.Length;
		PoolOfGO.Warmup(_prefab, count);
		NWArrowsCR();
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

	void IEBSPlayerLeftListener.OnPlayerLeft(PlayerID playerID)
	{
		for (int id = 0; id < NWArrows.Length; id++)
		{
			var arrow = NWArrows[id];
			if (arrow.PlayerID == playerID)
				NWArrows.Set(id, default);
		}
	}

	void IEBSDropListener.OnDropped(PlayerID playerID, Vector2 position)
	{
		var index = NWArrows.FindFirstIndex(it => !it.IsAlive);
		if (index < 0) return;

		NWArrows.Set(index, new NSArrow {
			IsAlive = true,
			PlayerID = playerID,
			InitPosition = position,
		});
	}

	void IEBSPickupListener.OnPickup(PlayerID playerID, int id)
	{
		if (id <= 0) return;

		var arrow = NWArrows[id - 1];
		if (arrow.PlayerID != playerID)
		{
			var playerArrowIndex = NWArrows.FindFirstIndex(it => it.PlayerID == playerID);
			var playerArrow = NWArrows[playerArrowIndex];
			playerArrow.PlayerID = arrow.PlayerID;
			NWArrows.Set(playerArrowIndex, playerArrow);
		}

		NWArrows.Set(id - 1, default);
	}

	private void NWArrowsCR()
	{
		for (int i = _instances.Count - 1; i >= 0; i--)
		{
			var instance = _instances[i];
			if (_activeIDs.Contains(instance.ID))
			{
				var arrow = NWArrows[instance.ID];
				if (!arrow.IsAlive)
				{
					var pickupObject = instance.GO.GetComponentInChildren<PickupObject>();
					pickupObject.PlayerID = 0;
					pickupObject.Id = 0;

					PoolOfGO.Release(instance.GO);
					_activeIDs.Remove(instance.ID);
					_instances.RemoveAt(i);
				}
			}
		}

		for (int id = 0; id < NWArrows.Length; id++)
		{
			if (!_activeIDs.Contains(id))
			{
				var arrow = NWArrows[id];
				if (arrow.IsAlive)
				{
					var instanceGO = PoolOfGO.Fetch(_prefab, parent: transform);
					var pickupObject = instanceGO.GetComponentInChildren<PickupObject>();
					pickupObject.PlayerID = arrow.PlayerID;
					pickupObject.Id = id + 1;

					_activeIDs.Add(id);
					_instances.Add(new Instance {
						ID = id,
						GO = instanceGO,
					});

					instanceGO.transform.position = (Vector2)arrow.InitPosition;
				}
			}
		}
	}

	private struct NSArrow : INetworkStruct
	{
		public PlayerID PlayerID;
		public NetworkBool IsAlive;
		public Vector2Compressed InitPosition;
	}

	private struct Instance
	{
		public int ID;
		public GameObject GO;
	}
}

}
