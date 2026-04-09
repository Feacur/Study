using StudyPhotonBare.Tools;
using UnityEngine;
using DevAssert = UnityEngine.Assertions.Assert;


namespace StudyPhotonBare.Components
{

public class PoolObject : MonoBehaviour
{
	// @note fetch the root object, should be one step away only
	public PoolObject Prefab => _prefab ? _prefab.Prefab : this;

	[Header("Private")]
	private PoolObject _prefab;

	public PoolObject Instantiate(InstantiateParameters parameters = default)
	{
		DevAssert.IsNull(_prefab);
		DevAssert.IsTrue(gameObject.IsPrefab());
		var ret = Instantiate(this, parameters: parameters);
		var pooled = ret.GetComponent<PoolObject>();
		pooled._prefab = this;
		return ret;
	}
}

}
