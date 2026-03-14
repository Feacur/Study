using UnityEngine;

public class CameraController : MonoBehaviour
{
	public static CameraController Instance { get; private set; }

	[HideInInspector] public Transform target;

	void Awake()
	{
		Instance = this;
		enabled = false;
	}

	void OnDestroy()
	{
		Instance = null;
	}

	void LateUpdate()
	{
		if (target)
		{
			transform.SetPositionAndRotation(target.position, target.rotation);
		}
	}
}
