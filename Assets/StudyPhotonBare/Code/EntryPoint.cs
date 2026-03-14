using Cysharp.Threading.Tasks;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntryPoint : MonoBehaviour
	, IPlayerJoined
	, IPlayerLeft
{
	public static EntryPoint Instance;
	public static readonly byte[] Token = System.Guid.NewGuid().ToByteArray();

	[SerializeField] AvatarManager _avatarManagerPrefab;
	[SerializeField] NetworkRunner _networkRunnerPrefab;
	[SerializeField] Button _networkButton;
	[SerializeField] TMP_Text _networkText;

	private NetworkRunner _networkRunner;

	void Awake()
	{
		Instance = this;
		_networkButton.onClick.AddListener(ToggleNetwork);
		_networkButton.gameObject.SetActive(true);
		_networkText.text = "network";
	}

	void OnDestroy()
	{
		_networkButton.onClick.RemoveListener(ToggleNetwork);
		if (Instance == this)
			Instance = null;
	}

	void Update()
	{
		var keyboard = Keyboard.current;
		if (_networkRunner && keyboard.escapeKey.wasPressedThisFrame) {
			var nextMenuState = !_networkButton.gameObject.activeSelf;
			_networkButton.gameObject.SetActive(nextMenuState);
			GameCursor.Instance.SetState(nextMenuState);
		}
	}

	private async void ToggleNetwork()
	{
		Debug.Log("ToggleNetwork invoked");
		_networkButton.interactable = false;
		_networkText.text = "...";

		var ct = destroyCancellationToken;

		if (_networkRunner)
		{
			await _networkRunner.Shutdown();
			_networkRunner = null;
		}
		else
		{
			var activeScene = SceneManager.GetActiveScene();

			var instance = Instantiate(_networkRunnerPrefab);
			var result = await instance.StartGame(new StartGameArgs {
				GameMode = GameMode.AutoHostOrClient, ConnectionToken = Token,
				Scene = SceneRef.FromIndex(activeScene.buildIndex),
				OnGameStarted = runner => { _networkRunner = runner; },
			});
			Debug.Log(result);
			if (result.Ok && !ct.IsCancellationRequested)
			{
				Instantiate(_avatarManagerPrefab); // @note should it be spawned before `StartGame` instead ?
				// @note should it just be set to `instance` right away or after at least after `StartGame` ?
				await UniTask.WaitUntil(() => _networkRunner || ct.IsCancellationRequested);
			}
		}

		if (!ct.IsCancellationRequested)
		{
			var nextMenuState = !_networkRunner;
			_networkButton.gameObject.SetActive(nextMenuState);
			GameCursor.Instance.SetState(nextMenuState);

			_networkText.text = _networkRunner ? "shutdown" : "start";
			_networkButton.interactable = true;
			Debug.Log("ToggleNetwork completed");
		}
	}

	void IPlayerJoined.PlayerJoined(PlayerRef player)
	{
	}

	void IPlayerLeft.PlayerLeft(PlayerRef player)
	{
	}
}
