using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways;

public sealed class AircraftVisualSortingController : MonoBehaviour
{
	private const int AircraftOrderOffset = 0;

	private const int TextOrderOffset = 1;

	private const int GaugeOrderOffset = 2;

	private readonly List<RendererWithOffset> iconRenderers_ = new List<RendererWithOffset>();

	private readonly List<Renderer> textRenderers_ = new List<Renderer>();

	private readonly List<Renderer> gaugeRenderers_ = new List<Renderer>();

	private readonly List<Renderer> hudIndicatorRenderers_ = new List<Renderer>();

	private Aircraft aircraft_;

	private AircraftAltitude aircraftAltitude_;

	private SortingGroup sortingGroup_;

	private AltitudeLevel lastAltitude_;

	private int sortingLayerId_;

	private int authoredRootOrder_;

	private int authoredInternalOrder_;

	private long stableSequence_;

	private bool initialized_;

	private TMP_Text[] pendingTexts_;

	private bool primaryRendererWarningLogged_;

	private readonly struct RendererWithOffset
	{
		internal readonly Renderer Renderer;

		internal readonly int OrderOffset;

		internal RendererWithOffset(Renderer renderer, int orderOffset)
		{
			Renderer = renderer;
			OrderOffset = orderOffset;
		}
	}

	internal AltitudeLevel CurrentAltitude => lastAltitude_;

	internal int SortingLayerId => sortingLayerId_;

	internal int AuthoredRootOrder => authoredRootOrder_;

	internal long StableSequence => stableSequence_;

	internal bool Initialized => initialized_;

	public static AircraftVisualSortingController GetOrCreate(Aircraft aircraft)
	{
		if (aircraft == null)
		{
			return null;
		}
		AircraftVisualSortingController controller = aircraft.GetComponent<AircraftVisualSortingController>();
		if (controller == null)
		{
			controller = aircraft.gameObject.AddComponent<AircraftVisualSortingController>();
		}
		return controller;
	}

	public void Initialize(Aircraft aircraft, AircraftAltitude aircraftAltitude, params TMP_Text[] texts)
	{
		if (initialized_ || aircraft == null || aircraftAltitude == null)
		{
			return;
		}
		aircraft_ = aircraft;
		aircraftAltitude_ = aircraftAltitude;
		pendingTexts_ = texts;
		if (aircraft.AP == null)
		{
			return;
		}

		Renderer primaryRenderer = aircraft.AP.GetComponent<Renderer>();
		if (primaryRenderer == null)
		{
			if (!primaryRendererWarningLogged_)
			{
				primaryRendererWarningLogged_ = true;
				Plugin.Log?.LogWarning("Aircraft visual sorting deferred: AP renderer was not found.");
			}
			return;
		}

		sortingLayerId_ = primaryRenderer.sortingLayerID;
		authoredInternalOrder_ = primaryRenderer.sortingOrder;
		RegisterIconRenderers(aircraft.AP.GetComponentsInChildren<Renderer>(true));

		sortingGroup_ = aircraft.GetComponent<SortingGroup>();
		if (sortingGroup_ == null)
		{
			sortingGroup_ = aircraft.gameObject.AddComponent<SortingGroup>();
		}
		authoredRootOrder_ = sortingGroup_.sortingOrder;
		if (primaryRenderer.sortingOrder > authoredRootOrder_)
		{
			authoredRootOrder_ = primaryRenderer.sortingOrder;
		}
		sortingGroup_.sortingLayerID = sortingLayerId_;

		if (pendingTexts_ != null)
		{
			for (int i = 0; i < pendingTexts_.Length; i++)
			{
				RegisterText(pendingTexts_[i]);
			}
		}

		lastAltitude_ = aircraftAltitude_.altitude_;
		initialized_ = true;
		ApplyMemberSorting();
		AircraftVisualSortingRegistry.Register(this);
	}

	private void Update()
	{
		if (!initialized_ && aircraft_ != null && aircraftAltitude_ != null)
		{
			Initialize(aircraft_, aircraftAltitude_, pendingTexts_);
		}
	}

