using UnityEngine;


namespace StudyPhotonBare.Tools
{

public class GameCameraRig : MonoBehaviour
{
	public static GameCameraRig Instance { get; private set; }

	public Camera Camera => _camera;

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
