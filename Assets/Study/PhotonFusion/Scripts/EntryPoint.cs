using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Study.PhotonFusion
{
	public class EntryPoint : MonoBehaviour
	{
		[SerializeField] GameObject[] _defaultObjects;
		[SerializeField] Button _switchPresenceButton;
		[SerializeField] TMP_Text _switchPresenceText;

		void Awake()
		{
			ChangeMenuVisibility(true);
			_switchPresenceButton.onClick.AddListener(SwitchPresence);
			_switchPresenceText.text = "start";
		}

		void OnDestroy()
		{
			_switchPresenceButton.onClick.RemoveListener(SwitchPresence);
		}

		void Update()
		{
			var keyboard = Keyboard.current;
			if (keyboard.escapeKey.wasPressedThisFrame)
				ChangeMenuVisibility(!_switchPresenceButton.gameObject.activeSelf);
		}

		private void SwitchPresence()
		{
			Debug.Log($"[Study] {nameof(EntryPoint)}.{nameof(SwitchPresence)} clicked");
			SwitchPresenceAsync().Forget();
			async UniTaskVoid SwitchPresenceAsync()
			{
				Debug.Log($"[Study] {nameof(SwitchPresenceAsync)} started");
				_switchPresenceButton.interactable = false;
				_switchPresenceText.text = "processing...";
				if (Player.Local)
				{
					await Network.Instance.Shutdown();
					await UniTask.WaitWhile(() => Player.Local);
					_switchPresenceText.text = "login";
				}
				else
				{
					// @note it's meant to instantiate a NetworkRunner et al here
					foreach (var it in _defaultObjects)
						Instantiate(it, new InstantiateParameters {
							parent = transform,
						});

					await Network.Instance.StartGame();
					await UniTask.WaitUntil(() => Player.Local);
					_switchPresenceText.text = "logout";
					ChangeMenuVisibility(false);
				}
				if (_switchPresenceButton) // @todo handle via cancellation ?
					_switchPresenceButton.interactable = true;
				Debug.Log($"[Study] {nameof(SwitchPresenceAsync)} finished");
			}
		}

		private void ChangeMenuVisibility(bool state)
		{
			Cursor.visible = state;
			Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
			_switchPresenceButton.gameObject.SetActive(state);
		}
	}
}
