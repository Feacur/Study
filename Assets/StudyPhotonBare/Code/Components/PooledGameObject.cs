using StudyPhotonBare.Tools;
using UnityEngine;
using UnityEngine.Assertions;


namespace StudyPhotonBare.Components
{

public class PooledGameObject : MonoBehaviour
{
	// @note fetch the root object, should be one step away only
	public PooledGameObject Prefab => _prefab ? _prefab.Prefab : this;

	[Header("Private")]
	private PooledGameObject _prefab;

	public PooledGameObject Instantiate(InstantiateParameters parameters = default)
	{
		Assert.IsNull(_prefab); // dev
		Assert.IsTrue(gameObject.IsPrefab()); // dev
		var ret = Instantiate(this, parameters: parameters);
		var pooled = ret.GetComponent<PooledGameObject>();
		pooled._prefab = this;
		return ret;
	}
}

}
