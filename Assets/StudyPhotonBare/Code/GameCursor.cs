using UnityEngine;
using UnityEngine.InputSystem;

public class GameCursor : MonoBehaviour
{
	public static GameCursor Instance { get; private set; }

	[Header("Visuals")]
	[SerializeField] SpriteRenderer _visuals;

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
			SetCustomVisible(Utils.InBounds(screenPosition));
	#endif
	}

	public void SetConfined(bool state)
	{
		SetCustomVisible(state);
		Cursor.lockState = state
			? CursorLockMode.Confined
			: CursorLockMode.None;
	}

	public Vector2 GetCenterOffsetRelative()
	{
		var mouse = Mouse.current;
		var screenSize = Utils.ScreenSize;
		var position = mouse.position.ReadValue();
		var offset = position - screenSize / 2;
		return new Vector2(
			Mathf.Clamp(offset.x / screenSize.x, -0.5f, 0.5f),
			Mathf.Clamp(offset.y / screenSize.y, -0.5f, 0.5f)
		);
	}

	private void SetCustomVisible(bool state)
	{
		Cursor.visible = !state;
		_visuals.enabled = state;
	}
}
