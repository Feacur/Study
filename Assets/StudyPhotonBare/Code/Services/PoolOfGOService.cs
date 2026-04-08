using System;
using System.Collections.Generic;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Pooling;
using UnityEngine;
using UnityEngine.Assertions;
using ID = System.Int32; // should be the same type as `Object.GetInstanceID()`
using Object = UnityEngine.Object;

namespace StudyPhotonBare.Services
{

public sealed class PoolOfGOService : IService
	, IDisposable
{
	private GameObject _root;
	private readonly Dictionary<ID, Queue<PooledGameObject>> _instances = new Dictionary<ID, Queue<PooledGameObject>>();

	public PoolOfGOService() => EventBus.Subscribe(this);

	void IService.Initialize()
	{
		_root = new GameObject($"{nameof(PoolOfGOService)} root");
		_root.SetActive(false);
		Object.DontDestroyOnLoad(_root);
	}

	void IDisposable.Dispose()
	{
		foreach (var (_, queue) in _instances)
			queue.Clear();
		_instances.Clear();
		Object.Destroy(_root);
	}

	public void WarmupAdditive(GameObject prefab, byte count)
	{
		var pooled = prefab.GetComponent<PooledGameObject>();
		if (pooled)
		{
			var queue = GetQueue(pooled);
			for (byte i = 0; i < count; i++)
			{
				var instance = pooled.Instantiate(parameters: new InstantiateParameters {
					parent = _root.transform,
				});
				queue.Enqueue(instance);
			}
		}
		else Assert.IsTrue(false); // dev
	}

	public GameObject Get(GameObject prefab, Transform parent = null)
	{
		var pooled = prefab.GetComponent<PooledGameObject>();
		if (pooled)
		{
			PooledGameObject ret;
			var queue = GetQueue(pooled);
			if (queue.Count > 0)
			{
				ret = queue.Dequeue();
				ret.transform.SetParent(parent);
			}
			else ret = pooled.Instantiate(parameters: new InstantiateParameters {
				parent = parent,
				scene = parent
					? parent.gameObject.scene
					: _root.scene,
			});
			return ret.gameObject;
		}
		else
		{
			Assert.IsTrue(false); // dev
			return Object.Instantiate(prefab, parameters: new InstantiateParameters {
				parent = parent,
			});
		}
	}

	public void Ret(GameObject instance)
	{
		var pooled = instance.GetComponent<PooledGameObject>();
		if (pooled)
		{
			var queue = GetQueue(pooled);
			instance.transform.SetParent(_root.transform);
			queue.Enqueue(pooled);
		}
		else
		{
			Assert.IsTrue(false); // dev
			Object.Destroy(instance);
		}
	}

	private Queue<PooledGameObject> GetQueue(PooledGameObject pooled)
	{
		var prefab = pooled.Prefab;
		var id = prefab.GetInstanceID();

		if (!_instances.TryGetValue(id, out var queue))
			_instances.Add(id, queue = new Queue<PooledGameObject>());

		return queue;
	}
}

}
