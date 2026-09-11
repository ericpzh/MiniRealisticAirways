using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniRealisticAirways;

// 两个仪表在同帧共享投影结果；同帧内相机或飞机位移都会重新投影。
// 弱键不会延长飞机生命周期。
internal static class AircraftViewport
{
	private sealed class Entry
	{
		internal int Frame = -1;
		internal Camera Camera;
		internal Vector3 CameraPosition;
		internal Vector3 Position;
		internal bool Visible;
	}
	private static readonly ConditionalWeakTable<Aircraft, Entry> cache_ = new ConditionalWeakTable<Aircraft, Entry>();
	internal static bool IsVisible(Aircraft aircraft, Camera camera)
	{
		if (aircraft == null || camera == null) return false;
		Entry entry = cache_.GetOrCreateValue(aircraft);
		Vector3 position = aircraft.gameObject.transform.position;
		Vector3 cameraPosition = camera.transform.position;
		if (entry.Frame != Time.frameCount || entry.Camera != camera || entry.CameraPosition.x != cameraPosition.x || entry.CameraPosition.y != cameraPosition.y || entry.CameraPosition.z != cameraPosition.z || entry.Position.x != position.x || entry.Position.y != position.y || entry.Position.z != position.z)
		{
			Vector2 viewport = camera.WorldToViewportPoint(position);
			entry.Visible = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
			entry.Frame = Time.frameCount;
			entry.Camera = camera;
			entry.CameraPosition = cameraPosition;
			entry.Position = position;
		}
		return entry.Visible;
	}
}
