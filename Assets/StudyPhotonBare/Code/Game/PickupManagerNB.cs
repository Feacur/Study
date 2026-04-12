using System;
using System.Collections.Generic;
using Fusion;
using StudyPhotonBare.Components;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Root;
using StudyPhotonBare.Services;
using StudyPhotonBare.Tools;
using UnityEngine;
using PlayerID = System.Int32;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class PickupManagerNB : NetworkBehaviour
	, IEBSDropListener
	, IEBSPickupListener
	, IEBSPlayerLeftListener
	, IEBSNetworkMigrationListener
{
	[Header("Logics")] // @todo CMS
	[SerializeField] ContactFilter2D _contactFilter;

	[Header("Visuals")]
	[SerializeField] GameObject _prefab; // @todo CMS

	[Header("Networked")]
	[Networked, Capacity(Constants.ArrowsLimit), OnChangedRender(nameof(NWArrowsCR))] NetworkArray<NSArrow> NWArrows { get; }

	[Header("Private")]
	private readonly HashSet<int> _activeIDXs = new();
	private readonly List<Instance> _instances = new();

	[Header("Accessors")]
	private PoolGOService PoolOfGO => ServiceLocator.Get<PoolGOService>(); // @todo cache on spawn ?

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

	void IEBSDropListener.OnDropped(PlayerID playerID, Vector2 position)
	{
		var index = NWArrows.FindFirstIndex(it => it.IsDroppable);
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

	void IEBSPlayerLeftListener.OnPlayerLeft(PlayerID playerID)
	{
		for (int idx = 0; idx < NWArrows.Length; idx++)
		{
			var arrow = NWArrows[idx];
			if (arrow.PlayerID == playerID)
				NWArrows.Set(idx, default);
		}
	}

	void IEBSNetworkMigrationListener.OnHostMigrated()
	{
		for (int idx = 0; idx < NWArrows.Length; idx++)
			NWArrows.Set(idx, default);
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
					var pickupObject = instance.GO.GetComponentInChildren<PickupMarker>();
					pickupObject.PlayerID = 0;
					pickupObject.ID = 0;

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
					var instanceGO = PoolOfGO.Fetch(_prefab, parent: transform);
					var pickupObject = instanceGO.GetComponentInChildren<PickupMarker>();
					pickupObject.PlayerID = arrow.PlayerID;
					pickupObject.ID = idx + 1;

					_activeIDXs.Add(idx);
					_instances.Add(new Instance {
						ID = idx,
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

		public bool IsDroppable => !IsAlive;
	}

	private struct Instance
	{
		public int ID;
		public GameObject GO;
	}
}

}
