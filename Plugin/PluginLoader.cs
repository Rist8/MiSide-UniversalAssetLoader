using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System.Reflection;
using HarmonyLib;

public static class PluginInfo
{
    public const string PLUGIN_GUID = "UniversalAssetLoader";
    public const string PLUGIN_NAME = "Universal Asset Loader";
    public const string PLUGIN_VERSION = "0.11.6";

    public static PluginLoader Instance;
    public static string AssetsFolder = Paths.PluginPath + "\\" + PluginInfo.PLUGIN_GUID + "\\Assets";
    public static string DependenciesFolder = Paths.PluginPath + "\\" + PluginInfo.PLUGIN_GUID + "\\Dependencies";
}

[BepInPlugin("org.miside.plugins.assetloader", PluginInfo.PLUGIN_NAME, "0.11.6")]
public class PluginLoader : BasePlugin
{
    public ManualLogSource Logger { get; private set; }

    public PluginLoader()
    {
        Logger = null!; // Initialize Logger to a non-null value to satisfy the compiler
    }

    public override void Load()
    {
        var harmony = new Harmony("org.miside.plugins.assetloader");
        harmony.PatchAll();

        Logger = (this as BasePlugin).Log;
        PluginInfo.Instance = this;

        var assimpPath = Path.Join(PluginInfo.DependenciesFolder, "assimp.dll");
        if (File.Exists(assimpPath))
        {
            Assimp.Unmanaged.AssimpLibrary.Instance.LoadLibrary(assimpPath);
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        IL2CPPChainloader.AddUnityComponent(typeof(Plugin));
        IL2CPPChainloader.AddUnityComponent(typeof(UtilityNamespace.LateCallUtility.CoroutineHandler));
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        // Directory containing custom assemblies
        string customDirectory = PluginInfo.DependenciesFolder;

        // Extract the assembly name
        var assemblyName = new AssemblyName(args.Name).Name;

        // Construct the path to the DLL
        string assemblyPath = Path.Combine(customDirectory, assemblyName + ".dll");

        // Check if the file exists and load it
        if (File.Exists(assemblyPath))
        {
            if (assemblyName == "assimp")
            {
                return null;
            }
            return Assembly.LoadFrom(assemblyPath);
        }

        return null; // Return null if not found
    }
}
