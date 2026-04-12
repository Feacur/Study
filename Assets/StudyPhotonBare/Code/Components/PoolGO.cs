using StudyPhotonBare.Tools;
using UnityEngine;
using DevAssert = UnityEngine.Assertions.Assert;


namespace StudyPhotonBare.Components
{

public class PoolGO : MonoBehaviour
{
	// @note fetch the root object, should be one step away only
	public PoolGO Prefab => _prefab ? _prefab.Prefab : this;

	[Header("Private")]
	private PoolGO _prefab;

	public PoolGO Instantiate(InstantiateParameters parameters = default)
	{
		DevAssert.IsNull(_prefab);
		DevAssert.IsTrue(gameObject.IsPrefab());
		var ret = Instantiate(this, parameters: parameters);
		var pooled = ret.GetComponent<PoolGO>();
		pooled._prefab = this;
		return ret;
	}
}

}
