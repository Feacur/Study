using StudyPhotonBare.Interfaces;
using StudyPhotonBare.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


namespace StudyPhotonBare.Root
{

public class ConnectionMenu : MonoBehaviour
	, INetworkListener
{
	public static ConnectionMenu Instance { get; private set; }

	[Header("Visuals (external)")]
	[SerializeField] Button _networkButton;
	[SerializeField] TMP_Text _networkText;

	[Header("Accessors")]
	private bool _connectionStatus;

	[Header("Accessors")]
	private GameCursor Cursor => GameCursor.Instance;
	public bool IsMenuVisible => _networkButton.gameObject.activeSelf;

	void Awake()
	{
		Instance = this;
		EventBus.Subscribe(this);
		_networkButton.onClick.AddListener(NetworkToggle);
		_networkButton.gameObject.SetActive(true);
		_networkText.text = "network";
	}

	void OnDestroy()
	{
		EventBus.Unsubscribe(this);
		_networkButton.onClick.RemoveListener(NetworkToggle);
		if (Instance == this)
			Instance = null;
	}

	void Update()
	{
		var keyboard = Keyboard.current;
		if (_connectionStatus && keyboard.escapeKey.wasPressedThisFrame) {
			SetMenuVisible(!IsMenuVisible);
		}
	}

	void INetworkListener.OnStatusChanged(bool status)
	{
		_connectionStatus = status;
		SetMenuVisible(!status);
		_networkText.text = status ? "shutdown" : "start";
		_networkButton.interactable = true;
	}

	void INetworkListener.OnLocalToken(byte[] token) { /*dummy*/ }

	private void NetworkToggle()
	{
		_networkButton.interactable = false;
		_networkText.text = "...";
		EventBus.Raise<INetworkControl>(it => it.ToggleStatus());
	}

	private void SetMenuVisible(bool state)
	{
		_networkButton.gameObject.SetActive(state);
		Cursor.SetConfined(!state);
	}
}

}
