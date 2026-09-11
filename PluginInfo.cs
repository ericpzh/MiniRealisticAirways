namespace MiniRealisticAirways;

public static class PluginInfo
{
	public const string PLUGIN_GUID = "MiniRealisticAirways";

	public const string PLUGIN_NAME = "MiniRealisticAirways";

	// BepInEx 5 parses this value with System.Version. Keep it numeric and
	// expose the descriptive build label separately for manifests and logs.
	public const string PLUGIN_VERSION = BuildInfo.Version;

	public const string BUILD_ID = BuildInfo.BuildId;
}
