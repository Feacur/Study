using UnityEngine;
using UnityEngine.InputSystem;

public class GameCursor : MonoBehaviour
{
	public static GameCursor Instance { get; private set; }

	[SerializeField] SpriteRenderer _visuals;

	private static Rect ScreenRect => new Rect(Vector2.zero, new Vector2(Screen.width - 1, Screen.height - 1));
	private static bool InBounds(Vector2 point) => ScreenRect.Contains(point);

	void Awake()
	{
		Instance = this;
		SetConfined(false);
	}

	void OnDestroy()
	{
		SetConfined(false);
		if (Instance == this)
			Instance = null;
	}

	void Update()
	{
		var mouse = Mouse.current;
		var camera = GameCameraRig.Instance.Camera;

		var screenPosition = mouse.position.ReadValue(); var z = -camera.transform.position.z;
		var worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, z));
		transform.position = new Vector3(worldPosition.x, worldPosition.y);

		var leftButtonPressed = mouse.leftButton.IsPressed();
		_visuals.transform.localScale = leftButtonPressed
			? Vector3.one * 0.5f
			: Vector3.one;

	#if UNITY_EDITOR
		if (Cursor.lockState == CursorLockMode.None)
			SetCustomVisible(InBounds(screenPosition));
	#endif
	}

	public void SetConfined(bool state)
	{
		SetCustomVisible(state);
		Cursor.lockState = state
			? CursorLockMode.Confined
			: CursorLockMode.None;
	}

	private void SetCustomVisible(bool state)
	{
		Cursor.visible = !state;
		_visuals.enabled = state;
	}
}
