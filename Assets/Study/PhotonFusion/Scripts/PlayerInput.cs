using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Study.PhotonFusion
{
	public class PlayerInput : NetworkBehaviour
		, IBeforeUpdate
	{
		private bool _inputIsConsumed;
		private Data _accumulated;

		void IBeforeUpdate.BeforeUpdate()
		{
			if (!HasInputAuthority)
				return;

			if (_inputIsConsumed)
			{
				_inputIsConsumed = false;
				_accumulated = default;
			}

			if (Cursor.lockState != CursorLockMode.None)
			{
				var keyboard = Keyboard.current;
				var mouse = Mouse.current;
				//
				_accumulated.move.x += keyboard.dKey.isPressed ? 1 : 0;
				_accumulated.move.x -= keyboard.aKey.isPressed ? 1 : 0;
				_accumulated.move.y += keyboard.wKey.isPressed ? 1 : 0;
				_accumulated.move.y -= keyboard.sKey.isPressed ? 1 : 0;
				//
				var mouseInput = mouse.delta.ReadValue();
				_accumulated.look += new Vector2(-mouseInput.y, mouseInput.x);
				//
				_accumulated.jump |= keyboard.spaceKey.wasPressedThisFrame;
			}
		}

		public void Consume(NetworkRunner runner, NetworkInput input)
		{
			_inputIsConsumed = true;
			_accumulated.move.Normalize();
			input.Set(_accumulated);
		}

		public struct Data : INetworkInput
		{
			public Vector2 move;
			public Vector2 look;
			public bool    jump;
		}
	}
}
