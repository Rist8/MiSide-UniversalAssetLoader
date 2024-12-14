using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

public static class PluginInfo
{
	public const string PLUGIN_GUID = "UniversalAssetLoader";
	public const string PLUGIN_NAME = "Universal Asset Loader";
	public const string PLUGIN_VERSION = "1.1.0";

	public static PluginLoader Instance;
	public static string AssetsFolder = Paths.PluginPath + "\\" + PluginInfo.PLUGIN_GUID + "\\Assets";
}

[BepInPlugin("org.miside.plugins.assetloader", PluginInfo.PLUGIN_NAME, "1.1.0")]
public class PluginLoader : BasePlugin
{
	public ManualLogSource Logger { get; private set; }

	public PluginLoader() {}

	public override void Load()
	{
		Logger = (this as BasePlugin).Log;
		PluginInfo.Instance = this;
		IL2CPPChainloader.AddUnityComponent(typeof(Plugin));
	}
}
