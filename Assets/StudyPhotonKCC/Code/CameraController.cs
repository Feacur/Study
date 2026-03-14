using UnityEngine;


namespace Study.PhotonKCC
{
	public class CameraController : MonoBehaviour
	{
		// @fixme account for scripts reloading, find object and cache
		public static CameraController Instance { get; private set; }

		[HideInInspector] public Transform target;

		void Awake()
		{
			Instance = this;
			enabled = false;
		}

		void OnDestroy()
		{
			if (Instance == this)
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
}
