#pragma warning disable IDE0130 // Namespace does not match folder structure

using StudyPhotonBare.Interfaces;
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
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;

		ServiceLocator.Set(new ResourcesService());
		ServiceLocator.Set(new NetworkService());
		ServiceLocator.Set(new PoolGOService());

		// @note some nuances
		// - should only be called once
		// - ... meaning all the services are registered at the very start, as it is meant to
		// - ... or services should ignore concecutive messages
		// - ... or partially unsubscribe themselves from the bus
		EventBus.Raise<IEBSInitializeable>(it => it.Initialize());

	#if UNITY_EDITOR // cleanup everything for
		UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
		static void PlayModeStateChanged(UnityEditor.PlayModeStateChange state)
		{
			if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
			UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
			ServiceLocator.Reset();
			EventBus.Reset();
		}
	#endif
	}

}

}
