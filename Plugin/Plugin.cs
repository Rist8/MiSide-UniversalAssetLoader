using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Diagnostics;
using System.Collections.Concurrent;
using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine.Playables;
using Il2CppSystem.Windows.Forms;
using static UtilityNamespace.LateCallUtility;

public class Plugin : MonoBehaviour
{
    public static string? currentSceneName;
    public static bool startup = true;

    private void Start()
    {
        ReadAssetsConfig();
        UtilityNamespace.LateCallUtility.Handler.StartCoroutine(LoadAssetsForPatchCoroutine());
        ReadAddonsConfigs();
        ConsoleMain.active = true;
        ConsoleMain.eventEnter = new UnityEvent();
        ConsoleMain.eventEnter.AddListener((UnityAction)(() => { ConsoleEnter(ConsoleMain.codeEnter); }));
    }
    static GameObject greenScreenCameraObject = null;
    static Camera greenScreenCamera = null;
    public static Dictionary<string, bool> Active = new Dictionary<string, bool>();
    private static float dx = 0.0f, dy = 0.0f, dz = 0.0f, rdx = 0.0f, rdy = 0.0f;
    public static List<string> AddonsConfig = new List<string>();
    public static void ConsoleEnter(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return;

        string[] parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1)
            return;

        // Capitalize specific keywords
        for (int i = 1; i < parts.Length; ++i)
        {
            switch (parts[i])
            {
                case "mita":
                case "sweater":
                case "attribute":
                case "skirt":
                case "pantyhose":
                case "body":
                case "hair":
                case "head":
                case "shoes":
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                    break;
            }
        }

        UnityEngine.Debug.Log("[INFO] Console in: " + s);

        assetCommands.RemoveAll(command =>
            command.name == parts[0] && command.args.SequenceEqual(parts.Skip(1)));

        if (!s.StartsWith("-"))
        {
            assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
        }

        UtilityNamespace.LateCallUtility.Handler.StartCoroutine(FindMitaCoroutine());

        assetCommands.RemoveAll(command =>
            command.name == parts[0] && command.args.SequenceEqual(parts.Skip(1)));

        if (parts[0] == "greenscreen")
        {
            HandleGreenScreenCommand(parts);
        }

