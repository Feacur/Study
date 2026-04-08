using StudyPhotonBare.Enums;
using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


namespace StudyPhotonBare.Root
{

public class ConnectionMenu : MonoBehaviour
	, INetworkStatusListener
{
	public static ConnectionMenu Instance { get; private set; }

	[Header("Visuals (external)")]
	[SerializeField] Button _networkButton;
	[SerializeField] TMP_Text _networkText;

	[Header("Private")]
	private NetworkStatus _networkStatus;

	[Header("Accessors")]
	private GameCursor Cursor => GameCursor.Instance;
	public bool IsMenuVisible => _networkButton.gameObject.activeSelf;

	void Awake()
	{
		Instance = this;
		EventBus.Subscribe(this);
		_networkButton.onClick.AddListener(ToggleNetwork);
		_networkButton.gameObject.SetActive(true);
		_networkText.text = "network";
	}

	void OnDestroy()
	{
		EventBus.Unsubscribe(this);
		_networkButton.onClick.RemoveListener(ToggleNetwork);
		if (Instance == this)
			Instance = null;
	}

	void Update()
	{
		var keyboard = Keyboard.current;
		if (CanToggleMenu() && keyboard.escapeKey.wasPressedThisFrame) {
			SetMenuVisible(!IsMenuVisible);
		}
	}

	void INetworkStatusListener.OnNetworkStatus(NetworkStatus status)
	{
		_networkStatus = status;
		switch (status)
		{
			case NetworkStatus.None:
				SetMenuVisible(true);
				_networkText.text = "Connect";
				_networkButton.interactable = true;
				break;

			case NetworkStatus.Running:
				SetMenuVisible(false);
				_networkText.text = "Shutdown";
				_networkButton.interactable = true;
				break;

			case NetworkStatus.Starting:
			case NetworkStatus.Migrating:
			case NetworkStatus.Shutting:
				SetMenuVisible(true);
				_networkText.text = status.ToString();
				_networkButton.interactable = false;
				break;
		}
	}

	private void ToggleNetwork() => EventBus.Raise<INetworkToggler>(it => it.ToggleNetwork());

	private void SetMenuVisible(bool state)
	{
		_networkButton.gameObject.SetActive(state);
		Cursor.SetConfined(!state);
	}

	private bool CanToggleMenu()
	{
		switch (_networkStatus)
		{
			case NetworkStatus.Running:
				return true;
		}
		return false;
	}
}

}
