using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;

namespace Study.PhotonFusion
{
	[RequireComponent(typeof(Rigidbody), typeof(SimpleKCC))]
	public class PlayerController : NetworkBehaviour
	{
		[SerializeField] Transform _cameraPivot;
		[SerializeField] Transform _cameraTarget;
		[SerializeField] float _moveSpeed = 4;
		[SerializeField] float _lookSpeed = 0.1f;
		[SerializeField] float _jumpHeight = 0.8f;

		private PlayerInput Input;
		private SimpleKCC _kcc;

		private static float Gravity => Physics.gravity.y;

		void Awake()
		{
			Input = GetComponent<PlayerInput>();
			_kcc = GetComponent<SimpleKCC>();
		}

		public override void Spawned()
		{
			Debug.Log($"[Study] {nameof(PlayerController)}.{nameof(Spawned)} {(HasInputAuthority ? "local" : "remote")} {Object.InputAuthority}");
			if (HasInputAuthority)
			{
				Network.Instance.Events.OnInput.AddListener(Input.Consume);
				CameraController.Instance.target = _cameraTarget;
				CameraController.Instance.enabled = true;
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			Debug.Log($"[Study] {nameof(PlayerController)}.{nameof(Despawned)} {(HasInputAuthority ? "local" : "remote")} {Object.InputAuthority}");
			if (HasInputAuthority)
			{
				Network.Instance.Events.OnInput.RemoveListener(Input.Consume);
				CameraController.Instance.target = null;
				CameraController.Instance.enabled = false;
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (!GetInput(out PlayerInput.Data input))
				return;

			var moveInput = input.move * _moveSpeed;
			var moveLocal = new Vector3(moveInput.x, 0, moveInput.y);
			var kinematicVelocity = _kcc.TransformRotation * moveLocal;
			var jumpImpulse = input.jump && _kcc.IsGrounded
				? Mathf.Sqrt(2 * Mathf.Abs(Gravity) * _jumpHeight) * _kcc.Rigidbody.mass
				: 0;
			_kcc.SetGravity(Gravity);
			_kcc.Move(kinematicVelocity, jumpImpulse);

			if (_kcc.Position.y < -10)
				_kcc.SetPosition(Vector3.zero);

			var lookInput = input.look * _lookSpeed;
			_kcc.AddLookRotation(lookInput, minPitch: -45, maxPitch: 75);

			var lookRotation = _kcc.GetLookRotation(pitch: true, yaw: false);
			_cameraPivot.localRotation = Quaternion.Euler(lookRotation.x, 0, 0);
		}
	}
}
