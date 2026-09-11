using UnityEngine;

namespace MiniRealisticAirways;

public class Altitude : MonoBehaviour
{
	public static string ToString(AltitudeLevel altitude)
	{
		return altitude switch
		{
			AltitudeLevel.Low => ">", 
			AltitudeLevel.Normal => ">>", 
			AltitudeLevel.High => ">>>", 
			_ => "-", 
		};
	}

	public static bool InputClimb()
	{
		return Input.GetKeyDown(KeyCode.W) || (!Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") > 0f);
	}

	public static bool InputDescend()
	{
		return Input.GetKeyDown(KeyCode.S) || (!Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Mouse ScrollWheel") < 0f);
	}
}
