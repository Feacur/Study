using Fusion;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Root;
using StudyPhotonBare.Tools;
using UnityEngine;
using UnityEngine.InputSystem;


namespace StudyPhotonBare.Game
{

[RequireComponent(typeof(NetworkObject))]
public class AvatarControllerNB : NetworkBehaviour
	, IBeforeUpdate
{
	[Header("Logics")] // @todo CMS
	[SerializeField] float _speed = 10;
	[SerializeField] Transform _cameraTarget;

	[Header("Visuals")]
	[SerializeField] float _lookRadius = 1;
	[SerializeField] float _lookSpeed = 100;
	[SerializeField] float _lookInertia = 0.1f;
	[SerializeField] float _crAimSpeed = 5;
	[SerializeField] Transform _aimTransform;

	[Header("Networked")]
	[Networked] Vector2 NWAim { get; set; }

	[Header("Private")]
	private bool _inputIsConsumed;
	private InputData _inputAccumulated;
	private Vector2 _cameraVelocity;

	[Header("Accessors")]
	private ConnectionMenu ConnectionMenu => ConnectionMenu.Instance;
	private bool AreControlsEnabled => HasInputAuthority && !ConnectionMenu.IsMenuVisible;
	private GameCameraRig CameraRig => GameCameraRig.Instance;
	private GameCursor Cursor => GameCursor.Instance;

	public override void Spawned()
	{
		if (HasInputAuthority)
		{
			var events = Runner.GetComponent<NetworkEvents>();
			events.OnInput.AddListener(InputConsume);
		}
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		if (HasInputAuthority)
		{
			var events = Runner.GetComponent<NetworkEvents>();
			events.OnInput.RemoveListener(InputConsume);
		}
	}

	public override void FixedUpdateNetwork()
	{
		if (GetInput(out InputData input))
		{
			input.Normalize();

			if (input.aim.x != 0 && input.aim.y != 0)
			{
				NWAim = input.aim;
			}

			{
				var deltaMove = input.move * (_speed * Runner.DeltaTime);
				var targetPosition = (Vector2)transform.position + deltaMove;
				transform.position = targetPosition;
			}

			if (input.buttons.IsSet(InputData.ACTION_ATTACK))
			{ // player would expect to shoot at where they've aimed; either before or after transform changes
				var position = (Vector2)transform.position + input.aim;
				EventBus.Raise<IEBSShooter>(it => { it.Shoot(position: position, direction: input.aim); }, tag: Object);
			}
		}
	}

	public override void Render()
	{
		{
			var target = NWAim * 0.5f;
			var distance = _crAimSpeed * Time.unscaledDeltaTime;
			_aimTransform.localPosition = Vector2.MoveTowards(_aimTransform.localPosition, target, distance);
		}
		if (AreControlsEnabled)
		{
			var maxOffset = _lookRadius * CameraRig.ZOffset;
			var cursorOffset = Cursor.GetCenterOffsetRelative();
			var cameraOffset = cursorOffset * maxOffset;
			var targetPosition = (Vector2)_cameraTarget.position + cameraOffset;
			CameraRig.transform.position = Vector2.SmoothDamp(current: CameraRig.transform.position,
				target: targetPosition,
				currentVelocity: ref _cameraVelocity,
				smoothTime: _lookInertia,
				maxSpeed: _lookSpeed,
				deltaTime: Time.deltaTime
			);
		}
	}

	void IBeforeUpdate.BeforeUpdate()
	{
		if (AreControlsEnabled)
		{
			if (_inputIsConsumed)
			{
				_inputIsConsumed = false;
				_inputAccumulated = default;
			}

			if (UnityEngine.Cursor.lockState != CursorLockMode.None)
			{
				var keyboard = Keyboard.current;
				var mouse = Mouse.current;

				{
					var moveFrame = Vector2.zero;
					moveFrame.x += keyboard.dKey.isPressed ? 1 : 0;
					moveFrame.x -= keyboard.aKey.isPressed ? 1 : 0;
					moveFrame.y += keyboard.wKey.isPressed ? 1 : 0;
					moveFrame.y -= keyboard.sKey.isPressed ? 1 : 0;
					_inputAccumulated.move += moveFrame;
				}

				if (mouse.leftButton.wasPressedThisFrame)
				{
					_inputAccumulated.buttons.Set(InputData.ACTION_ATTACK, true);
				}

				{
					var aim = Cursor.Position - (Vector2)transform.position;
					_inputAccumulated.aim = aim;
				}
			}
		}
	}

	private void InputConsume(NetworkRunner runner, NetworkInput input)
	{
		_inputIsConsumed = true;
		input.Set(_inputAccumulated);
	}

	private struct InputData : INetworkInput
	{
		public const byte ACTION_ATTACK = 1;

		public Vector2 move;
		public NetworkButtons buttons;
		public Vector2 aim;

		public void Normalize()
		{
			move.Normalize();
			aim.Normalize();
		}
	}
}

}
