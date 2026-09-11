using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>Stable identity for the three settings clones under persistent OptionsManager UI.</summary>
internal sealed class SettingsCloneMarker : MonoBehaviour
{
	internal enum Role
	{
		WindToggle,
		EventToggle,
		TcasToggle,
		WindText,
		EventText,
		TcasText
	}

	internal Role role_;

	// Settings labels are cloned from a stock control whose width is too small
	// for several locales.  Keep an immutable copy of that geometry on the
	// clone so a locale switch can shrink back to the original width instead of
	// accumulating the previous locale's expansion.
	internal bool labelLayoutCaptured_;
	internal float labelBaseWidth_;
	internal float labelBaseHeight_;
	internal float labelRightEdge_;
	internal float labelPivotY_;

	internal static SettingsCloneMarker Attach(GameObject target, Role role)
	{
		if (target == null)
		{
			return null;
		}
		SettingsCloneMarker marker = target.GetComponent<SettingsCloneMarker>();
		if (marker == null)
		{
			marker = target.AddComponent<SettingsCloneMarker>();
		}
		marker.role_ = role;
		return marker;
	}
}
