using UnityEngine;

namespace MiniRealisticAirways;

public class Speed : MonoBehaviour
{
	public const float SPEED_DELTA = 2f;

	public static float ToGameSpeed(SpeedLevel speed)
	{
		return speed switch
		{
			SpeedLevel.Slow => 20f, 
			SpeedLevel.Normal => 24f, 
			SpeedLevel.Fast => 28f, 
			_ => 0f, 
		};
	}

	public static SpeedLevel ToModSpeed(float speed)
	{
		if (speed < ToGameSpeed(SpeedLevel.Slow) - 2f)
		{
			return SpeedLevel.Stopped;
		}
		if (speed < ToGameSpeed(SpeedLevel.Normal) - 2f)
		{
			return SpeedLevel.Slow;
		}
		if (speed < ToGameSpeed(SpeedLevel.Fast) - 2f)
		{
			return SpeedLevel.Normal;
		}
		return SpeedLevel.Fast;
	}

	public static string ToString(SpeedLevel speed)
	{
		return speed switch
		{
			SpeedLevel.Slow => ">", 
			SpeedLevel.Normal => ">>", 
			SpeedLevel.Fast => ">>>", 
			_ => "|", 
		};
	}

	public static bool InputSpeedUp()
	{
		return Input.GetKeyDown(KeyCode.D) || (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") > 0f);
	}

	public static bool InputSlowDown()
	{
		return Input.GetKeyDown(KeyCode.A) || (Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") < 0f);
	}
}
