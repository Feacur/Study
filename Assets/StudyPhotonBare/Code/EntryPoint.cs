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
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;

		ServiceLocator.Set(new ResourcesService());
		ServiceLocator.Set(new NetworkService());

		PoolOfGameObjects.Init();
		// @todo init via the `EventBus` ?
		// is not appropriate for statics; but they can be singletons then (or services?)

	#if UNITY_EDITOR // cleanup everything for
		UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
		static void PlayModeStateChanged(UnityEditor.PlayModeStateChange state)
		{
			if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
			UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
			// @todo reset via the `EventBus` ?
			// then make sure it's the last and doesn't pool anything
			EventBus.Reset();
			ServiceLocator.Reset();
			PoolOfGameObjects.Reset();
		}
	#endif
	}

}

}
