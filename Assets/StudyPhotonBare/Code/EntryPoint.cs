#pragma warning disable IDE0130 // Namespace does not match folder structure

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
	}
}

}
