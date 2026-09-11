using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniRealisticAirways;

public class WaypointState : MonoBehaviour
{
	public PlaceableWaypoint waypoint_;

	private TMP_Text altitudeText_;

	private TMP_Text speedText_;

	private TMP_Text nameText_;

	private TMP_Text nameCursor_;

	// 高度/速度等级改用与飞机 HUD 一致的实心方块，跟在航点下方的前缀文本之后。
	private SpriteRenderer[] altitudeBlocks_;

	private SpriteRenderer[] speedBlocks_;

	private WaypointAltitude waypointAltitude_;

	private WaypointSpeed waypointSpeed_;

	private WaypointNameInput waypointNameInput_;

	private bool textCacheInitialized_;

	private bool lastVisible_;

	private bool lastFlightDataVisible_;

	private bool lastNamingBlink_;

	private bool lastNamingActive_;

	private AltitudeLevel lastAltitude_;

	private SpeedLevel lastSpeed_;

	private string lastNameValue_;

	private int lastLocaleRevision_ = -1;

	private int lastLayoutRevision_ = -1;

	private void StartText(ref TMP_Text text, float x, float y, float z = 5f, float size = 2f, HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left)
	{
		GameObject gameObject = new GameObject("Text");
		text = gameObject.AddComponent<TextMeshPro>();
		text.fontSize = size;
		text.horizontalAlignment = horizontalAlignment;
		text.verticalAlignment = VerticalAlignmentOptions.Top;
		text.rectTransform.sizeDelta = new Vector2(2f, 1f);
		gameObject.transform.SetParent(waypoint_.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(x, y, z);
		SortingGroup sortingGroup = gameObject.AddComponent<SortingGroup>();
		sortingGroup.sortingLayerName = "Text";
		sortingGroup.sortingOrder = 1;
	}

	private void StartBlockRow(ref SpriteRenderer[] blocks, string namePrefix)
	{
		blocks = new SpriteRenderer[3];
		for (int i = 0; i < 3; i++)
		{
			GameObject gameObject = new GameObject(namePrefix + (i + 1));
			gameObject.transform.SetParent(waypoint_.transform, worldPositionStays: false);
			// 初始尺寸按默认字号估算，HudLayoutProfiles.ApplyWaypoint 会按实测重设。
			gameObject.transform.localScale = new Vector3(HudLayoutProfiles.BlockSize(1f), HudLayoutProfiles.BlockSize(1f), 1f);
			gameObject.transform.localPosition = new Vector3(0f, 0f, 4.8f);
			SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
			spriteRenderer.sprite = HudIndicatorTexture.GetOrCreate();
			spriteRenderer.color = Color.white;
			spriteRenderer.enabled = false;
			// 与航点文本相同的排序层，否则方块会被场景背景遮挡（z 排序不可靠）。
			SortingGroup sortingGroup = gameObject.AddComponent<SortingGroup>();
			sortingGroup.sortingLayerName = "Text";
			sortingGroup.sortingOrder = 1;
			blocks[i] = spriteRenderer;
		}
	}

	private void Start()
	{
		if (!(waypoint_ == null))
		{
			StartText(ref altitudeText_, 0.7f, -2f);
			StartText(ref speedText_, 2.75f, -2f);
			StartText(ref nameText_, 0f, 0.5f, 5f, 6f, HorizontalAlignmentOptions.Center);
			StartText(ref nameCursor_, 0f, 0.5f, 5f, 6f);
			nameCursor_.gameObject.SetActive(false);
			nameText_.enableAutoSizing = false;
			nameText_.enableWordWrapping = false;
			nameText_.overflowMode = TextOverflowModes.Overflow;
			StartBlockRow(ref altitudeBlocks_, "AltitudeBlock");
			StartBlockRow(ref speedBlocks_, "SpeedBlock");
			waypointAltitude_ = waypoint_.GetComponent<WaypointAltitude>();
			waypointSpeed_ = waypoint_.GetComponent<WaypointSpeed>();
			waypointNameInput_ = waypoint_.GetComponent<WaypointNameInput>();
		}
	}

	private void Update()
	{
		if (waypoint_ == null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		if (altitudeText_ == null || speedText_ == null || nameText_ == null)
		{
			return;
		}
		bool visible = Plugin.showText_ && !waypoint_.Invisible;
		bool showFlightData = visible && waypoint_ is BaseWaypointAutoHeading && waypointAltitude_ != null && waypointSpeed_ != null;
		AltitudeLevel altitude = showFlightData ? waypointAltitude_.altitude_ : default(AltitudeLevel);
		SpeedLevel speed = showFlightData ? waypointSpeed_.speed_ : default(SpeedLevel);
		string nameValue = visible && waypointNameInput_ != null ? waypointNameInput_.text ?? string.Empty : string.Empty;
		// 命名会话激活提示：旧版没有专属 UI，这里补充闪烁下划线光标。
		// 未满五字时光标恒定占位；满五字改为叠加在第五字下方，闪烁不改变宽度。
		bool namingActive = visible && waypointNameInput_ != null && waypointNameInput_.active;
		bool namingBlink = namingActive && Animation.Blink();
		if (textCacheInitialized_ && (!showFlightData || lastLayoutRevision_ == ModLocalization.Revision) && lastLocaleRevision_ == ModLocalization.Revision && visible == lastVisible_ && showFlightData == lastFlightDataVisible_ && (!showFlightData || (altitude == lastAltitude_ && speed == lastSpeed_)) && namingActive == lastNamingActive_ && namingBlink == lastNamingBlink_ && string.Equals(nameValue, lastNameValue_, System.StringComparison.Ordinal))
		{
			return;
		}
		bool localeChanged = lastLocaleRevision_ != ModLocalization.Revision;
		textCacheInitialized_ = true;
		lastVisible_ = visible;
		lastFlightDataVisible_ = showFlightData;
		lastAltitude_ = altitude;
		lastSpeed_ = speed;
		lastNameValue_ = nameValue;
		lastNamingBlink_ = namingBlink;
		lastNamingActive_ = namingActive;
		bool fullNameCursor = namingActive && nameValue.Length >= 5;
		if (namingActive && !fullNameCursor) nameValue += namingBlink ? "_" : "<alpha=#00>_";
		lastLocaleRevision_ = ModLocalization.Revision;
		// 等级由文本后的实心方块表示（同飞机 HUD），文本只保留本地化前缀。
		// 前缀不含开头的换行：垂直定位由 HudLayoutProfiles 按实际字形中心控制，
		// 避免空行使 TMP 按矩形换行（此前 "高度：" 被折行的根因）。
		string altitudeText = showFlightData ? ModLocalization.Get("hud.altitudePrefix").TrimEnd() : string.Empty;
		string speedText = showFlightData ? ModLocalization.Get("hud.speedPrefix").TrimEnd() : string.Empty;
		SetText(altitudeText_, altitudeText, localeChanged);
		SetText(speedText_, speedText, localeChanged);
		SetText(nameText_, nameValue, localeChanged);
		UpdateBlocks(altitudeBlocks_, showFlightData ? (int)altitude : 0);
		UpdateBlocks(speedBlocks_, showFlightData ? (int)speed : 0);
		if (!showFlightData) lastLayoutRevision_ = -1;
		if (showFlightData && lastLayoutRevision_ != ModLocalization.Revision)
		{
			if (HudLayoutProfiles.ApplyWaypoint(altitudeText_, speedText_, nameText_, altitudeBlocks_, speedBlocks_))
				lastLayoutRevision_ = ModLocalization.Revision;
		}
		LayoutName(fullNameCursor, namingBlink);
	}

	private void LayoutName(bool fullNameCursor, bool blink)
	{
		// Move the entire name and its baseline up by one current-font line.
		float lineHeight = nameText_.GetPreferredValues("Ag").y;
		nameText_.transform.localPosition = new Vector3(0f, 0.5f + lineHeight, 5f);
		nameText_.ForceMeshUpdate(false, true);
		nameCursor_.gameObject.SetActive(fullNameCursor);
		if (!fullNameCursor) return;

		// At the limit, the underscore is an overlay, not a sixth layout slot.
		// Keep the same underscore baseline as the ordinary inline cursor.
		nameCursor_.fontSize = nameText_.fontSize;
		nameCursor_.enableAutoSizing = false;
		nameCursor_.enableWordWrapping = false;
		nameCursor_.overflowMode = TextOverflowModes.Overflow;
		SetText(nameCursor_, blink ? "_" : "<alpha=#00>_", true);
		nameCursor_.ForceMeshUpdate(false, true);
		if (nameText_.textInfo.characterCount < 5 || nameCursor_.textInfo.characterCount == 0) return;
		TMP_CharacterInfo last = nameText_.textInfo.characterInfo[4];
		TMP_CharacterInfo cursor = nameCursor_.textInfo.characterInfo[0];
		float x = (last.bottomLeft.x + last.topRight.x - cursor.bottomLeft.x - cursor.topRight.x) * 0.5f;
		float y = last.baseLine - cursor.baseLine;
		nameCursor_.transform.localPosition = nameText_.transform.localPosition + new Vector3(x, y, 0f);
	}

	private static void UpdateBlocks(SpriteRenderer[] blocks, int count)
	{
		if (blocks == null)
		{
			return;
		}
		for (int i = 0; i < blocks.Length; i++)
		{
			if (blocks[i] != null)
			{
				blocks[i].enabled = i < count;
			}
		}
	}

	private static void SetText(TMP_Text text, string value, bool force)
	{
		if (text != null && (force || text.text != value))
		{
			ChineseTypography.SetHudText(text, value, "Waypoint HUD " + text.name);
		}
	}

	private void OnDestroy()
	{
		if (altitudeText_ != null)
		{
			Object.Destroy(altitudeText_.gameObject);
		}
		if (speedText_ != null)
		{
			Object.Destroy(speedText_.gameObject);
		}
		if (nameText_ != null)
		{
			Object.Destroy(nameText_.gameObject);
		}
		if (nameCursor_ != null)
		{
			Object.Destroy(nameCursor_.gameObject);
		}
		DestroyBlocks(altitudeBlocks_);
		DestroyBlocks(speedBlocks_);
		altitudeText_ = null;
		speedText_ = null;
		nameText_ = null;
		altitudeBlocks_ = null;
		speedBlocks_ = null;
	}

	private static void DestroyBlocks(SpriteRenderer[] blocks)
	{
		if (blocks == null)
		{
			return;
		}
		for (int i = 0; i < blocks.Length; i++)
		{
			if (blocks[i] != null)
			{
				Object.Destroy(blocks[i].gameObject);
			}
		}
	}
}

