using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class Avatar : NetworkBehaviour
	, IBeforeUpdate
{
	[SerializeField] TMP_Text _lifetimeLabel;

	[Networked, OnChangedRender(nameof(UpdateLifetimeLabel))] private int Lifetime { get; set; }

	private bool _inputIsConsumed;
	private InputData _inputAccumulated;
	// private ChangeDetector _changeDetector; // @note alternatively use `[OnChangedRender]`

	public override void Spawned()
	{
		// _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
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
			Lifetime += 1;
			input.Normalize();
			input.move *= 10 * Runner.DeltaTime;
			transform.position += new Vector3(input.move.x, input.move.y);
		}
	}

	// public override void Render()
	// {
	// 	// var interpolator = new NetworkBehaviourBufferInterpolator(this);
	// 	// foreach (var change in _changeDetector.DetectChanges(this))
	// 	// {
	// 	// 	switch (change)
	// 	// 	{
	// 	// 		case nameof(Lifetime):
	// 	// 			UpdateLifetimeLabel();
	// 	// 			break;
	// 	// 	}
	// 	// }
	// }

	void IBeforeUpdate.BeforeUpdate()
	{
		if (!HasInputAuthority) return;

		if (_inputIsConsumed)
		{
			_inputIsConsumed = false;
			_inputAccumulated = default;
		}

		if (Cursor.lockState != CursorLockMode.None)
		{
			var keyboard = Keyboard.current;
			var moveFrame = Vector2.zero;
			moveFrame.x += keyboard.dKey.isPressed ? 1 : 0;
			moveFrame.x -= keyboard.aKey.isPressed ? 1 : 0;
			moveFrame.y += keyboard.wKey.isPressed ? 1 : 0;
			moveFrame.y -= keyboard.sKey.isPressed ? 1 : 0;
			_inputAccumulated.move += moveFrame;
		}
	}

	private void UpdateLifetimeLabel() => 
		_lifetimeLabel.text = (Lifetime / 10).ToString();

	private void InputConsume(NetworkRunner runner, NetworkInput input)
	{
		_inputIsConsumed = true;
		input.Set(_inputAccumulated);
	}

	private struct InputData : INetworkInput
	{
		public Vector2 move;

		public void Normalize()
		{
			move.Normalize();
		}
	}
}
