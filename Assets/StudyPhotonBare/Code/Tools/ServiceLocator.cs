
using System;
using System.Collections.Generic;
using StudyPhotonBare.Interfaces;

namespace StudyPhotonBare.Tools
{

public static class ServiceLocator
{
	// @todo store the very basic type instead
	// - getting a value of a static field with name "ServiceType" is a possibility
	// - no chance using static interface properties with the current C# version
	// - just fetch interfaces like with the `EventBus` ?
	private static readonly Dictionary<Type, IService> _instances = new Dictionary<Type, IService>();

	public static void Reset()
	{
		foreach (var (_, instance) in _instances)
			DisposeService(instance);
		_instances.Clear();
	}

	public static T Get<T>() where T : IService
	{
		var type = typeof(T);
		_instances.TryGetValue(type, out var ret);
		return (T)ret;
	}

	public static void Set<T>(T instance) where T : IService
	{
		var type = typeof(T);
		if (_instances.TryGetValue(type, out var existing))
			DisposeService(existing);
		_instances[type] = instance;
	}

	private static void DisposeService(IService instance)
	{
		if (instance is IDisposable disposable)
			disposable.Dispose();
	}
}

}
