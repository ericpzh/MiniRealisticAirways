using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MiniRealisticAirways;

// DiscordController is supplied by the game. Its stock Update destroys the
// component after one RunCallbacks exception, which makes a transient Discord
// startup failure permanent for the whole game session. Keep the component
// alive, retry the SDK connection at a bounded rate, resend the latest activity
// after reconnecting, and dispose the native wrapper exactly at application
// shutdown. Reflection keeps this plugin independent of the first-pass wrapper
// assembly at build time; the game still provides it at runtime.
internal static class PatchDiscordControllerRuntime
{
	private const float RetryInterval = 5f;

	// Discord 客户端未运行是玩家的正常环境状态而非故障：连接重试固定 10 分钟
	// 检查一次，玩家中途打开 Discord 即可自动恢复。RetryInterval(5s) 仍用于
	// 已连接状态下的活动推送节奏与回调超时窗口，不在此处拉长。
	private const float ConnectionRetryInterval = 600f;

	private static readonly FieldInfo DiscordField = AccessTools.Field(typeof(DiscordController), "_discord");

	private static readonly FieldInfo DiscordAppIdField = AccessTools.Field(typeof(DiscordController), "discordAppId");

	private static readonly MethodInfo RunCallbacksMethod = AccessTools.Method(DiscordField?.FieldType, "RunCallbacks");

	private static readonly MethodInfo DisposeMethod = AccessTools.Method(DiscordField?.FieldType, "Dispose");

	private static DiscordController controller_;

	private static RuntimeState state_;

	internal static bool Start(DiscordController controller)
	{
		RuntimeState state = GetState(controller);
		CacheActivity(controller, state, controller.defaultDetails, null);
		TryInitialize(controller, state, force: true);
		TrySendActivity(controller, state);
		return false;
	}

	internal static bool Update(DiscordController controller)
	{
		if (controller == null)
		{
			return false;
		}
		RuntimeState state = GetState(controller);
		if (state.quitting_)
		{
			return false;
		}

		float now = Time.unscaledTime;
		object discord = GetDiscord(controller);
		if (discord != null)
		{
			try
			{
				if (RunCallbacksMethod == null) throw new MissingMethodException("Discord.RunCallbacks is unavailable.");
				RunCallbacksMethod.Invoke(discord, null);
			}
			catch (Exception exception)
			{
				HandleSdkFailure(controller, state, "Discord callbacks failed", exception);
				discord = null;
			}
		}
		if (state.activity_.TryExpire(now))
		{
			state.health_.Failed();
			// The native callback may never arrive when Discord disappears. The state
			// guard invalidates the request before allowing a retry, so a late old
			// callback cannot settle the next request or clear a newer revision.
			LogRateLimited(state, "Discord activity callback timed out; retrying the latest activity.", warning: true);
		}

		if (discord != null && state.health_.ReconnectRequired)
		{
			state.nextRetryTime_ = now + ConnectionRetryInterval;
			Dispose(controller, state, logFailure: false);
			Plugin.Log?.LogWarning("Discord activity failed 3 consecutive times; connection disposed, retrying in 10 minutes.");
			discord = null;
		}
		if (discord == null && now >= state.nextRetryTime_)
		{
			TryInitialize(controller, state, force: false);
		}
		if (GetDiscord(controller) != null && state.activity_.CanSend(now))
		{
			TrySendActivity(controller, state);
		}
		return false;
	}

	internal static bool UpdateActivity(DiscordController controller, string details, string stateText)
	{
		if (controller == null)
		{
			return false;
		}
		RuntimeState state = GetState(controller);
		CacheActivity(controller, state, details, stateText);
		TryInitialize(controller, state, force: false);
		TrySendActivity(controller, state);
		return false;
	}

	internal static bool OnApplicationQuit(DiscordController controller)
	{
		if (controller != null)
		{
			RuntimeState state = GetState(controller);
			state.quitting_ = true;
			Dispose(controller, state, logFailure: true);
		}
		return false;
	}

	private static RuntimeState GetState(DiscordController controller)
	{
		if (!ReferenceEquals(controller_, controller) || state_ == null)
		{
			controller_ = controller;
			state_ = new RuntimeState();
		}
		return state_;
	}

	private static object GetDiscord(DiscordController controller)
	{
		if (controller == null || DiscordField == null)
		{
			return null;
		}
		try
		{
			return DiscordField.GetValue(controller);
		}
		catch (Exception exception)
		{
			Plugin.Log?.LogDebug("Could not read Discord SDK state: " + exception.GetBaseException().Message);
			return null;
		}
	}

