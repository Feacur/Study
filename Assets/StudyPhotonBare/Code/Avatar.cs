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

	[Networked] private int Lifetime { get; set; }

	private bool _inputIsConsumed;
	private InputData _inputAccumulated;
	private ChangeDetector _changeDetector;
	private float Speed => 10 * Time.unscaledDeltaTime;

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
		if (GetInput(out InputData input))
		{
			Lifetime += 1;
			var move = new Vector3(input.move.x, input.move.y, 0);
			transform.position += move;
		}
	}

	public override void Render()
	{
		foreach (var change in _changeDetector.DetectChanges(this))
		{
			switch (change)
			{
				case nameof(Lifetime):
					UpdateLifetimeLabel();
					break;
			}
		}
	}

	void IBeforeUpdate.BeforeUpdate()
	{
		if (_inputIsConsumed)
		{
			_inputIsConsumed = false;
			_inputAccumulated = default;
		}

		if (Cursor.lockState != CursorLockMode.None)
		{
			var keyboard = Keyboard.current;
			var moveFrame = Vector2.zero;
			moveFrame.x += keyboard.dKey.isPressed ? Speed : 0;
			moveFrame.x -= keyboard.aKey.isPressed ? Speed : 0;
			moveFrame.y += keyboard.wKey.isPressed ? Speed : 0;
			moveFrame.y -= keyboard.sKey.isPressed ? Speed : 0;
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
	}
}
