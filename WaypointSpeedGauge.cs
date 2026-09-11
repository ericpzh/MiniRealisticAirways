using System;
using UnityEngine;

namespace MiniRealisticAirways;

/// <summary>
/// 航点速度仪表：图标右侧横排竖向箭头（原版样式）。可见箭头整体的重心与航点
/// 中心等高（含箭头字形的光学校正），等级变化时按可见数量重新居中；行的水平
/// 位置与左侧高度仪表关于航点竖直对称轴镜像。
/// </summary>
public class WaypointSpeedGauge : Gauge
{
	public PlaceableWaypoint waypoint_;

	private Camera mainCamera_;

	// 可见行的水平盒中心与左侧高度列镜像；竖向箭头精灵向下延伸 0.9，
	// 行中心（RowY - 0.45）即可见箭头重心；取 0.45 使其落在航点中心水平线上。
	internal const float RowCenterOffset = 1.025f;
	internal const float ArrowStride = 0.25f;
	internal const float RowY = 0.45f;
	public void UpdateWaypointSpeedGauge(SpeedLevel speed)
	{
		if (waypoint_ != null && !waypoint_.Invisible && waypoint_ is BaseWaypointAutoHeading)
		{
			if (!Ready())
			{
				TryInitialize();
			}
			if (PointerUtility.IsOver(waypoint_.transform, ref mainCamera_))
			{
				PositionVisibleArrows((int)speed);
				UpdateGaugeSpriteRenderers((int)(speed - 1));
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
			gameObjects_[i].transform.rotation = Quaternion.AngleAxis(90f, Vector3.back);
		}
		PositionVisibleArrows(2);
		EnableSpriteRenderer(2);
	}

	// visibleCount：可见箭头数（1..3）。默认（低速/1 格）停在高速 3 格时最左侧
	// 那个箭头的水平位置；随速度提升逐级向右增加箭头，箭头始终从最左向右填充。
	private void PositionVisibleArrows(int visibleCount)
	{
		if (gameObjects_ == null || gameObjects_.Count < 3)
		{
			return;
		}
		int count = Math.Clamp(visibleCount, 1, 3);
		// 高速 3 格时整体居中于镜像轴：行宽 = 2*间距 + 跨度，最左箭头 x。
		float rowWidth = 2f * ArrowStride + 0.3f;
		float leftArrowX = RowCenterOffset - rowWidth * 0.5f;
		for (int i = 0; i < 3; i++)
		{
			gameObjects_[i].transform.localPosition = new Vector3(leftArrowX + (float)i * ArrowStride, RowY, -9f);
		}
	}
}