	private static bool TryInitialize(DiscordController controller, RuntimeState state, bool force)
	{
		if (controller == null || state.quitting_ || GetDiscord(controller) != null)
		{
			return GetDiscord(controller) != null;
		}
		float now = Time.unscaledTime;
		if (!force && now < state.nextRetryTime_)
		{
			return false;
		}
		if (DiscordField == null || DiscordAppIdField == null || DiscordField.FieldType == null)
		{
			LogRateLimited(state, "Discord SDK fields are unavailable; status integration remains disabled.", warning: true);
			state.nextRetryTime_ = now + ConnectionRetryInterval;
			return false;
		}

		try
		{
			long appId = (long)DiscordAppIdField.GetValue(controller);
			object discord = Activator.CreateInstance(DiscordField.FieldType, new object[] { appId, 1UL });
			DiscordField.SetValue(controller, discord);
			state.activity_.ConnectionEstablished();
			state.health_.Connected();
			state.nextRetryTime_ = now + RetryInterval;
			LogRateLimited(state, "Discord status connection initialized; activity will be synchronized.", warning: false);
			return true;
		}
		catch (Exception exception)
		{
			// 只将 SDK 明确报告未安装/未运行视为正常环境；InternalError 仍需诊断。
			state.nextRetryTime_ = now + ConnectionRetryInterval;
			Exception cause = Unwrap(exception);
			// This game's wrapper leaves Result at its default; its constructor puts
			// the actual enum name in Message instead. Match only exact known names.
			string result = cause.GetType().FullName == "Discord.ResultException" ? cause.Message : null;
			LogRateLimited(state, "Discord status initialization failed; retrying in 10 minutes: " + cause.Message,
				warning: !DiscordConnectionHealth.IsExpectedUnavailable(result));
			return false;
		}
	}

	private static void CacheActivity(DiscordController controller, RuntimeState state, string details, string stateText)
	{
		state.details_ = details ?? controller.defaultDetails;
		state.state_ = string.IsNullOrEmpty(stateText) ? controller.defaultState : stateText + " aircraft managed";
		controller.currentDetails = state.details_;
		controller.currentState = state.state_;
		state.activity_.MarkActivityChanged();
	}

	private static bool TrySendActivity(DiscordController controller, RuntimeState state)
	{
		float now = Time.unscaledTime;
		object discord = GetDiscord(controller);
		if (discord == null || !state.activity_.CanSend(now) || state.quitting_ || state.health_.ReconnectRequired)
		{
			return false;
		}

		try
		{
			object activityManager = discord.GetType().GetMethod("GetActivityManager", Type.EmptyTypes)?.Invoke(discord, null);
			if (activityManager == null)
			{
				throw new MissingMethodException("Discord.GetActivityManager returned null.");
			}
			Type activityType = activityManager.GetType().Assembly.GetType("Discord.Activity");
			Type assetsType = activityManager.GetType().Assembly.GetType("Discord.ActivityAssets");
			if (activityType == null || assetsType == null)
			{
				throw new MissingMethodException("Discord activity payload types are unavailable.");
			}
			object activity = Activator.CreateInstance(activityType);
			activityType.GetField("Details")?.SetValue(activity, state.details_);
			activityType.GetField("State")?.SetValue(activity, state.state_);
			object assets = Activator.CreateInstance(assetsType);
			assetsType.GetField("LargeImage")?.SetValue(assets, GetStringField(controller, "largeImageKey", "banner-03"));
			assetsType.GetField("LargeText")?.SetValue(assets, GetStringField(controller, "largeImageText", "Mini Airways"));
			activityType.GetField("Assets")?.SetValue(activity, assets);

			MethodInfo updateMethod = FindActivityUpdateMethod(activityManager.GetType(), activityType);
			if (updateMethod == null)
			{
				throw new MissingMethodException("Discord ActivityManager.UpdateActivity is unavailable.");
			}
			Type callbackType = updateMethod.GetParameters()[1].ParameterType;
			DiscordActivityState.RequestSnapshot request = state.activity_.BeginRequest(now, RetryInterval);
			object callback = CreateActivityCallback(callbackType, state, request);
			updateMethod.Invoke(activityManager, new[] { activity, callback });
			return true;
		}
		catch (Exception exception)
		{
			state.activity_.InvalidateConnection(now, RetryInterval);
			state.health_.Failed();
			LogRateLimited(state, "Discord activity update failed; will retry: " + Unwrap(exception).Message, warning: true);
			return false;
		}
	}

	private static MethodInfo FindActivityUpdateMethod(Type managerType, Type activityType)
	{
		foreach (MethodInfo method in managerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (method.Name == "UpdateActivity" && parameters.Length == 2 && parameters[0].ParameterType == activityType)
			{
				return method;
			}
		}
		return null;
	}

	private static object CreateActivityCallback(Type callbackType, RuntimeState state, DiscordActivityState.RequestSnapshot request)
	{
		ParameterInfo[] parameters = callbackType.GetMethod("Invoke")?.GetParameters();
		if (parameters == null || parameters.Length != 1)
		{
			throw new MissingMethodException("Discord activity callback signature is unavailable.");
		}
		Type resultType = parameters[0].ParameterType;
		MethodInfo method = typeof(ActivityCallbackTarget).GetMethod("OnResult", BindingFlags.Instance | BindingFlags.NonPublic);
		method = method?.MakeGenericMethod(resultType);
		if (method == null)
		{
			throw new MissingMethodException("Discord activity callback bridge is unavailable.");
		}
		return Delegate.CreateDelegate(callbackType, new ActivityCallbackTarget(state, request), method);
	}

