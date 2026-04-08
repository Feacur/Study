using System;
using System.Collections.Generic;
using StudyPhotonBare.Interfaces;


public static class EventBus
{
	private static readonly Type ISubscriberType = typeof(IEventBusSubscriber);

	private static readonly Subscriptions global = new Subscriptions();
	private static readonly Dictionary<object, Subscriptions> tagged = new Dictionary<object, Subscriptions>();

	public static void Reset()
	{
		global.Reset();
		foreach (var (_, local) in tagged)
			local.Reset();
	}

	public static void Subscribe<T>(T instance) where T : IEventBusSubscriber
	{
		global.Subscribe(instance);
		foreach (var (_, local) in tagged)
			local.Subscribe(instance);
	}

	public static void Unsubscribe<T>(T instance) where T : IEventBusSubscriber
	{
		global.Unsubscribe(instance);
		foreach (var (_, local) in tagged)
			local.Unsubscribe(instance);
	}

	public static void Raise<T>(Action<T> action) where T : IEventBusSubscriber
		=> global.Raise(action);

	public static void SubscribeTagged<T>(object tag, T instance) where T : IEventBusSubscriber
	{
		if (!tagged.TryGetValue(tag, out var subs))
			tagged.Add(tag, subs = new Subscriptions());
		subs.Subscribe(instance);
	}

	public static void UnsubscribeTagged<T>(object tag, T instance) where T : IEventBusSubscriber
	{
		if (tagged.TryGetValue(tag, out var local))
		{
			local.Unsubscribe(instance);
			if (local.Count == 0)
				tagged.Remove(tag);
		}
	}

	public static void RaiseTagged<T>(object tag, Action<T> action) where T : IEventBusSubscriber
	{
		if (tagged.TryGetValue(tag, out var local))
			local.Raise(action);
	}

	private static IEnumerable<Type> GetInterfaces<T>() where T : IEventBusSubscriber
	{
		var type = typeof(T);
		var interfaces = type.GetInterfaces();
		foreach (var it in interfaces)
		{
			if (it == ISubscriberType) continue;
			if (!ISubscriberType.IsAssignableFrom(it)) continue;
			yield return it;
		}
	}

	private class Subscriptions
	{
		// @todo maybe pool collections
		private readonly Dictionary<Type, List<IEventBusSubscriber>> _instances = new Dictionary<Type, List<IEventBusSubscriber>>();

		public int Count => _instances.Count;

		public void Reset()
		{
			foreach (var (_, listeners) in _instances)
				listeners.Clear();
			_instances.Clear();
		}

		public void Subscribe<T>(T instance) where T : IEventBusSubscriber
		{
			var interfaces = GetInterfaces<T>();
			foreach (var type in interfaces)
			{
				if (!_instances.TryGetValue(type, out var instances))
					_instances.Add(type, instances = new List<IEventBusSubscriber>());
				instances.Add(instance);
			}
		}

		public void Unsubscribe<T>(T instance) where T : IEventBusSubscriber
		{
			var interfaces = GetInterfaces<T>();
			foreach (var type in interfaces)
			{
				if (!_instances.TryGetValue(type, out var listeners))
					_instances.Add(type, listeners = new List<IEventBusSubscriber>());
				listeners.Remove(instance);
				if (listeners.Count == 0)
					_instances.Remove(type);
			}
		}

		public void Raise<T>(Action<T> action) where T : IEventBusSubscriber
		{
			var type = typeof(T);
			if (!_instances.TryGetValue(type, out var listeners))
				return;
			// @todo make sure no one changes the iterated list
			foreach (var instance in listeners)
			{
				var typedInstance = (T)instance;
				action(typedInstance);
			}
		}
	}
}
