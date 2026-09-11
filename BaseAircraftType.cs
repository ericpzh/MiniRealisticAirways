using UnityEngine;

namespace MiniRealisticAirways;

public class BaseAircraftType : MonoBehaviour
{
	public Weight weight_ = Weight.Medium;

	public override string ToString()
	{
		return weight_ switch
		{
			Weight.Light => ".", 
			Weight.Medium => "-", 
			Weight.Heavy => "=", 
			_ => "", 
		};
	}

	public static Weight RandomWeight()
	{
		float value = Random.value;
		if (value <= 0.025f)
		{
			return Weight.Light;
		}
		if (value >= 0.7f)
		{
			return Weight.Heavy;
		}
		return Weight.Medium;
	}

	public virtual float GetScaleFactor()
	{
		return weight_ switch
		{
			Weight.Light => 0.6f, 
			Weight.Medium => 1.25f, 
			Weight.Heavy => 2f, 
			_ => 1f, 
		};
	}
}