	private static string GetStringField(DiscordController controller, string name, string fallback)
	{
		FieldInfo field = AccessTools.Field(typeof(DiscordController), name);
		object value = field?.GetValue(controller);
		return value as string ?? fallback;
	}

	private static void OnActivityResult(RuntimeState state, DiscordActivityState.RequestSnapshot request, object result)
	{
		bool success = result != null && string.Equals(result.ToString(), "Ok", StringComparison.Ordinal);
		if (!state.activity_.TryComplete(request, success, Time.unscaledTime, RetryInterval))
		{
			// A callback may arrive after a newer UpdateActivity call, a timeout
			// retry, or SDK disposal/reconnection. The state guard ignores it.
			return;
		}
		if (!success)
		{
			state.health_.Failed();
			LogRateLimited(state, "Discord activity update returned " + (result ?? "null") + "; will retry.", warning: true);
		}
		else
		{
			bool first = state.health_.Synchronized();
			// Do not let the initialization log's shared throttle hide this confirmation.
			if (first) Plugin.Log?.LogInfo("Discord activity synchronized successfully.");
		}
	}

	private static void HandleSdkFailure(DiscordController controller, RuntimeState state, string prefix, Exception exception)
	{
		state.nextRetryTime_ = Time.unscaledTime + ConnectionRetryInterval;
		Dispose(controller, state, logFailure: false);
		LogRateLimited(state, prefix + "; SDK will be recreated: " + Unwrap(exception).Message, warning: true);
	}

	private static void Dispose(DiscordController controller, RuntimeState state, bool logFailure)
	{
		state.health_.Disconnected();
		object discord = GetDiscord(controller);
		if (discord == null)
		{
			state.activity_.InvalidateConnection(Time.unscaledTime, RetryInterval);
			return;
		}
		try
		{
			DisposeMethod?.Invoke(discord, null);
		}
		catch (Exception exception)
		{
			if (logFailure)
			{
				Plugin.Log?.LogWarning("Discord SDK disposal failed: " + Unwrap(exception).Message);
			}
		}
		finally
		{
			state.activity_.InvalidateConnection(Time.unscaledTime, RetryInterval);
			try
			{
				DiscordField?.SetValue(controller, null);
			}
			catch (Exception exception)
			{
				if (logFailure)
				{
					Plugin.Log?.LogWarning("Could not clear Discord SDK reference: " + exception.GetBaseException().Message);
				}
			}
		}
	}

	private static void LogRateLimited(RuntimeState state, string message, bool warning)
	{
		float now = Time.unscaledTime;
		if (now - state.lastLogTime_ < RetryInterval)
		{
			return;
		}
		state.lastLogTime_ = now;
		if (warning)
		{
			Plugin.Log?.LogWarning(message);
		}
		else
		{
			Plugin.Log?.LogInfo(message);
		}
	}

	private static Exception Unwrap(Exception exception)
	{
		return exception is TargetInvocationException invocation && invocation.InnerException != null
			? invocation.InnerException
			: exception.GetBaseException();
	}

	private sealed class RuntimeState
	{
		internal readonly DiscordActivityState activity_ = new DiscordActivityState();
		internal readonly DiscordConnectionHealth health_ = new DiscordConnectionHealth();
		internal string details_ = string.Empty;
		internal string state_ = string.Empty;
		internal bool quitting_;
		internal float nextRetryTime_;
		internal float lastLogTime_ = -RetryInterval;
	}

	private sealed class ActivityCallbackTarget
	{
		private readonly RuntimeState state_;
		private readonly DiscordActivityState.RequestSnapshot request_;

		internal ActivityCallbackTarget(RuntimeState state, DiscordActivityState.RequestSnapshot request)
		{
			state_ = state;
			request_ = request;
		}

		private void OnResult<T>(T result)
		{
			OnActivityResult(state_, request_, result);
		}
	}
}

[HarmonyPatch(typeof(DiscordController), "Start")]
internal static class PatchDiscordControllerStart
{
	[HarmonyPrefix]
	private static bool Prefix(DiscordController __instance)
	{
		return PatchDiscordControllerRuntime.Start(__instance);
	}
}

[HarmonyPatch(typeof(DiscordController), "Update")]
internal static class PatchDiscordControllerUpdate
{
	[HarmonyPrefix]
	private static bool Prefix(DiscordController __instance)
	{
		return PatchDiscordControllerRuntime.Update(__instance);
	}
}

[HarmonyPatch(typeof(DiscordController), "UpdateActivity")]
internal static class PatchDiscordControllerActivity
{
	[HarmonyPrefix]
	private static bool Prefix(DiscordController __instance, string details, string state)
	{
		return PatchDiscordControllerRuntime.UpdateActivity(__instance, details, state);
	}
}

[HarmonyPatch(typeof(DiscordController), "OnApplicationQuit")]
internal static class PatchDiscordControllerQuit
{
	[HarmonyPrefix]
	private static bool Prefix(DiscordController __instance)
	{
		return PatchDiscordControllerRuntime.OnApplicationQuit(__instance);
	}
}
