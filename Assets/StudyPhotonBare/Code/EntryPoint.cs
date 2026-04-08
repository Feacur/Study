#pragma warning disable IDE0130 // Namespace does not match folder structure

using StudyPhotonBare.Pooling;
using StudyPhotonBare.Services;
using StudyPhotonBare.Tools;
using UnityEngine;

namespace StudyPhotonBare.Root
{

public static class EntryPoint
{
	[RuntimeInitializeOnLoadMethod]
	private static void RuntimeInitializeOnLoad()
	{
		Application.quitting += OnQuit;
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;

		PoolOfGameObjects.Init();

		ServiceLocator.Set(new ResourcesService());
		ServiceLocator.Set(new NetworkService());
	}

	private static void OnQuit()
	{
		Application.quitting -= OnQuit;
		PoolOfGameObjects.Reset();
		ServiceLocator.Reset();
		EventBus.Reset();
	}
}

}
