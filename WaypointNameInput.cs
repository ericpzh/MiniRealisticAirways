using UnityEngine;

namespace MiniRealisticAirways;

public class WaypointNameInput : MonoBehaviour
{
	public string text = "";

	public PlaceableWaypoint waypoint_;

	public bool active;

	private const int MAX_LENGTH = 5;
	private string originalText_;

	private static readonly System.Collections.Generic.List<WaypointNameInput> inputs_ = new System.Collections.Generic.List<WaypointNameInput>();
	private static WaypointNameInput focused_;
	private static WaypointNameInput hovered_;
	private static Camera mainCamera_;
	private static int processedFrame_ = -1;
	private static bool consumedFrame_;
	// 会话在本帧结束过。原版把 Space 绑定为暂停（LevelManager.Update），命名必须
	// 通过 OnPauseTimeBtnPressed 前缀接管同一个按键；该戳记防止同帧"结束后又立即
	// 重新开启"或把结束用的按键再交给暂停。
	private static int sessionEndedFrame_ = -1;

	internal static bool AnyActive
	{
		get { ProcessInput(); return focused_ != null || consumedFrame_; }
	}

	internal static Transform HoveredTarget
	{
		get { ProcessInput(); return hovered_ == null ? null : hovered_.waypoint_.transform; }
	}

	private void OnEnable() { active = false; if (!inputs_.Contains(this)) inputs_.Add(this); }
	private void OnDisable()
	{
		active = false;
		if (focused_ == this) EndSession();
		if (hovered_ == this) hovered_ = null;
		inputs_.Remove(this);
		if (inputs_.Count == 0) mainCamera_ = null;
	}
	private void OnDestroy() { OnDisable(); }
	private void Update() { ProcessInput(); }
	private void OnApplicationFocus(bool hasFocus)
	{
		if (!hasFocus && focused_ == this) EndSession();
	}

	// All consumers resolve the same target and naming state before reading flight keys.
	// This is independent of Unity's order of Update callbacks.
	private static void ProcessInput()
	{
		if (processedFrame_ == Time.frameCount) return;
		processedFrame_ = Time.frameCount;
		consumedFrame_ = sessionEndedFrame_ == Time.frameCount;
		hovered_ = null;
		float best = float.PositiveInfinity;
		foreach (WaypointNameInput input in inputs_)
		{
			if (input == null || !input.isActiveAndEnabled || input.waypoint_ == null || input.waypoint_.Invisible || !(input.waypoint_ is BaseWaypointAutoHeading)) continue;
			if (PointerUtility.TryGetDistance(input.waypoint_.transform, ref mainCamera_, out float distance)
				&& (distance < best || (distance == best && (hovered_ == null || input.GetInstanceID() < hovered_.GetInstanceID()))))
			{
				best = distance;
				hovered_ = input;
			}
		}
		if (focused_ != null && (!focused_.isActiveAndEnabled || focused_.waypoint_ == null || focused_.waypoint_.Invisible || !focused_.active))
		{
			EndSession();
		}
		if (focused_ != null)
		{
			consumedFrame_ = true;
			// Space 不在这里处理：它与原版暂停共用一个按键，统一由 ConsumePauseKey
			// 在 LevelManager.OnPauseTimeBtnPressed 的前缀里仲裁，避免两种结局同帧打架。
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape)
				|| (Input.GetMouseButtonDown(0) && hovered_ != focused_) || Time.timeScale == 0f)
			{
				EndSession(cancel: Input.GetKeyDown(KeyCode.Escape));
			}
			else focused_.Type(); // Continue editing after the pointer leaves the waypoint.
		}
	}

	/// <summary>
	/// LevelManager.OnPauseTimeBtnPressed 的键盘 Space 仲裁入口。
	/// 命名中：Space 结束会话并拦截暂停；悬停航点：Space 开启会话并拦截暂停；
	/// 其余情况返回 false，原版暂停照常执行。暂停按钮的鼠标点击不含 Space，不受影响。
	/// </summary>
	internal static bool ConsumePauseKey()
	{
		ProcessInput();
		if (!Input.GetKeyDown(KeyCode.Space)) return false;
		if (sessionEndedFrame_ == Time.frameCount) return true;
		if (focused_ != null)
		{
			EndSession();
			return true;
		}
		if (Time.timeScale != 0f && hovered_ != null && sessionEndedFrame_ != Time.frameCount)
		{
			focused_ = hovered_;
			focused_.originalText_ = focused_.text ?? string.Empty;
			focused_.active = true;
			consumedFrame_ = true;
			return true;
		}
		return false;
	}

	private static void EndSession(bool cancel = false)
	{
		if (focused_ != null)
		{
			if (cancel) focused_.text = focused_.originalText_;
			focused_.originalText_ = null;
			focused_.active = false;
			focused_ = null;
		}
		consumedFrame_ = true;
		sessionEndedFrame_ = Time.frameCount;
	}

	private void Type()
	{
		text ??= string.Empty;
		if (Input.GetKeyDown(KeyCode.Backspace) && text.Length > 0)
		{
			text = text.Substring(0, text.Length - 1);
			return;
		}
		string inputString = Input.inputString;
		for (int i = 0; i < inputString.Length; i++)
		{
			char c = inputString[i];
			if ((char.IsLetter(c) || char.IsNumber(c)) && text.Length < MAX_LENGTH)
			{
				text += c;
				text = text.ToUpperInvariant();
			}
		}
	}
}
