using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Study.PhotonKCC
{
	public class EntryPoint : MonoBehaviour
	{
		// @fixme account for scripts reloading, find object and cache
		public static EntryPoint Instance { get; private set; }

		[SerializeField] GameObject[] _defaultObjects;
		[SerializeField] Button _switchPresenceButton;
		[SerializeField] TMP_Text _switchPresenceText;
		[SerializeField] Button _pushSnapshotButton;

		void Awake()
		{
			Instance = this;
			_switchPresenceButton.onClick.AddListener(SwitchPresence);
			_pushSnapshotButton.onClick.AddListener(Network.PushSnapshot);
			UpdatePushSnapshotButton(false);
		}

		void OnDestroy()
		{
			_switchPresenceButton.onClick.RemoveListener(SwitchPresence);
			_pushSnapshotButton.onClick.RemoveListener(Network.PushSnapshot);
			if (Instance == this)
				Instance = null;
		}

		void Start()
		{
			ChangeMenuVisibility(true);
			_switchPresenceText.text = "start";
		}

		void Update()
		{
			var keyboard = Keyboard.current;
			if (keyboard.escapeKey.wasPressedThisFrame)
				ChangeMenuVisibility(!_switchPresenceButton.gameObject.activeSelf);
		}

		public void SpawnDefaultObjects()
		{
			// @note it's meant to instantiate a NetworkRunner et al here
			foreach (var it in _defaultObjects)
				Instantiate(it, new InstantiateParameters {
					parent = transform,
				});
		}

		public void UpdatePushSnapshotButton(bool state)
		{
			_pushSnapshotButton.gameObject.SetActive(state);
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
					await Network.Shutdown();
					await UniTask.WaitWhile(() => Player.Local);
					_switchPresenceText.text = "login";
				}
				else
				{
					await Network.StartGame();
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
