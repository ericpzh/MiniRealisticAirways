using System;
using System.Collections.Generic;

namespace MiniRealisticAirways;

internal static class AircraftVisualSortingRegistry
{
	private static readonly List<AircraftVisualSortingController> Controllers = new List<AircraftVisualSortingController>();

	private static readonly HashSet<AircraftVisualSortingController> KnownControllers = new HashSet<AircraftVisualSortingController>();

	private static readonly Dictionary<int, int> LayerBaseOrders = new Dictionary<int, int>();

	private static readonly Dictionary<int, int> LayerNextOrders = new Dictionary<int, int>();

	private static readonly Comparison<AircraftVisualSortingController> SortComparison = CompareControllers;

	private static long nextStableSequence_;

	internal static void Register(AircraftVisualSortingController controller)
	{
		if (controller == null || !KnownControllers.Add(controller))
		{
			return;
		}
		controller.AssignStableSequence(nextStableSequence_++);
		Controllers.Add(controller);
		Rebalance();
	}

	internal static void Unregister(AircraftVisualSortingController controller)
	{
		KnownControllers.Remove(controller);
		if (Controllers.Remove(controller))
		{
			Rebalance();
		}
	}

	internal static void NotifyAltitudeChanged(AircraftVisualSortingController controller)
	{
		if (controller != null && KnownControllers.Contains(controller))
		{
			Rebalance();
		}
	}

	internal static void Reset()
	{
		Controllers.Clear();
		KnownControllers.Clear();
		LayerBaseOrders.Clear();
		LayerNextOrders.Clear();
		nextStableSequence_ = 0L;
	}

	private static int CompareControllers(AircraftVisualSortingController left, AircraftVisualSortingController right)
	{
		int altitudeComparison = left.CurrentAltitude.CompareTo(right.CurrentAltitude);
		if (altitudeComparison != 0)
		{
			return altitudeComparison;
		}
		return left.StableSequence.CompareTo(right.StableSequence);
	}

	private static void Rebalance()
	{
		RemoveDestroyedControllers();
		LayerBaseOrders.Clear();
		LayerNextOrders.Clear();
		for (int i = 0; i < Controllers.Count; i++)
		{
			AircraftVisualSortingController controller = Controllers[i];
			int layerId = controller.SortingLayerId;
			int baseOrder = controller.AuthoredRootOrder;
			if (!LayerBaseOrders.TryGetValue(layerId, out int currentBase) || baseOrder > currentBase)
			{
				LayerBaseOrders[layerId] = baseOrder;
			}
		}

		Controllers.Sort(SortComparison);
		for (int i = 0; i < Controllers.Count; i++)
		{
			AircraftVisualSortingController controller = Controllers[i];
			int layerId = controller.SortingLayerId;
			if (!LayerNextOrders.TryGetValue(layerId, out int nextOrder))
			{
				nextOrder = LayerBaseOrders[layerId];
			}
			controller.ApplyRootOrder(nextOrder);
			LayerNextOrders[layerId] = nextOrder == int.MaxValue ? int.MaxValue : nextOrder + 1;
		}
	}

	private static void RemoveDestroyedControllers()
	{
		for (int i = Controllers.Count - 1; i >= 0; i--)
		{
			if (Controllers[i] == null)
			{
				KnownControllers.Remove(Controllers[i]);
				Controllers.RemoveAt(i);
			}
		}
	}
}
