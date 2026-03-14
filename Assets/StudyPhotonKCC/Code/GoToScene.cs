using UnityEngine;
using UnityEngine.SceneManagement;

namespace Study.PhotonKCC
{
	public class GoToScene : MonoBehaviour
	{
		public void Do(string name)
		{
			Debug.Log($"[Study] {nameof(GoToScene)}.{nameof(Do)} clicked");
			SceneManager.LoadSceneAsync(name, new LoadSceneParameters {
				loadSceneMode = LoadSceneMode.Single,
			});
		}
	}
}
