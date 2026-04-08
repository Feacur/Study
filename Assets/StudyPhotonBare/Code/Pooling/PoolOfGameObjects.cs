using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using ID = System.Int32; // should be the same type as `Object.GetInstanceID()`


namespace StudyPhotonBare.Pooling
{

public static class PoolOfGameObjects
{
	private static GameObject _root;
	private static readonly Dictionary<ID, Queue<PooledGameObject>> _instances = new Dictionary<ID, Queue<PooledGameObject>>();

	public static void Init()
	{
		_root = new GameObject($"{nameof(PoolOfGameObjects)} root");
		_root.SetActive(false);
		Object.DontDestroyOnLoad(_root);
	}

	public static void Reset()
	{
		foreach (var it in _instances)
			it.Value.Clear();
		_instances.Clear();
	}

	public static void WarmupAdditive(GameObject prefab, byte count)
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

	public static GameObject Get(GameObject prefab, Transform parent = null)
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

	public static void Ret(GameObject instance)
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

	private static Queue<PooledGameObject> GetQueue(PooledGameObject pooled)
	{
		var prefab = pooled.Prefab;
		var id = prefab.GetInstanceID();

		if (!_instances.TryGetValue(id, out var queue))
			_instances.Add(id, queue = new Queue<PooledGameObject>());

		return queue;
	}
}

}
