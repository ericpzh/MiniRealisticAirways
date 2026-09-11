using System.Collections;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// Commits locale-dependent Mod UI after Unity's own localization callbacks
/// and layout pass have settled. Requests are generation based, so a rapid
/// language sequence can only commit its last selection. A slow poll also
/// re-syncs when Unity raises SelectedLocaleChanged late or drops it, which
/// otherwise left Mod text one language behind after repeated cycling.
/// </summary>
internal sealed class LocalizedUiRefreshCoordinator : MonoBehaviour
{
	private const string ObjectName = "MiniRealisticAirways.LocaleRefresh";

	// Unity 冷加载新语言的表时 SelectedLocaleChanged 可能迟到数秒；轮询周期取
	// 足够发现偏差又不至于每帧查询 LocalizationSettings。
	private const float LocalePollInterval = 0.5f;

	private static LocalizedUiRefreshCoordinator instance_;

	private Coroutine commitCoroutine_;

	private int requestedGeneration_;

	private string requestedLocale_;

	private int requestedRevision_;

	private float nextLocalePollTime_;

	internal static void Request(string localeCode, int revision)
	{
		LocalizedUiRefreshCoordinator coordinator = EnsureInstance();
		if (coordinator == null)
		{
			return;
		}
		coordinator.requestedGeneration_++;
		coordinator.requestedLocale_ = localeCode;
		coordinator.requestedRevision_ = revision;
		if (coordinator.commitCoroutine_ == null)
		{
			coordinator.commitCoroutine_ = coordinator.StartCoroutine(coordinator.CommitLatestCoroutine());
		}
	}

	private static LocalizedUiRefreshCoordinator EnsureInstance()
	{
		if (instance_ != null)
		{
			return instance_;
		}
		GameObject gameObject = new GameObject(ObjectName);
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		DontDestroyOnLoad(gameObject);
		instance_ = gameObject.AddComponent<LocalizedUiRefreshCoordinator>();
		return instance_;
	}

	private IEnumerator CommitLatestCoroutine()
	{
		while (true)
		{
			int generation = requestedGeneration_;
			string localeCode = requestedLocale_;
			int revision = requestedRevision_;

			// SelectedLocaleChanged is raised before several stock localizers update
			// their targets. Give those callbacks and the first Canvas layout pass a
			// chance to complete before Mod text and fonts become authoritative.
			yield return null;
			yield return new WaitForEndOfFrame();
			if (generation != requestedGeneration_)
			{
				continue;
			}

			if (ModLocalization.CommitPendingLocale(localeCode, revision))
			{
				Canvas.ForceUpdateCanvases();
				yield return null;
				Canvas.ForceUpdateCanvases();
				LocalizedFontRegistry.RetireUnreferencedRuntimeFonts();
			}
			else
			{
				// 期间版本号又被推进：不能静默丢弃这次请求，立即回到循环顶端取最新。
				Plugin.Log?.LogDebug("Locale commit for " + localeCode + " was superseded; retrying with the latest request.");
			}

			if (generation == requestedGeneration_)
			{
				break;
			}
		}
		commitCoroutine_ = null;
	}

	private void Update()
	{
		if (Time.unscaledTime < nextLocalePollTime_)
		{
			return;
		}
		nextLocalePollTime_ = Time.unscaledTime + LocalePollInterval;
		if (ModLocalization.EnsureMatchesSelectedLocale())
		{
			Plugin.Log?.LogInfo("Re-synced Mod locale with the game's selected locale after a late or missed change event.");
		}
	}

	private void OnDestroy()
	{
		if (instance_ == this)
		{
			instance_ = null;
		}
	}
}
