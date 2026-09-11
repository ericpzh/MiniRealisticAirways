using System;
using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

// 风向模拟与着陆概率；WindSock.cs 保留 HUD 视图和生命周期。
internal partial class WindSock
{
	private const float WIND_RANDOM_BASE = 180f;

	private const float WIND_RANDOM_OFFSET_LIMIT = 30f;

	private const float WIND_BASE_TIME = 1800f;

	private const float WIND_RANDOM_TIME_OFFSET_LIMIT = 150f;

	private const float UPDATE_COUNT = 360f;

	public bool CanLand(float heading, Weight weight)
	{
		if (Settings.DISABLE_WIND)
		{
			return true;
		}
		float num = Math.Min((heading - windDirection_ < 0f) ? (heading - windDirection_ + 360f) : (heading - windDirection_), (windDirection_ - heading < 0f) ? (windDirection_ - heading + 360f) : (windDirection_ - heading));
		if (num <= 90f)
		{
			return true;
		}
		float probability = GoAroundProbability(num, weight);
		if (probability < UnityEngine.Random.value)
		{
			return true;
		}
		Plugin.Log.LogInfo("Go-around induced by wind. Current wind: " + windDirection_ + " Current Heading: " + heading + " Angle: " + num + " Probability " + probability);
		return false;
	}

	private void CorrectWindDirection()
	{
		if (windDirection_ < 0f)
		{
			windDirection_ += 360f;
		}
		else if (windDirection_ >= 360f)
		{
			windDirection_ -= 360f;
		}
	}

	private float GoAroundProbability(float x, Weight weight)
	{
		// 135 度为基础概率中点，1.05 控制随顺风夹角增长的曲线斜率。
		float num = (float)(1.0 / (1.0 + Math.Pow(1.05, 135f - x)));
		switch (weight)
		{
		case Weight.Light:
			num += 0.1f;
			break;
		case Weight.Medium:
			num += 0.05f;
			break;
		}
		return Math.Clamp(num, 0f, 1f);
	}

	private float RandomUniform(float limit)
	{
		return UnityEngine.Random.value * 2f * limit - limit;
	}

	private float RandomDirection()
	{
		float num = RandomUniform(WIND_RANDOM_OFFSET_LIMIT);
		float num2 = (((double)UnityEngine.Random.value > 0.5) ? 1 : (-1));
		return WIND_RANDOM_BASE * num2 + num;
	}

	private IEnumerator UpdateWindCoroutine()
	{
		while (windsock_ != null)
		{
			float updateTime = WIND_BASE_TIME + RandomUniform(WIND_RANDOM_TIME_OFFSET_LIMIT);
			float timeGradient = updateTime / UPDATE_COUNT;
			float directionChange = RandomDirection();
			float windGradient = directionChange / UPDATE_COUNT;
			Plugin.Log.LogInfo("Wind updated, moving from " + windDirection_ + " by " + directionChange + " degrees in " + updateTime + " seconds.");
			WaitForSeconds updateDelay = new WaitForSeconds(timeGradient);
			for (int i = 0; i < UPDATE_COUNT && windsock_ != null; i++)
			{
				windDirection_ += windGradient;
				CorrectWindDirection();
				yield return updateDelay;
			}
		}
	}

}
