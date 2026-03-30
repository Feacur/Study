using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class Utils
{
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
}
