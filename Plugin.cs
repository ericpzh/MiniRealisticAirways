using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniRealisticAirways;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static ManualLogSource Log;

	internal static bool showText_ = true;

	internal static WindSock windsock_;

	internal static EventManager eventManager_;

	internal const int MAX_WHILE_LOOP_ITER = 1000;

	// Harmony and scene callbacks are process-lifetime services. BepInEx normally
	// keeps BaseUnityPlugin objects alive, but some Unity/Linux launch orders
	// destroy that host while retaining the game scene. Keeping these references
	// static prevents a host teardown from silently disabling the mod.
	private static Harmony harmony_;

	private static bool harmonyInstalled_;

	private static bool sceneHookInstalled_;

	private static int initializedSceneHandle_ = int.MinValue;

	private static GameObject mapServicesRoot_;

	private void Awake()
	{
		Log = base.Logger;
		Log.LogInfo("Plugin MiniRealisticAirways is loaded (build " + PluginInfo.BUILD_ID + ").");
		if (!ModLocalization.ValidateCatalog(out string catalogError))
		{
			Log.LogError("MiniRealisticAirways localization catalog validation failed: " + catalogError);
		}
		// Subscribe once to the game's locale service before any cloned UI is
		// created. The catalog is embedded in this DLL and has no MapPort
		// dependency; subscribers update only their cached labels on a change.
		ModLocalization.Initialize();
		InstallSceneHook();
		InstallHarmony();
		ProcessLaunchOptions();
		// BepInEx may be injected after the first sceneLoaded notification. Make a
		// best-effort pass immediately; authoritative game Start/Update patches
		// retry when their objects are actually ready.
		InitializeScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
		Log.LogInfo("Initial scene bootstrap completed.");
	}

	private static void InstallSceneHook()
	{
		if (sceneHookInstalled_)
		{
			return;
		}
		SceneManager.sceneLoaded += OnSceneLoaded;
		sceneHookInstalled_ = true;
	}

	private void InstallHarmony()
	{
		if (harmonyInstalled_)
		{
			Log.LogDebug("Harmony patches were already installed; retaining the process-lifetime patch set.");
			return;
		}
		try
		{
			harmony_ = new Harmony(PluginInfo.PLUGIN_GUID);
			harmony_.PatchAll();
			harmonyInstalled_ = true;
			Log.LogInfo("Harmony patches installed.");
		}
		catch (Exception exception)
		{
			Log.LogError("Harmony patch installation failed; continuing with available features: " + exception.GetBaseException().Message);
		}
	}

	private static void ProcessLaunchOptions()
	{
		try
		{
			Settings.ProcessLaunchOptions();
			Log?.LogInfo("Launch options processed.");
		}
		catch (Exception exception)
		{
			Log?.LogError("Launch option processing failed; using defaults: " + exception.GetBaseException().Message);
		}
	}

	// Kept private and named for the MapPort soft-compatibility bridge. The
	// method is static so it remains callable even if Unity destroys the plugin
	// host object during a scene transition.
	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		InitializeScene(scene, mode);
	}

	private static void InitializeScene(Scene scene, LoadSceneMode mode)
	{
		if (!scene.IsValid() || initializedSceneHandle_ == scene.handle)
		{
			return;
		}
		initializedSceneHandle_ = scene.handle;
		if (mode == LoadSceneMode.Single)
		{
			ResetSceneState();
		}
		Log?.LogInfo("Scene loaded: " + scene.name);
		if (scene.name == "Menu")
		{
			// MainMenuManager.Start is the normal path. This scene-load pass handles
			// late plugin injection when the manager already exists; its Update
			// postfix remains the final retry if the UI is still being built.
			MainMenuManager manager = UnityEngine.Object.FindObjectOfType<MainMenuManager>();
			if (manager != null)
			{
				if (MainMenuManagerPatch.TryEnsureButton(manager))
				{
					StartAutomaticTutorialIfReady();
				}
			}
			// OptionsManager is DontDestroyOnLoad. If this plugin was injected after
			// its Start callback, the Harmony postfix cannot run retroactively; make
			// one cached-field bootstrap pass for the already-live instance.
			OptionsManagerPatch.TryEnsureSetup(OptionsManager.instance);
		}
		if (IsMapScene(scene.name))
		{
			TryInitializeMapServices();
		}
	}

	private static bool IsMapScene(string sceneName)
	{
		return sceneName == "MapPlayer" || sceneName == "London" || sceneName == "CreatorPlayer";
	}

	private static void StartAutomaticTutorialIfReady()
	{
		if (AudioManager.instance != null)
		{
			AudioManager.instance.StartCoroutine(Tutorial.ShowTutorialCoroutine());
		}
	}

	private static void ResetSceneState()
	{
		AircraftVisualSortingRegistry.Reset();
		Tutorial.ResetSceneState();
		Settings.ResetSceneState();
		// Clear scene snapshots but retain runtime font atlases that are still used
		// by persistent menu controls; recreating CJK/Arabic atlases on every map
		// transition is unnecessary native-memory churn.
		ChineseTypography.ResetSceneState();
		AircraftHudLayoutController.ResetSharedSprite();
		windsock_ = null;
		eventManager_ = null;
		mapServicesRoot_ = null;
		PatchOnTriggerStay2D.ResetCameraCache();
		// Texture caches are scene-owned and can otherwise retain Unity objects
		// after a Restart/menu round-trip. These methods are idempotent.
		FuelGaugeTextures.DestroyTextures();
		GaugeArrowTexture.DestroyTexture();
		WeatherCellTextures.DestroyTextures();
	}

	internal static bool TryInitializeMapServices()
	{
		if (AircraftManager.Instance == null || UpgradeManager.Instance == null)
		{
			return false;
		}
		GameObject escButton = GameObject.Find("ESC_Button");
		if (escButton == null)
		{
			return false;
		}
		if (mapServicesRoot_ == escButton && windsock_ != null && eventManager_ != null)
		{
			return true;
		}
		try
		{
			FuelGaugeTextures.PreLoadTextures();
			GaugeArrowTexture.PreLoadTexture();
			WeatherCellTextures.PreLoadTextures();
			windsock_ = escButton.GetComponent<WindSock>();
			if (windsock_ == null)
			{
				windsock_ = escButton.AddComponent<WindSock>();
			}
			windsock_.windsock_ = escButton;
			windsock_.InitializeText();
			eventManager_ = escButton.GetComponent<EventManager>();
			if (eventManager_ == null)
			{
				eventManager_ = escButton.AddComponent<EventManager>();
			}
			if (escButton.GetComponent<TextVisibilityHotkey>() == null)
			{
				escButton.AddComponent<TextVisibilityHotkey>();
			}
			mapServicesRoot_ = escButton;
			Log?.LogInfo("Map services initialized from " + escButton.name + ".");
			return true;
		}
		catch (Exception exception)
		{
			Log?.LogError("Map service initialization failed: " + exception.GetBaseException().Message);
			return false;
		}
	}

	private void OnDestroy()
	{
		// Do not unsubscribe or unpatch here. On affected Linux launch orders this
		// callback can be caused by host teardown rather than process shutdown;
		// removing the process-lifetime hooks at this point makes the mod vanish.
		Log?.LogWarning("Plugin host object was destroyed; retaining Harmony and scene hooks for this process.");
	}
}
