using System;
using System.Collections;
using System.Collections.Generic;
using MapSelection;
using UnityEngine;

namespace MiniRealisticAirways;

public class EventManager : MonoBehaviour
{
	public static Runway closedRunway_;

	public static Weather weather_;

	public const float EVENT_RESTORE_TIME = 75f;

	public static Sprite f16Sprite_;

	public static Sprite b747Sprite_;

	private static EventManager activeInstance_;

	private List<Event> events_;

	private int index_ = 0;

	private const float EVENT_BASE_TIME = 1800f;

	private const float EVENT_RANDOM_TIME_OFFSET_LIMIT = 300f;

	private const float EVENT_RETRIGGER_INTERVAL = 1f;

	private const int MAX_TRIGGER_ATTEMPTS = 10;

	private IEnumerator StartEventCoroutine()
	{
		WaitForSeconds retryDelay = new WaitForSeconds(EVENT_RETRIGGER_INTERVAL);
		WaitForSeconds restoreDelay = new WaitForSeconds(EVENT_RESTORE_TIME);
		while (events_ != null && events_.Count > 0)
		{
			yield return new WaitForSeconds(EVENT_BASE_TIME + (2f * UnityEngine.Random.value - 1f) * EVENT_RANDOM_TIME_OFFSET_LIMIT);
			// 不适用事件最多等待 MAX_TRIGGER_ATTEMPTS 秒，再尝试下一种；本轮最多触发一个特情。
			for (int skipped = 0; events_ != null && skipped < events_.Count; skipped++)
			{
				Event currentEvent = events_[GetIndex(index_)];
				bool triggered = false;
				for (int attempt = 0; currentEvent != null && attempt < MAX_TRIGGER_ATTEMPTS; attempt++)
				{
					if (currentEvent.Trigger()) { triggered = true; break; }
					yield return retryDelay;
				}
				index_ = GetIndex(index_ + 1);
				if (!triggered) continue;
				yield return restoreDelay;
				if (currentEvent != null) currentEvent.Restore();
				break;
			}
		}
	}

	private void Start()
	{
		activeInstance_ = this;
		closedRunway_ = null;
		weather_ = null;
		f16Sprite_ = null;
		b747Sprite_ = null;
		if (Settings.DISABLE_EVENTS)
		{
			return;
		}
		EngineOut item = base.gameObject.GetComponent<EngineOut>();
		if (item == null)
		{
			item = base.gameObject.AddComponent<EngineOut>();
		}
		RunwayClose item2 = base.gameObject.GetComponent<RunwayClose>();
		if (item2 == null)
		{
			item2 = base.gameObject.AddComponent<RunwayClose>();
		}
		LowFuelArrival lowFuelEvent = base.gameObject.GetComponent<LowFuelArrival>();
		if (lowFuelEvent == null)
		{
			lowFuelEvent = base.gameObject.AddComponent<LowFuelArrival>();
		}
		BadWeather badWeatherEvent = base.gameObject.GetComponent<BadWeather>();
		if (badWeatherEvent == null)
		{
			badWeatherEvent = base.gameObject.AddComponent<BadWeather>();
		}
		events_ = new List<Event>
		{
			item,
			item2,
			lowFuelEvent,
			badWeatherEvent
		};
		Utils.Shuffle(events_);
		Plugin.Log.LogInfo("Event setup completed.");
		StartCoroutine(StartEventCoroutine());
		PreLevel02Manager preLevel02Manager = (PreLevel02Manager)UnityEngine.Object.FindObjectOfType(typeof(PreLevel02Manager));
		if (preLevel02Manager == null)
		{
			return;
		}
		List<MapItem> fieldValue = preLevel02Manager.GetFieldValue<List<MapItem>>("MapData");
		if (fieldValue == null)
		{
			return;
		}
		foreach (MapItem item3 in fieldValue)
		{
			if (item3 == null || item3.MapContent == null || !(item3.sceneName == "SanFrancisco"))
			{
				continue;
			}
			AirForceOne componentInChildren = item3.MapContent.GetComponentInChildren<AirForceOne>();
			if (!(componentInChildren == null))
			{
				GameObject fieldValue2 = componentInChildren.GetFieldValue<GameObject>("F16Prefab");
				GameObject fieldValue3 = componentInChildren.GetFieldValue<GameObject>("B747Prefab");
				if (!(fieldValue2 == null) && !(fieldValue3 == null))
				{
					f16Sprite_ = GetAircraftSprite(fieldValue2);
					b747Sprite_ = GetAircraftSprite(fieldValue3);
				}
			}
			break;
		}
	}

	private static Sprite GetAircraftSprite(GameObject prefab)
	{
		Aircraft aircraft = prefab.GetComponent<Aircraft>();
		SpriteRenderer renderer = aircraft != null && aircraft.AP != null
			? aircraft.AP.GetComponent<SpriteRenderer>() : prefab.GetComponentInChildren<SpriteRenderer>(true);
		return renderer == null ? null : renderer.sprite;
	}

	private int GetIndex(int index)
	{
		return events_ == null || events_.Count == 0 ? 0 : Math.Abs(index % events_.Count);
	}

	private void OnDestroy()
	{
		StopAllCoroutines();
		events_?.Clear();
		events_ = null;
		if (activeInstance_ == this)
		{
			activeInstance_ = null;
			closedRunway_ = null;
			weather_ = null;
			f16Sprite_ = null;
			b747Sprite_ = null;
		}
	}
}
