using System;
using System.Collections.Generic;
using StudyPhotonBare.Interfaces;
using Interface = System.Type;
using Subscribers = System.Collections.Generic.List<StudyPhotonBare.Interfaces.IEventBusSubscriber>;
using Tag = System.Object;


public static class EventBus
{
	private static readonly Tag NilTagForBroadcasting = new();
	private static readonly Interface BasicInterface = typeof(IEventBusSubscriber);
	private static readonly Dictionary<Tag, Storage> tags = new Dictionary<Tag, Storage>();

	public static void Reset()
	{
		foreach (var (_, storage) in tags)
			storage.Reset();
		tags.Clear();
	}

	public static void Subscribe<T>(T instance, Tag tag = default) where T : IEventBusSubscriber
	{
		tag ??= NilTagForBroadcasting;
		if (!tags.TryGetValue(tag, out var storage))
			tags.Add(tag, storage = new Storage());
		storage.Subscribe(instance);
	}

	public static void Unsubscribe<T>(T instance, Tag tag = default) where T : IEventBusSubscriber
	{
		tag ??= NilTagForBroadcasting;
		if (tags.TryGetValue(tag, out var storage))
		{
			storage.Unsubscribe(instance);
			if (storage.Count == 0)
				tags.Remove(tag);
		}
	}

	public static void Raise<T>(Action<T> action, Tag tag = default) where T : IEventBusSubscriber
	{
		tag ??= NilTagForBroadcasting;
		if (tags.TryGetValue(tag, out var storage))
			storage.Raise(action);
	}

	private static IEnumerable<Interface> GetInterfaces<T>() where T : IEventBusSubscriber
	{
		var input = typeof(T);
		var interfaces = input.GetInterfaces();
		foreach (var @interface in interfaces)
		{
			if (@interface == BasicInterface) continue;
			if (!BasicInterface.IsAssignableFrom(@interface)) continue;
			yield return @interface;
		}
	}

	private class Storage
	{
		// @todo maybe pool collections
		private readonly Dictionary<Interface, Subscribers> _instances = new Dictionary<Interface, Subscribers>();

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
			foreach (var @interface in interfaces)
			{
				if (!_instances.TryGetValue(@interface, out var instances))
					_instances.Add(@interface, instances = new Subscribers());
				// @todo allow only one instance per interface ?
				instances.Add(instance);
			}
		}

		public void Unsubscribe<T>(T instance) where T : IEventBusSubscriber
		{
			var interfaces = GetInterfaces<T>();
			foreach (var @interface in interfaces)
			{
				if (!_instances.TryGetValue(@interface, out var listeners))
					_instances.Add(@interface, listeners = new Subscribers());
				listeners.Remove(instance);
				if (listeners.Count == 0)
					_instances.Remove(@interface);
			}
		}

		public void Raise<T>(Action<T> action) where T : IEventBusSubscriber
		{
			var input = typeof(T);
			if (!_instances.TryGetValue(input, out var listeners))
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
