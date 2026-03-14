using UnityEngine;

public class GameCursor : MonoBehaviour
{
	public static GameCursor Instance;

	void Awake()
	{
		Instance = this;
	}

	void OnDestroy()
	{
		SetState(true);
		if (Instance == this)
			Instance = null;
	}

	public void SetState(bool state)
	{
		Cursor.visible = state;
		Cursor.lockState = state
			? CursorLockMode.None
			: CursorLockMode.Locked;
	}
}