	public void RegisterGaugeRenderers(IList<SpriteRenderer> renderers)
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Count; i++)
		{
			Renderer renderer = renderers[i];
			if (renderer != null && !gaugeRenderers_.Contains(renderer))
			{
				gaugeRenderers_.Add(renderer);
			}
		}
		if (initialized_)
		{
			ApplyRenderers(gaugeRenderers_, GaugeOrderOffset);
		}
	}

	/// <summary>
	/// Registers the non-text HUD indicators (the solid altitude/speed blocks)
	/// at the same local order as the HUD text. Legacy gauge renderers remain
	/// registered at the next local order so the original aircraft-side arrows
	/// stay visible above the text and blocks.
	/// </summary>
	public void RegisterHudIndicatorRenderers(IList<SpriteRenderer> renderers)
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Count; i++)
		{
			Renderer renderer = renderers[i];
			if (renderer != null && !hudIndicatorRenderers_.Contains(renderer))
			{
				hudIndicatorRenderers_.Add(renderer);
			}
		}
		if (initialized_)
		{
			ApplyRenderers(hudIndicatorRenderers_, TextOrderOffset);
		}
	}

	public void RefreshVisualGroup()
	{
		if (initialized_)
		{
			ApplyMemberSorting();
			AircraftVisualSortingRegistry.NotifyAltitudeChanged(this);
		}
	}

	internal void AssignStableSequence(long sequence)
	{
		stableSequence_ = sequence;
	}

	internal void ApplyRootOrder(int sortingOrder)
	{
		if (sortingGroup_ != null && sortingGroup_.sortingOrder != sortingOrder)
		{
			sortingGroup_.sortingOrder = sortingOrder;
		}
	}

	private void RegisterIconRenderers(Renderer[] renderers)
	{
		if (renderers == null)
		{
			return;
		}
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			if (renderer != null && !ContainsIconRenderer(renderer))
			{
				long orderOffset = (long)renderer.sortingOrder - authoredInternalOrder_;
				iconRenderers_.Add(new RendererWithOffset(renderer, orderOffset > int.MaxValue ? int.MaxValue : orderOffset < int.MinValue ? int.MinValue : (int)orderOffset));
			}
		}
	}

	private bool ContainsIconRenderer(Renderer renderer)
	{
		for (int i = 0; i < iconRenderers_.Count; i++)
		{
			if (iconRenderers_[i].Renderer == renderer)
			{
				return true;
			}
		}
		return false;
	}

	private void RegisterText(TMP_Text text)
	{
		if (text == null)
		{
			return;
		}
		SortingGroup nestedGroup = text.GetComponent<SortingGroup>();
		if (nestedGroup != null && nestedGroup != sortingGroup_)
		{
			nestedGroup.enabled = false;
			Destroy(nestedGroup);
		}
		Renderer renderer = text.GetComponent<Renderer>();
		if (renderer != null && !textRenderers_.Contains(renderer))
		{
			textRenderers_.Add(renderer);
		}
	}

	private void ApplyMemberSorting()
	{
		ApplyIconRenderers();
		ApplyRenderers(textRenderers_, TextOrderOffset);
		ApplyRenderers(hudIndicatorRenderers_, TextOrderOffset);
		ApplyRenderers(gaugeRenderers_, GaugeOrderOffset);
	}

	private void ApplyIconRenderers()
	{
		for (int i = 0; i < iconRenderers_.Count; i++)
		{
			RendererWithOffset entry = iconRenderers_[i];
			Renderer renderer = entry.Renderer;
			if (renderer == null)
			{
				continue;
			}
			if (renderer.sortingLayerID != sortingLayerId_)
			{
				renderer.sortingLayerID = sortingLayerId_;
			}
			int sortingOrder = AddSortingOffset(authoredInternalOrder_, AircraftOrderOffset + entry.OrderOffset);
			if (renderer.sortingOrder != sortingOrder)
			{
				renderer.sortingOrder = sortingOrder;
			}
		}
	}

	private void ApplyRenderers(List<Renderer> renderers, int orderOffset)
	{
		int sortingOrder = AddSortingOffset(authoredInternalOrder_, orderOffset);
		for (int i = 0; i < renderers.Count; i++)
		{
			Renderer renderer = renderers[i];
			if (renderer == null)
			{
				continue;
			}
			if (renderer.sortingLayerID != sortingLayerId_)
			{
				renderer.sortingLayerID = sortingLayerId_;
			}
			if (renderer.sortingOrder != sortingOrder)
			{
				renderer.sortingOrder = sortingOrder;
			}
		}
	}

	private static int AddSortingOffset(int baseOrder, int offset)
	{
		long value = (long)baseOrder + offset;
		if (value > int.MaxValue)
		{
			return int.MaxValue;
		}
		if (value < int.MinValue)
		{
			return int.MinValue;
		}
		return (int)value;
	}

	private void LateUpdate()
	{
		if (!initialized_ || aircraft_ == null || aircraftAltitude_ == null)
		{
			return;
		}
		AltitudeLevel altitude = aircraftAltitude_.altitude_;
		if (altitude != lastAltitude_)
		{
			lastAltitude_ = altitude;
			AircraftVisualSortingRegistry.NotifyAltitudeChanged(this);
		}
	}

	private void OnDestroy()
	{
		if (initialized_)
		{
			AircraftVisualSortingRegistry.Unregister(this);
		}
		iconRenderers_.Clear();
		textRenderers_.Clear();
		gaugeRenderers_.Clear();
		hudIndicatorRenderers_.Clear();
		aircraft_ = null;
		aircraftAltitude_ = null;
		pendingTexts_ = null;
		sortingGroup_ = null;
		initialized_ = false;
	}
}
