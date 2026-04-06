using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class AvatarNB : NetworkBehaviour
	, IBeforeUpdate
{
	[Header("Systems")]
	[SerializeField] ArrowsNB _arrows;

	[Header("Logics")]
	[SerializeField] float _speed = 10;

	[Header("Visuals")]
	[SerializeField] float _cameraOffset = 10;
	[SerializeField] float _crAimSpeed = 5;
	[SerializeField] TMP_Text _lifetimeLabel;
	[SerializeField] Transform _aimTransform;

	[Header("Networked")]
	[Networked, OnChangedRender(nameof(NWLifetimeCR))] int NWLifetime { get; set; }
	[Networked] Vector2 NWAim { get; set; }

	private bool _inputIsConsumed;
	private InputData _inputAccumulated;
	private Vector3 _cameraSmoothDamp;
	private ChangeDetector _changeDetector;

	public override void Spawned()
	{
		_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
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
		if (HasStateAuthority)
		{
			NWLifetime += 1;
		}

		if (GetInput(out InputData input))
		{
			input.Normalize();

			if (input.aim.x != 0 && input.aim.y != 0)
			{
				NWAim = input.aim;
			}

			{
				var deltaMove = input.move * (_speed * Runner.DeltaTime);
				transform.position += Translate2D(deltaMove);
			}

			// @note spawn authority belongs either to the host/server in the corresponding mode
			// or to the local player in shared mode
			if (HasStateAuthority && input.buttons.IsSet(InputData.ACTION_ATTACK))
			{ // player would expect to shoot at where they've aimed; either before or after transform changes
				transform.GetPositionAndRotation(out var avatarPosition, out var _);
				var direction = Translate2D(NWAim);
				var position = avatarPosition + direction;
				_arrows.Spawn(position: position, direction: direction);
			}
		}
	}

	public override void Render()
	{
		// var interpolator = new NetworkBehaviourBufferInterpolator(this);
		// foreach (var change in _changeDetector.DetectChanges(this))
		// {
		// 	switch (change)
		// 	{
		// 		case nameof(NWLifetime): NWLifetimeCR(); break;
		// 	}
		// }

		{
			var target = Translate2D(NWAim * 0.5f);
			var distance = _crAimSpeed * Time.unscaledDeltaTime;
			_aimTransform.localPosition = Vector3.MoveTowards(_aimTransform.localPosition, target, distance);
		}

		if (HasInputAuthority)
		{
			var rig = GameCameraRig.Instance;
			var centerOffset = GameCursor.Instance.GetCenterOffsetRelative();
			var targetPosition = transform.position + Translate2D(centerOffset * _cameraOffset);
			rig.transform.position = Vector3.SmoothDamp(rig.transform.position, targetPosition, ref _cameraSmoothDamp, Time.unscaledDeltaTime);
		}
	}

	void IBeforeUpdate.BeforeUpdate()
	{
		if (HasInputAuthority && !EntryPoint.Instance.IsMenuVisible)
		{
			if (_inputIsConsumed)
			{
				_inputIsConsumed = false;
				_inputAccumulated = default;
			}

			if (Cursor.lockState != CursorLockMode.None)
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
					var aim = GameCursor.Instance.transform.position - transform.position;
					_inputAccumulated.aim = aim;
				}
			}
		}
	}

	private void NWLifetimeCR() => 
		_lifetimeLabel.text = (NWLifetime / 10).ToString();

	private Vector3 Translate2D(Vector2 input) =>
		new Vector3(input.x, input.y, 0);

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
