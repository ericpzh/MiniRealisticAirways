using UnityEngine;

namespace MiniRealisticAirways;

internal static class PointerUtility
{
	// 0.01 世界单位不足 1 像素，航点高度/速度/命名几乎无法触发；改为航点图标量级。
	private const float PointerHitRadius = 0.5f;
	private static int pointerFrame_ = -1;
	private static Camera pointerCamera_;
	private static Vector2 pointer_;

	internal static bool IsOver(Transform target, ref Camera camera)
	{
		return target != null && WaypointNameInput.HoveredTarget == target;
	}

	internal static bool TryGetDistance(Transform target, ref Camera camera, out float distanceSquared)
	{
		distanceSquared = float.PositiveInfinity;
		if (target == null)
		{
			return false;
		}
		if (camera == null)
		{
			camera = Camera.main;
		}
		if (camera == null)
		{
			return false;
		}
		if (pointerFrame_ != Time.frameCount || pointerCamera_ != camera)
		{
			pointer_ = camera.ScreenToWorldPoint(Input.mousePosition);
			pointerFrame_ = Time.frameCount;
			pointerCamera_ = camera;
		}
		Vector2 delta = (Vector2)target.position - pointer_;
		distanceSquared = delta.sqrMagnitude;
		return distanceSquared <= PointerHitRadius * PointerHitRadius;
	}
}
