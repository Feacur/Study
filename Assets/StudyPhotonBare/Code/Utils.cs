using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace StudyPhotonBare.Tools
{

public static class Utils
{
	public static Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);
	public static Rect ScreenRect => new Rect(Vector2.zero, ScreenSize);
	public static bool InBounds(Vector2 point) => ScreenRect.Contains(point);

	public static Vector3 Translate2D(Vector2 input) => new Vector3(input.x, input.y, 0);

	public static bool IsPrefab(this GameObject go) => go.scene.rootCount == 0;

	public static async void CatchCancel(this Task task)
	{
		// @note no much purpose here, minimally mimics `UniTask`'s `.Forget` extension,
		// specifically catches async exceptions instead of abandoning them
		// to the void of a scheduler
		try
		{
			await task;
		}
		catch (OperationCanceledException ex)
		{
			Debug.Log("CatchCancel: " + ex.Message);
		}
	}

	public static bool CanSpawn(NetworkRunner runner, PlayerRef player)
		=> runner.IsServer
		|| runner.Topology == Topologies.Shared
		&& runner.LocalPlayer == player;
}

}
