using System;
using System.Collections.Generic;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Components;
using UnityEngine;
using UObject = UnityEngine.Object;
using ID = System.Int32; // @note should be the same type as `Object.GetInstanceID()`
using DevAssert = UnityEngine.Assertions.Assert;
using POQueue = System.Collections.Generic.Queue<StudyPhotonBare.Components.PoolObject>;


namespace StudyPhotonBare.Services
{

public sealed class PoolOfGOService : IService
	, IEBSInitializeable
	, IDisposable
{
	private GameObject _root;
	private readonly Dictionary<ID, POQueue> _instances = new();

	public PoolOfGOService() => EventBus.Subscribe(this);

	void IEBSInitializeable.Initialize()
	{
		EventBus.Unsubscribe<IEBSInitializeable>(this);
		_root = new GameObject($"{nameof(PoolOfGOService)} root");
		_root.SetActive(false);
		UObject.DontDestroyOnLoad(_root);
	}

	void IDisposable.Dispose()
	{
		foreach (var (_, queue) in _instances)
			queue.Clear();
		_instances.Clear();
		UObject.Destroy(_root);
	}

	public void Warmup(GameObject prefab, int count)
	{
		var pooled = prefab.GetComponent<PoolObject>();
		DevAssert.IsNotNull(pooled);
		var queue = GetQueue(pooled);
		while (queue.Count < count)
		{
			var instance = pooled.Instantiate(parameters: new InstantiateParameters {
				parent = _root.transform,
			});
			queue.Enqueue(instance);
		}
	}

	public GameObject Fetch(GameObject prefab, Transform parent = null)
	{
		var pooled = prefab.GetComponent<PoolObject>();
		DevAssert.IsNotNull(pooled);
		if (pooled)
		{
			var ret = FetchInternal();
			return ret;
		}
		else
		{ // fallback
			var ret = UObject.Instantiate(prefab, parameters: new InstantiateParameters {
				parent = parent,
			});
			return ret;
		}

		GameObject FetchInternal()
		{
			var queue = GetQueue(pooled);
			if (queue.Count > 0)
			{
				var ret = queue.Dequeue();
				ret.transform.SetParent(parent, worldPositionStays: false);
				return ret.gameObject;
			}
			else
			{
				var ret = pooled.Instantiate(parameters: new InstantiateParameters {
					parent = parent,
					scene = parent
						? parent.gameObject.scene
						: _root.scene,
				});
				return ret.gameObject;
			}
		}
	}

	public void Release(GameObject instance)
	{
		var pooled = instance.GetComponent<PoolObject>();
		DevAssert.IsNotNull(pooled);
		if (pooled)
		{
			var queue = GetQueue(pooled);
			instance.transform.SetParent(_root.transform, worldPositionStays: false);
			queue.Enqueue(pooled);
		}
		else
		{ // fallback
			UObject.Destroy(instance);
		}
	}

	private POQueue GetQueue(PoolObject pooled)
	{
		var prefab = pooled.Prefab;
		var id = prefab.GetInstanceID();

		if (!_instances.TryGetValue(id, out var queue))
			_instances.Add(id, queue = new POQueue());

		return queue;
	}
}

}
