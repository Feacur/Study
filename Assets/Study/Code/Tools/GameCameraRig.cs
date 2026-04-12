using UnityEngine;


namespace Study.Tools
{

public class GameCameraRig : MonoBehaviour
{
	public static GameCameraRig Instance { get; private set; }

	public Camera Camera => _camera;
	public float ZOffset => -_camera.transform.localPosition.z;

	[Header("Visuals")]
	[SerializeField] Camera _camera;

	void Awake()
	{
		Instance = this;
	}

	void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
}

}
