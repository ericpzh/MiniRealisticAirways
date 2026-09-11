using UnityEngine;

namespace MiniRealisticAirways;

public class Event : MonoBehaviour
{
	public virtual bool Trigger()
	{
		return true;
	}

	public virtual void Restore()
	{
	}
}
