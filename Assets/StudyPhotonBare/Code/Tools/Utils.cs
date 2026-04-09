using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;


namespace StudyPhotonBare.Tools
{

public static class Utils
{
	public static Vector2 ScreenSize => new(Screen.width, Screen.height);
	public static Rect ScreenRect => new(Vector2.zero, ScreenSize);
	public static bool InBounds(Vector2 point) => ScreenRect.Contains(point);

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

	public static int GetPlayerID(byte[] token)
		=> new Guid(token).GetHashCode();

	public static bool CanManagePlayer(NetworkRunner runner, PlayerRef player)
		=> runner.Topology == Topologies.Shared && runner.LocalPlayer == player
		|| runner.IsServer;

	public static bool CanActWithAuthority(NetworkRunner runner)
		=> runner.IsSharedModeMasterClient
		|| runner.IsServer;

	public static int FindFirstIndex<T>(this NetworkArray<T> container, Predicate<T> predicate)
	{
		for (int i = 0; i < container.Length; i++)
		{
			var it = container[i];
			if (predicate.Invoke(it))
				return i;
		}
		return -1;
	}

	public static int FindLastIndex<T>(this NetworkArray<T> container, Predicate<T> predicate)
	{
		for (int i = container.Length - 1; i >= 0; i--)
		{
			var it = container[i];
			if (predicate.Invoke(it))
				return i;
		}
		return -1;
	}

	public static int Count<T>(this NetworkArray<T> container, Predicate<T> predicate)
	{
		int count = 0;
		for (int i = container.Length - 1; i >= 0; i--)
		{
			var it = container[i];
			if (predicate.Invoke(it))
				count += 1;
		}
		return count;
	}
}

}