        HandleAddonConfig(s);
    }

    private static void HandleGreenScreenCommand(string[] parts)
    {
        if (greenScreenCameraObject == null)
        {
            greenScreenCameraObject = new GameObject("GreenScreenCamera");
        }

        if (greenScreenCamera == null)
        {
            greenScreenCamera = greenScreenCameraObject.AddComponent<Camera>();
            greenScreenCamera.clearFlags = CameraClearFlags.SolidColor;
            greenScreenCamera.backgroundColor = Color.green;
        }

        // Set the camera's position and rotation defaults
        greenScreenCamera.transform.position = new Vector3(0.65f + dx, 1.6f + dy, 0.85f + dz);
        greenScreenCamera.transform.rotation = Quaternion.Euler(10 + rdx, -135 + rdy, 0);

        if (parts.Length == 2)
        {
            bool isActive = parts[1] != "off";
            greenScreenCameraObject.SetActive(isActive);
            SetGreenScreenObjectsActive(isActive);
        }
        else
        {
            greenScreenCameraObject.SetActive(true);
            SetGreenScreenObjectsActive(false);
        }

        if (parts.Length == 5 && parts[1] == "pos")
        {
            greenScreenCamera.transform.position = new Vector3(
                float.Parse(parts[2]) + 0.65f,
                float.Parse(parts[3]) + 1.6f,
                float.Parse(parts[4]) + 0.85f);
        }
        else if (parts.Length == 5 && parts[1] == "rot")
        {
            greenScreenCamera.transform.rotation = Quaternion.Euler(
                float.Parse(parts[2]) + 10,
                float.Parse(parts[3]) - 135,
                float.Parse(parts[4]));
        }
        else if (parts.Length == 9 && parts[1] == "pos" && parts[5] == "rot")
        {
            greenScreenCamera.transform.position = new Vector3(
                float.Parse(parts[2]) + 0.65f,
                float.Parse(parts[3]) + 1.6f,
                float.Parse(parts[4]) + 0.85f);

            greenScreenCamera.transform.rotation = Quaternion.Euler(
                float.Parse(parts[6]) + 10,
                float.Parse(parts[7]) - 135,
                float.Parse(parts[8]));
        }
    }

    private static void SetGreenScreenObjectsActive(bool isActive)
    {
        var partic = GameObject.Find("ParticlesBack");
        if (partic != null)
        {
            partic.SetActive(isActive);
        }

        var cyl = GameObject.Find("Cylinder");
        if (cyl != null)
        {
            cyl.SetActive(isActive);
        }
    }

    public static void ReadAddonsConfigs()
    {
        try
        {
            AddonsConfig.Clear();
            foreach (var file in AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "txt"))
            {
                if (file.ToLower().Contains("addons_config.txt"))
                {
                    UnityEngine.Debug.Log($"[INFO] Found addons config file: {Path.GetRelativePath(PluginInfo.AssetsFolder, file)}");
                    var lines = File.ReadAllLines(file).ToList();
                    AddonsConfig.AddRange(lines);
                }
            }


        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    private static void HandleAddonConfig(string s)
    {
        try
        {
            foreach (var line in AddonsConfig)
            {
                if (line.StartsWith("*"))
                {
                    string command = line.Substring(1).ToLower();
                    if (s == command)
                    {
                        ClothesMenuPatcher.LogOnClick(line.Substring(1));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    public static Dictionary<string, Assimp.Mesh[]>? loadedModels;
    public static Dictionary<string, Texture2D>? loadedTextures;
    public static Dictionary<string, AudioClip>? loadedAudio;
    public static List<(string name, string[] args)> assetCommands;


    void ReadAssetsConfig()
    {
        string filePath = Path.Combine(PluginInfo.AssetsFolder, "assets_config.txt");
        assetCommands = new List<(string name, string[] args)>();

        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                // Ignore empty or commented lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                // Split line on commands with arguments list
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    void ReadActiveAddons()
    {
        string filePath = Path.Combine(PluginInfo.AssetsFolder, "active_mods.txt");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "");
            return;
        }

        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                UnityEngine.Debug.Log($"[INFO] Active mod: {line}");
                try
                {
                    ClothesMenuPatcher.ClickAddonButton(line);
                }
                catch (System.Exception)
                {
                    UnityEngine.Debug.Log($"[ERROR] Failed to load mod: {line}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    static bool loaded = false;

    static System.Collections.IEnumerator LoadAssetsForPatchCoroutine()
    {
        if (loadedModels != null) yield break;
        loaded = false;

        loadedModels = new Dictionary<string, Assimp.Mesh[]>();
        loadedTextures = new Dictionary<string, Texture2D>();
        loadedAudio = new Dictionary<string, AudioClip>();

        PluginInfo.Instance.Logger.LogInfo($"Processor count : {Environment.ProcessorCount}");
        var stopwatch = Stopwatch.StartNew();
        float frameStartTime = Time.realtimeSinceStartup;


        var audioFiles = AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "ogg");
        foreach (var file in audioFiles)
        {
            // Load audio files
            AudioClip audioFile = null;
            yield return AssetLoader.LoadAudioCoroutine(Path.GetFileNameWithoutExtension(file), File.OpenRead(file), clip => audioFile = clip);
            audioFile.hideFlags = HideFlags.DontSave;

            string filename = Path.GetRelativePath(PluginInfo.AssetsFolder, file);
            filename = Path.ChangeExtension(filename, null);
            if (!loadedAudio.ContainsKey(filename))
            {
                loadedAudio.Add(filename, audioFile);
                PluginInfo.Instance.Logger.LogInfo($"Loaded audio from file: '{filename}'");
            }
        }
        PluginInfo.Instance.Logger.LogInfo($"Loaded all audio in {stopwatch.ElapsedMilliseconds}ms");

        // Load model files
        stopwatch.Restart();
        var modelFiles = AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "fbx");
        var loadedModelsLocal = new ConcurrentDictionary<string, Assimp.Mesh[]>();

        int maxParallelism = Math.Max(Environment.ProcessorCount - 1, 1);

        foreach (var fileBatch in SplitIntoBatches(modelFiles, maxParallelism))
        {
            Parallel.ForEach(fileBatch, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, file =>
            {
                var meshes = AssetLoader.LoadFBX(file);
                string filename = Path.GetRelativePath(PluginInfo.AssetsFolder, file);
                filename = Path.ChangeExtension(filename, null);
                loadedModelsLocal.TryAdd(filename, meshes);
                PluginInfo.Instance.Logger.LogInfo($"Loaded meshes from file: '{filename}', {meshes.Length} meshes");
            });

            foreach (var kvp in loadedModelsLocal)
            {
                if (!loadedModels.ContainsKey(kvp.Key))
                {
                    loadedModels.Add(kvp.Key, kvp.Value);
                }
            }
            loadedModelsLocal.Clear();

            // Yield control if needed
            if ((Time.realtimeSinceStartup - frameStartTime) * 1000 > 30)
            {
                stopwatch.Stop(); // Pause the stopwatch
                yield return null; // Yield control back to Unity
                frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
                stopwatch.Start(); // Resume the stopwatch
            }
        }
        PluginInfo.Instance.Logger.LogInfo($"Loaded all meshes in {stopwatch.ElapsedMilliseconds}ms");

        // Load texture files
        stopwatch.Restart();
        var textureFiles = AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "png", "jpg", "jpeg");
        foreach (var file in textureFiles)
        {
            var texture = AssetLoader.LoadTexture(file);
            if (texture != null)
            {
                string filename = Path.GetRelativePath(PluginInfo.AssetsFolder, file);
                filename = Path.ChangeExtension(filename, null);
                if (!loadedTextures.ContainsKey(filename))
                {
                    loadedTextures.Add(filename, texture);
                    PluginInfo.Instance.Logger.LogInfo($"Loaded texture from file: '{filename}'");
                }
            }

            // Yield every N files or if processing takes longer than a threshold
            if ((Time.realtimeSinceStartup - frameStartTime) * 1000 > 30)
            {
                stopwatch.Stop(); // Pause the stopwatch
                yield return null; // Yield control back to Unity
                frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
                stopwatch.Start(); // Resume the stopwatch
            }
        }
        PluginInfo.Instance.Logger.LogInfo($"Loaded all textures in {stopwatch.ElapsedMilliseconds}ms");
        loaded = true;
    }

    private static IEnumerable<List<string>> SplitIntoBatches(IEnumerable<string> files, int batchSize)
    {
        var batch = new List<string>(batchSize);
        foreach (var file in files)
        {
            batch.Add(file);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<string>(batchSize);
            }
        }
        if (batch.Count > 0)
        {
            yield return batch;
        }
    }



    public static string[] mitaNames = { "Usual", "MitaTrue", "ShortHairs", "Kind", "Cap",
        "Little", "Maneken", "Black", "Dreamer", "Mila",
        "Creepy", "Core", "MitaGame", "MitaPerson Mita", "Dream",
        "Future", "Broke", "Glasses", "MitaPerson Future", "CreepyMita",
        "Old", "MitaPerson Old", "MitaTrue(Clone)", "MitaChibi(Clone)", "Chibi", "MitaShortHairs(Clone)", "MitaKind(Clone)",
        "MitaCap(Clone)", "MitaLittle(Clone)", "MitaManeken(Clone)", "MitaBlack(Clone)", "MitaDreamer(Clone)",
        "Mila(Clone)", "MitaCreepy(Clone)", "MitaCore(Clone)", "IdleHide", "Mita"
    };


    public static List<GameObject> mitas = new List<GameObject>();

    public static System.Collections.IEnumerator FindMitaCoroutine(string modName = "", bool disactivation = false)
    {
        var animators = Reflection.FindObjectsOfType<Animator>(true);
        List<GameObject> mitaAnimators = new List<GameObject>();

        mitas.Clear();

        foreach (var obj in animators)
        {
            var anim = obj.Cast<Animator>();
            var runtimeController = anim.runtimeAnimatorController;

            if (runtimeController != null)
            {
                for (int i = 0; i < mitaNames.Length; ++i)
                {
                    string mitaName = mitaNames[i];
                    if (runtimeController.name.Contains(mitaName) || obj.name.Contains(mitaName))
                    {
                        if (mitaAnimators.Contains(anim.gameObject))
                            continue;
                        mitaAnimators.Add(anim.gameObject);
                        break;
                    }
                }
            }

        }


        // Patch each Mita over multiple frames
        for (int i = 0; i < mitaNames.Length; ++i)
        {
            string mitaName = mitaNames[i];
            string fullName = mitaName;

            if (mitaAnimators.Count <= i)
                continue;
            mitas.Add(mitaAnimators[i]);
            yield return PatchMitaCoroutine(modName, mitas[i], false, disactivation);

        }
    }


    public static void CreateMeshBackup(Dictionary<string, SkinnedMeshRenderer> renderers)
    {
        foreach (var renderer in renderers)
        {
            if (renderer.Value.transform.parent.Find(renderer.Value.name + "_backup") != null)
                return;

            var objSkinned = UnityEngine.Object.Instantiate(renderer.Value, renderer.Value.transform.position, renderer.Value.transform.rotation, renderer.Value.transform.parent);
            objSkinned.name = renderer.Value.name + "_backup";
            objSkinned.material = new Material(renderer.Value.material);
            objSkinned.gameObject.SetActive(false);
        }
    }

    public static System.Collections.IEnumerator RestoreMeshBackupCoroutine(
        string modName,
        Dictionary<string, SkinnedMeshRenderer> skinnedRenderers,
        Dictionary<string, MeshRenderer> staticRenderers
    )
    {
        UnityEngine.Debug.Log($"[INFO] Attempt to remove mod: '{modName}'");

        var skinnedAppendix = new HashSet<string>();
        var staticAppendix = new HashSet<string>();
        var replacedMeshes = new HashSet<string>();
        var removedMeshes = new HashSet<string>();

        bool found = false;
        string currentName = "";

        // Parse `AddonsConfig` line by line (this can be optimized further if needed)
        foreach (string line in AddonsConfig)
        {
            if (line.StartsWith("*"))
            {
                currentName = line.Substring(1);
                if (found)
                    break;
                continue;
            }
            if (line.StartsWith("-") || string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                continue;

            if (currentName == modName)
            {
                found = true;
                string[] parts1 = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts1.Length > 2)
                {
                    switch (parts1[0])
                    {
                        case "create_static_appendix":
                            staticAppendix.Add(parts1[2]);
                            break;
                        case "create_skinned_appendix":
                            skinnedAppendix.Add(parts1[2]);
                            break;
                        case "replace_mesh":
                        case "replace_tex":
                            if (!skinnedAppendix.Contains(parts1[2]) && !staticAppendix.Contains(parts1[2]))
                                replacedMeshes.Add(parts1[2]);
                            break;
                        case "remove":
                            if (!skinnedAppendix.Contains(parts1[2]) && !staticAppendix.Contains(parts1[2]) && !replacedMeshes.Contains(parts1[2]))
                                removedMeshes.Add(parts1[2]);
                            break;
                    }
                }
            }
        }

        // Process skinned renderers
        int processedCount = 0;
        foreach (var renderer in skinnedRenderers.Values)
        {
            if (skinnedAppendix.Contains(renderer.name))
            {
                UnityEngine.Object.Destroy(renderer.gameObject);
                skinnedAppendix.Remove(renderer.name);
                processedCount++;
                continue;
            }

            if (replacedMeshes.Contains(renderer.name))
            {
                var backup = renderer.transform.parent.Find(renderer.name + "_backup");
                if (backup != null)
                {
                    var backupRenderer = backup.GetComponent<SkinnedMeshRenderer>();
                    var armatureBackup = new AssetLoader.ArmatureData(backupRenderer);
                    var armature = new AssetLoader.ArmatureData(renderer);

                    renderer.sharedMesh = backupRenderer.sharedMesh;
                    renderer.material = backupRenderer.material;

                    for (int i = 0; i < armature.clothNodes.Count; ++i)
                    {
                        armature.clothNodes[i].cullRendererList = armatureBackup.clothNodes[i].cullRendererList;
                    }

                    renderer.gameObject.SetActive(true);
                }

                replacedMeshes.Remove(renderer.name);
                processedCount++;
                continue;
            }

            if (removedMeshes.Contains(renderer.name))
            {
                renderer.gameObject.SetActive(true);
                removedMeshes.Remove(renderer.name);
                processedCount++;
                continue;
            }

            // Yield control every 10 renderers to avoid freezing
            if (processedCount % 10 == 0)
            {
                yield return null;
            }
        }

        // Process static renderers
        foreach (var renderer in staticRenderers.Values)
        {
            if (staticAppendix.Contains(renderer.name))
            {
                UnityEngine.Object.Destroy(renderer.gameObject);
                staticAppendix.Remove(renderer.name);
                processedCount++;
                continue;
            }

            if (replacedMeshes.Contains(renderer.name))
            {
                var backup = renderer.transform.parent.Find(renderer.name + "_backup");
                if (backup != null)
                {
                    renderer.GetComponent<MeshFilter>().sharedMesh = backup.GetComponent<MeshFilter>().sharedMesh;
                    renderer.material = backup.GetComponent<MeshRenderer>().material;
                    renderer.gameObject.SetActive(true);
                }

                replacedMeshes.Remove(renderer.name);
                processedCount++;
                continue;
            }

            if (removedMeshes.Contains(renderer.name))
            {
                renderer.gameObject.SetActive(true);
                removedMeshes.Remove(renderer.name);
                processedCount++;
                continue;
            }

            // Yield control every 10 renderers to avoid freezing
            if (processedCount % 10 == 0)
            {
                yield return null;
            }
        }

        UnityEngine.Debug.Log($"[INFO] Mod '{modName}' deactivated successfully.");
    }

    // Global dictionary to track applied commands per object
    public static Dictionary<GameObject, HashSet<string>> globalAppliedCommands = new();

    public static System.Collections.IEnumerator PatchMitaCoroutine(string modName, GameObject mita, bool recursive = false, bool disactivation = false)
    {
        var stopwatch = Stopwatch.StartNew();
        float frameStartTime = Time.realtimeSinceStartup;

        if (!globalAppliedCommands.ContainsKey(mita))
        {
            globalAppliedCommands[mita] = new HashSet<string>();
        }

        if (mita.name == "MitaTrue(Clone)" && !recursive)
        {
            yield return PatchMitaCoroutine(modName, mita.transform.Find("MitaUsual").gameObject, true, disactivation);
            mita = mita.transform.Find("MitaTrue").gameObject;
        }

        var renderersList = Reflection.GetComponentsInChildren<SkinnedMeshRenderer>(mita, true);
        var staticRenderersList = Reflection.GetComponentsInChildren<MeshRenderer>(mita, true);
        var renderers = new Dictionary<string, SkinnedMeshRenderer>();
        var staticRenderers = new Dictionary<string, MeshRenderer>();

        foreach (var renderer in renderersList)
            renderers[renderer.name.Trim()] = renderer;

        foreach (var renderer in staticRenderersList)
            staticRenderers[renderer.name.Trim()] = renderer;

        if (currentSceneName == "SceneMenu")
        {
            CreateMeshBackup(renderers);
            if (disactivation)
            {
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(RestoreMeshBackupCoroutine(modName, renderers, staticRenderers));
            }
        }

        foreach (var command in assetCommands)
        {
            if (command.args.Length == 0 || command.args[0] != "Mita")
                continue;

            string commandKey = $"{command.name} {string.Join(" ", command.args)}";

            if (globalAppliedCommands[mita].Contains(commandKey))
            {
                UnityEngine.Debug.Log($"[INFO] Skipping already applied command: {commandKey} on '{mita.name}'");
                continue;
            }

            switch (command.name)
            {
                case "remove":
                    Commands.ApplyRemoveCommand(command, mita, renderers, staticRenderers);
                    break;
                case "recover":
                    Commands.ApplyRecoverCommand(command, mita, renderers, staticRenderers);
                    break;
                case "replace_tex":
                    Commands.ApplyReplaceTexCommand(command, mita, renderers, staticRenderers);
                    break;
                case "replace_mesh":
                    yield return Commands.ApplyReplaceMeshCommandCoroutine(command, mita, renderers, staticRenderers, mita.name);
                    break;
                case "resize_mesh":
                    Commands.ApplyResizeMeshCommand(command, mita, renderers, staticRenderers);
                    break;
                case "create_skinned_appendix":
                    Commands.ApplyCreateSkinnedAppendixCommand(command, mita, renderers);
                    break;
                case "create_static_appendix":
                    Commands.ApplyCreateStaticAppendixCommand(command, mita, staticRenderers);
                    break;
                case "set_scale":
                    Commands.ApplySetScaleCommand(command, mita);
                    break;
                case "move_position":
                    Commands.ApplyMovePositionCommand(command, mita);
                    break;
                case "set_rotation":
                    Commands.ApplySetRotationCommand(command, mita);
                    break;
                case "shader_params":
                    Commands.ApplyShaderParamsCommand(command, mita, renderers, staticRenderers);
                    break;
                case "remove_outline":
                    Commands.ApplyRemoveOutlineCommand(command, mita, renderers, staticRenderers);
                    break;
                case "recover_outline":
                    Commands.ApplyAddOutlineCommand(command, mita, renderers, staticRenderers);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"[WARNING] Unknown command: {command.name}");
                    break;
            }

            globalAppliedCommands[mita].Add(commandKey);

            // Yield control every 15ms to avoid freezing
            if ((Time.realtimeSinceStartup - frameStartTime) > 1f / 240f)
            {
                stopwatch.Stop(); // Pause the stopwatch
                yield return null; // Yield control back to Unity
                frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
                stopwatch.Start(); // Resume the stopwatch
            }
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"[INFO] Patched '{mita.name}' in {stopwatch.ElapsedMilliseconds}ms.");
    }


    public static void FindPlayer()
    {
        var animators = Reflection.FindObjectsOfType<Animator>(true);
        foreach (var animator in animators)
        {
            if (animator.name.Contains("Person") || animator.name.Contains("Player"))
            {
                GameObject personObject = animator.gameObject;
                UnityEngine.Debug.Log($"[INFO] Found 'Person' object with Animator: {personObject.name}");
                PatchPlayer(personObject);
                // return;
            }
        }
    }

    public static void PatchPlayer(GameObject player)
    {
        var stopwatch = Stopwatch.StartNew();

        // Ensure the object is tracked in the global dictionary
        if (!Plugin.globalAppliedCommands.ContainsKey(player))
            Plugin.globalAppliedCommands[player] = new HashSet<string>();

        var renderers = new Dictionary<string, SkinnedMeshRenderer>();
        var staticRenderers = new Dictionary<string, MeshRenderer>();

        var skinnedRenderers = player.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in skinnedRenderers)
            renderers[renderer.name.Trim()] = renderer;

        var meshRenderers = player.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
            staticRenderers[renderer.name.Trim()] = renderer;

        foreach (var command in Plugin.assetCommands)
        {
            if (command.args.Length == 0 || command.args[0] != "Player")
                continue;

            string commandKey = $"{command.name} {string.Join(" ", command.args)}";

            // Check the globalAppliedCommands dictionary to skip already applied commands
            if (Plugin.globalAppliedCommands[player].Contains(commandKey))
            {
                UnityEngine.Debug.Log($"[INFO] Skipping already applied command: {commandKey} on '{player.name}'.");
                continue;
            }

            try
            {
                switch (command.name)
                {
                    case "remove":
                        Commands.ApplyRemoveCommand(command, player, renderers, staticRenderers);
                        break;
                    case "recover":
                        Commands.ApplyRecoverCommand(command, player, renderers, staticRenderers);
                        break;
                    case "replace_tex":
                        Commands.ApplyReplaceTexCommand(command, player, renderers, staticRenderers);
                        break;
                    case "replace_mesh":
                        Commands.ApplyReplaceMeshCommandCoroutine(command, player, renderers, staticRenderers, "Player");
                        break;
                    case "resize_mesh":
                        Commands.ApplyResizeMeshCommand(command, player, renderers, staticRenderers);
                        break;
                    case "create_skinned_appendix":
                        Commands.ApplyCreateSkinnedAppendixCommand(command, player, renderers, true);
                        break;
                    case "create_static_appendix":
                        Commands.ApplyCreateStaticAppendixCommand(command, player, staticRenderers);
                        break;
                    case "set_scale":
                        Commands.ApplySetScaleCommand(command, player);
                        break;
                    case "move_position":
                        Commands.ApplyMovePositionCommand(command, player);
                        break;
                    case "set_rotation":
                        Commands.ApplySetRotationCommand(command, player);
                        break;
                    default:
                        UnityEngine.Debug.LogWarning($"[WARNING] Unknown command: {command.name}");
                        break;
                }

                // Mark command as applied in the global dictionary
                Plugin.globalAppliedCommands[player].Add(commandKey);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ERROR] Error processing command: {commandKey} on '{player.name}'.\n{e}");
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log($"[INFO] Patched '{player.name}' in {stopwatch.ElapsedMilliseconds}ms.");
        }
    }

    VideoPlayer currentVideoPlayer = null;
    Action onCurrentVideoEnded = null;
    Image logo = null;
    Transform sceneObjectTransform = null;

    void PatchMenuScene()
    {
        sceneObjectTransform = GameObject.Find("MenuGame/Scene").transform;
        gameObjectCount = sceneObjectTransform.childCount;
        UnityEngine.Debug.Log($"[INFO] Patching game scene.");
        var command = assetCommands.FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_logo", (null, null));
        if (command.name != null)
        {
            var animators = Reflection.FindObjectsOfType<Animator>(true);
            GameObject gameName = null;
            foreach (var obj in animators)
                if (obj.name == "NameGame")
                {
                    gameName = obj.Cast<Animator>().gameObject;
                    Destroy(obj);
                    break;
                }

            for (int i = 0; i < gameName.transform.childCount; i++)
            {
                var tr = gameName.transform.GetChild(i);
                if (tr.name == "Background")
                {
                    Texture2D tex = loadedTextures[command.args[0]];
                    Destroy(Reflection.GetComponent<UIShiny>(tr));

                    logo = Reflection.GetComponent<Image>(tr);
                    logo.preserveAspect = true;
                    logo.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2.0f);
                    Reflection.GetComponent<RectTransform>(tr).sizeDelta = new Vector2(1600, 400);
                }
                else
                    tr.gameObject.SetActive(false);
            }
        }

        command = assetCommands.FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_music", (null, null));
        if (command.name != null)
        {
            var musicSources = Reflection.FindObjectsOfType<AudioSource>(true);
            foreach (var source in musicSources)
                if (source.name == "Music")
                {
                    source.clip = loadedAudio[command.args[0]];
                    source.volume = GlobalGame.VolumeGame;
                    source.Play();
                    break;
                }
        }

        ClothesMenuPatcher.Run();

        UnityEngine.Debug.Log($"[INFO] Game scene patching completed.");

        if (startup)
        {
            ReadActiveAddons();
            startup = false;
        }
    }

    private static float baseMovementSpeed = 0.03f;
    private static float maxMovementSpeed = 0.1f;
    private static float mouseSensitivity = 0.7f;

    private static int gameObjectCount = 0;

    public static System.Collections.IEnumerator SceneLoading(string sceneName)
    {
        // Start the scene loading process
        UnityEngine.Debug.Log("Waiting for assets loading...");

        var sceneLoading = Reflection.FindObjectsOfType<SceneLoading>(true)[0];

        bool initialState = sceneLoading.loadReady;

        // Wait until assets are loaded
        while (!loaded)
        {
            if (sceneLoading.loadReady)
                initialState = true;
            sceneLoading.loadReady = false;
            yield return null; // Keep yielding until assets are fully loaded
        }

        sceneLoading.loadReady = initialState;

        UnityEngine.Debug.Log("Assets have been loaded, activating scene...");

    }


    void Update()
    {

        if (ConsoleMain.liteVersion)
        {
            ConsoleMain.liteVersion = false;
        }

        if (currentSceneName != SceneManager.GetActiveScene().name)
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            OnSceneChanged();
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
        {
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(LoadAssetsForPatchCoroutine());
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(FindMitaCoroutine());
            FindPlayer();
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
        {
            if (greenScreenCameraObject == null || !greenScreenCameraObject.active)
                ConsoleEnter("greenscreen");
            else
                ConsoleEnter("greenscreen off");
        }

        if (greenScreenCameraObject != null && greenScreenCameraObject.active)
        {
            // Mouse Input for Rotation
            float mouseX = UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Update rotation deltas
            rdy += mouseX; // Horizontal rotation (Y-axis)
            rdx -= mouseY; // Vertical rotation (X-axis)

            // Clamp the vertical rotation to prevent flipping
            rdx = Mathf.Clamp(rdx, -90f, 90f);

            // Movement Input
            Vector3 forward = greenScreenCamera.transform.forward;
            Vector3 right = greenScreenCamera.transform.right;
            Vector3 up = greenScreenCamera.transform.up;

            forward.y = 0; // Ignore vertical component for planar movement
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            // Adjust speed if Shift is held
            if (UnityEngine.Input.GetKey(KeyCode.LeftShift))
            {
                baseMovementSpeed = Mathf.Min(baseMovementSpeed * 1.02f, maxMovementSpeed);
            }
            else
            {
                baseMovementSpeed = 0.03f;
            }

            Vector3 movement = Vector3.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W)) movement += forward * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.S)) movement -= forward * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.D)) movement += right * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.A)) movement -= right * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.Space)) movement += up * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl)) movement -= up * baseMovementSpeed;

            // Update Camera Transform
            Vector3 newPosition = greenScreenCamera.transform.position + movement;
            Quaternion newRotation = Quaternion.Euler(rdx, -135 + rdy, 0);

            greenScreenCamera.transform.SetPositionAndRotation(newPosition, newRotation);
        }

        if (currentVideoPlayer != null)
        {
            if ((ulong)currentVideoPlayer.frame + 5 > currentVideoPlayer.frameCount)
            {
                UnityEngine.Debug.Log($"[INFO] Video playback ended.");
                Destroy(currentVideoPlayer.transform.parent.gameObject);
                currentVideoPlayer = null;
                onCurrentVideoEnded?.Invoke();
                onCurrentVideoEnded = null;
            }
        }

        if (currentSceneName == "SceneMenu")
        {
            if (logo != null)
                logo.color = Color.white;
            if (sceneObjectTransform.childCount != gameObjectCount)
            {
                gameObjectCount = sceneObjectTransform.childCount;
                if (sceneObjectTransform.childCount == 6 && !sceneObjectTransform.GetChild(5).gameObject.name.Contains("Particle"))
                    UtilityNamespace.LateCallUtility.Handler.StartCoroutine(FindMitaCoroutine());
            }
        }
    }
    void OnSceneChanged()
    {
        try
        {
            UnityEngine.Debug.Log($"[INFO] Scene changed to: {currentSceneName}.");
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(LoadAssetsForPatchCoroutine());
            globalAppliedCommands.Clear();
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(FindMitaCoroutine());
            FindPlayer();
            if (currentSceneName == "SceneMenu")
                PatchMenuScene();
            else if (currentSceneName == "SceneLoading")
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(SceneLoading("SceneMenu"));
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[ERROR] {e}");
            enabled = false;
        }
    }

    void PlayFullscreenVideo(Action onVideoEnd)
    {
        var rootGO = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in rootGO)
            if (go != gameObject) go.gameObject.SetActive(false);

        Camera camera = new GameObject("VideoCamera").AddComponent<Camera>();
        VideoPlayer videoPlayer = new GameObject("VideoPlayer").AddComponent<VideoPlayer>();
        videoPlayer.transform.parent = camera.transform;
        camera.backgroundColor = Color.black;
        camera.gameObject.AddComponent<AudioListener>();
        videoPlayer.playOnAwake = false;
        videoPlayer.targetCamera = camera;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.url = "file://" + PluginInfo.AssetsFolder + "/intro.mp4";
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, videoPlayer.gameObject.AddComponent<AudioSource>());

        currentVideoPlayer = videoPlayer;
        onCurrentVideoEnded = onVideoEnd;

        videoPlayer.Play();
        UnityEngine.Debug.Log($"[INFO] Video playback started.");
    }
}
