using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public static class Utils
{
	public static Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);
	public static Rect ScreenRect => new Rect(Vector2.zero, ScreenSize);
	public static bool InBounds(Vector2 point) => ScreenRect.Contains(point);

	public static async UniTaskVoid RunSafeUniTask(UniTask task)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException ex)
		{
			Debug.Log("RunTaskSafe: " + ex.Message);
		}
	}

	public static async UniTaskVoid RunSafeUniTask<T>(UniTask<T> task)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException ex)
		{
			Debug.Log("RunTaskSafe: " + ex.Message);
		}
	}

	public static bool CanSpawn(NetworkRunner runner, PlayerRef player)
		=> runner.IsServer
		|| runner.Topology == Topologies.Shared
		&& runner.LocalPlayer == player;
}
