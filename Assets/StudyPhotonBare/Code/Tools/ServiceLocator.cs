
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
	private static readonly Dictionary<Type, IService> _instances = new Dictionary<Type, IService>();

	public static T Get<T>() where T : IService
	{
		var type = typeof(T);
		_instances.TryGetValue(type, out var ret);
		return (T)ret;
	}

	public static void Set<T>(T instance) where T : IService
	{
		var type = typeof(T);
		_instances[type] = instance;
	}
}

}
