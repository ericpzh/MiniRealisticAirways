using System;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// 航点高度仪表：图标左侧竖排箭头（原版样式）。可见箭头整体的重心与航点中心
/// 等高，等级变化时按可见数量重新居中；列的水平位置与右侧速度仪表关于航点
/// 竖直对称轴镜像。
/// </summary>
public class WaypointAltitudeGauge : Gauge
{
	public PlaceableWaypoint waypoint_;

	private Camera mainCamera_;

	// 箭头精灵 0.9 宽、0.3 高（左下轴点）。可见列的水平盒中心与速度仪表镜像。
	internal const float ColumnCenterOffset = 1.025f;
	internal const float ArrowStride = 0.25f;
	internal const float ArrowSpan = 0.3f;
	private const float ArrowWidth = 0.9f;

	public void UpdateWaypointAltitudeGauge(AltitudeLevel altitude)
	{
		if (waypoint_ != null && !waypoint_.Invisible && waypoint_ is BaseWaypointAutoHeading)
		{
			if (!Ready())
			{
				TryInitialize();
			}
			if (PointerUtility.IsOver(waypoint_.transform, ref mainCamera_))
			{
				PositionVisibleArrows((int)altitude);
				UpdateGaugeSpriteRenderers((int)(altitude - 1));
			}
		}
	}

	private void Start()
	{
		if (waypoint_ != null)
		{
			TryInitialize();
		}
	}

	protected override void ConfigureRenderers()
	{
		if (waypoint_ == null)
		{
			throw new System.InvalidOperationException("仪表所属对象不可用。");
		}
		for (int i = 0; i < 3; i++)
		{
			gameObjects_[i].transform.SetParent(waypoint_.transform);
			gameObjects_[i].transform.localScale = new Vector3(1f, 1f, 1f);
			gameObjects_[i].transform.localPosition = new Vector3(0f, 0f, -9f);
		}
		PositionVisibleArrows(2);
		EnableSpriteRenderer(2);
	}

	// visibleCount：可见箭头数（1..3）。可见段竖向居中于航点中心。
	private void PositionVisibleArrows(int visibleCount)
	{
		if (gameObjects_ == null || gameObjects_.Count < 3)
		{
			return;
		}
		int count = Math.Clamp(visibleCount, 1, 3);
		float stackHeight = (float)(count - 1) * ArrowStride + ArrowSpan;
		float bottomY = -stackHeight * 0.5f;
		float x = -(ColumnCenterOffset + ArrowWidth * 0.5f);
		for (int i = 0; i < 3; i++)
		{
			gameObjects_[i].transform.localPosition = new Vector3(x, bottomY + (float)i * ArrowStride, -9f);
		}
	}
}
