using System;
using Fusion;


namespace StudyPhotonBare.Tools
{

public static class Utils
{
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
