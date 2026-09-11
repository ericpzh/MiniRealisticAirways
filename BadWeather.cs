using UnityEngine;

namespace MiniRealisticAirways;

public class BadWeather : Event
{
	private Weather weather_;

	public override bool Trigger()
	{
		GameObject gameObject = GameObject.Find("ESC_Button");
		if (gameObject == null)
		{
			return false;
		}
		if (EventManager.weather_ != null)
		{
			return false;
		}
		weather_ = gameObject.GetComponent<Weather>();
		if (weather_ == null)
		{
			weather_ = gameObject.AddComponent<Weather>();
		}
		if (weather_ == null)
		{
			return false;
		}
		// 启用组件以允许首次 Start；禁飞 enabled_ 仍由 20 秒淡入结束后开启。
		weather_.enabled = true;
		EventManager.weather_ = weather_;
		Plugin.Log?.LogInfo("BadWeather Triggered.");
		return true;
	}

	public override void Restore()
	{
		Plugin.Log.LogWarning("BadWeather Restored.");
		if (weather_ != null)
		{
			weather_.DestroyWeather();
		}
		EventManager.weather_ = null;
		weather_ = null;
	}
}
