using System;
using System.Collections.Generic;
using StudyPhotonBare.Interfaces;

public static class EventBus
{
	private static readonly Type ISubscriberType = typeof(IEventBusSubscriber);

	// @todo maybe pool collections
	private static readonly Dictionary<Type, List<IEventBusSubscriber>> _subscribers = new Dictionary<Type, List<IEventBusSubscriber>>();

	public static void Reset()
	{
		foreach (var (_, instance) in _subscribers)
			instance.Clear();
		_subscribers.Clear();
	}

	public static void Subscribe<T>(T instance) where T : IEventBusSubscriber
	{
		var interfaces = GetInterfaces<T>();
		foreach (var type in interfaces)
		{
			if (!_subscribers.TryGetValue(type, out var instances))
				_subscribers.Add(type, instances = new List<IEventBusSubscriber>());
			instances.Add(instance);
		}
	}

	public static void Unsubscribe<T>(T instance) where T : IEventBusSubscriber
	{
		var interfaces = GetInterfaces<T>();
		foreach (var type in interfaces)
		{
			if (!_subscribers.TryGetValue(type, out var instances))
				_subscribers.Add(type, instances = new List<IEventBusSubscriber>());
			instances.Remove(instance);
			if (instances.Count == 0)
				_subscribers.Remove(type);
		}
	}

	public static void Raise<T>(Action<T> action) where T : IEventBusSubscriber
	{
		var type = typeof(T);
		if (!_subscribers.TryGetValue(type, out var instances))
			return;
		// @todo make sure no one changes the iterated list
		foreach (var instance in instances)
		{
			var typedInstance = (T)instance;
			action(typedInstance);
		}
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
}
