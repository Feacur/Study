
using UnityEngine;
using PlayerID = System.Int32;

namespace StudyPhotonBare.Interfaces
{

public interface IEBSDropListener : IEventBusSubscriber
{
	void OnDropped(PlayerID playerID, Vector2 position);
}

}
