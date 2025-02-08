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
using static AssetLoader;
using Il2CppInterop.Runtime;
using System.Text.RegularExpressions;

public class Plugin : MonoBehaviour
{
    public static bool startup = true;
    public static int targetFrameRate = 120;

    private void Start()
    {
        int targetFrameRate = UnityEngine.Application.targetFrameRate;
        CheckDevMode();
        ReadAssetsConfig();
        UtilityNamespace.LateCallUtility.Handler.StartCoroutine(AssetLoader.LoadAssetsForPatchCoroutine());
        ReadAddonsConfigs();
        ConsoleCommandHandler.Initialize();
    }

    public static Dictionary<string, bool> Active = new Dictionary<string, bool>();
    public static List<string> AddonsConfig = new List<string>();

    public static void CheckDevMode()
    {
        if (System.IO.File.Exists(PluginInfo.AssetsFolder + "/devmode.txt"))
        {
            UnityEngine.Debug.LogError("[INFO] Developer mode enabled.");
            ClothesMenuPatcher.DeveloperMode = true;
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

    public static void HandleAddonConfig(string s)
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


    public static void ReadAssetsConfig()
    {
        string filePath = Path.Combine(PluginInfo.AssetsFolder, "assets_config.txt");
        ConsoleCommandHandler.assetCommands = new List<(string name, string[] args)>();

        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                // Ignore empty or commented lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                // Regex to match arguments, preserving quoted substrings
                var matches = Regex.Matches(line, @"[\""].+?[\""]|[^ ]+");
                var parts = matches.Cast<Match>().Select(m => m.Value).ToArray();

                // Remove surrounding quotes from quoted arguments
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = parts[i].Trim('\"');
                }

                // Add command and arguments to assetCommands
                ConsoleCommandHandler.assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
    }

    public static void ReadActiveAddons()
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

    public static System.Collections.IEnumerator PatchAssets()
    {
        float frameStartTime = Time.realtimeSinceStartup;
        var textures = Resources.FindObjectsOfTypeAll(Il2CppType.Of<Texture2D>());
        var textureDict = new Dictionary<string, Texture2D>();
        var sprites = Resources.FindObjectsOfTypeAll(Il2CppType.Of<Sprite>());
        var spritesDict = new Dictionary<string, Sprite>();
        var audios = Resources.FindObjectsOfTypeAll(Il2CppType.Of<AudioClip>());
        var audioDict = new Dictionary<string, AudioClip>();

        foreach (var texture in textures)
        {
            if (loadedTextureInstanceIds.Contains(texture.GetInstanceID()))
                continue;

            var tex = texture.Cast<Texture2D>();
            if (tex != null)
            {
                string baseName = tex.name;
                string uniqueName = baseName;
                int counter = 1;

                // Generate a unique name if a texture with the same name already exists
                while (textureDict.ContainsKey(uniqueName))
                {
                    uniqueName = $"{baseName}#{counter}";
                    counter++;
                }

                textureDict[uniqueName] = tex;
            }
        }


        if ((Time.realtimeSinceStartup - frameStartTime) > 1 / UnityEngine.Application.targetFrameRate)
        {
            yield return null; // Yield control back to Unity
            frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
        }

        foreach (var sprite in sprites)
        {
            var spr = sprite.Cast<Sprite>();
            if (spr != null)
            {
                string baseName = spr.name;
                string uniqueName = baseName;
                int counter = 1;

                // Generate a unique name if a sprite with the same name already exists
                while (audioDict.ContainsKey(uniqueName))
                {
                    uniqueName = $"{baseName}#{counter}";
                    counter++;
                }

                spritesDict[uniqueName] = spr;
            }
        }


        if ((Time.realtimeSinceStartup - frameStartTime) > 1 / UnityEngine.Application.targetFrameRate)
        {
            yield return null; // Yield control back to Unity
            frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
        }

        foreach (var audio in audios)
        {
            if (loadedAudioInstanceIds.Contains(audio.GetInstanceID()))
                continue;

            var aud = audio.Cast<AudioClip>();
            if (aud != null)
            {
                string baseName = aud.name;
                string uniqueName = baseName;
                int counter = 1;

                // Generate a unique name if a audio with the same name already exists
                while (audioDict.ContainsKey(uniqueName))
                {
                    uniqueName = $"{baseName}#{counter}";
                    counter++;
                }

                audioDict[uniqueName] = aud;
            }
        }


        if ((Time.realtimeSinceStartup - frameStartTime) > 1 / UnityEngine.Application.targetFrameRate)
        {
            yield return null; // Yield control back to Unity
            frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
        }

        foreach (var command in ConsoleCommandHandler.assetCommands)
        {
            if (command.args.Length == 0)
                continue;

            string commandKey = $"{command.name} {string.Join(" ", command.args)}";

            switch (command.name)
            {
                case "replace_2D":
                    Commands.ApplyReplace2DCommand(command, textureDict);
                    break;
                case "replace_sprite":
                    UtilityNamespace.LateCallUtility.Handler.StartCoroutine(Commands.ApplyReplaceSprite(command, spritesDict));
                    break;
                case "replace_audio":
                    Commands.ApplyReplaceAudioCommand(command, audioDict);
                    break;
            }

            if ((Time.realtimeSinceStartup - frameStartTime) > 1 / UnityEngine.Application.targetFrameRate)
            {
                yield return null; // Yield control back to Unity
                frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
            }
        }
    }

    public static bool loaded = false;


    public static string[] mitaNames = { "Usual", "MitaTrue", "ShortHairs", "Kind", "Cap",
        "Little", "Maneken", "Black", "Dreamer", "Mila",
        "Creepy", "Core", "MitaGame", "MitaPerson Mita", "Dream",
        "Future", "Broke", "Glasses", "MitaPerson Future", "CreepyMita",
        "Old", "MitaPerson Old", "MitaTrue(Clone)", "MitaChibi(Clone)", "Chibi", "MitaShortHairs(Clone)", "MitaKind(Clone)",
        "MitaCap(Clone)", "MitaLittle(Clone)", "MitaManeken(Clone)", "MitaBlack(Clone)", "MitaDreamer(Clone)",
        "Mila(Clone)", "MitaCreepy(Clone)", "MitaCore(Clone)", "IdleHide", "Mita"
    };


    public static List<GameObject> mitas = new List<GameObject>();

    private static List<GameObject> GetMitaAnimators()
    {
        var animators = Reflection.FindObjectsOfType<Animator>(true);
        var mitaAnimators = new List<GameObject>();

        foreach (var obj in animators)
        {
            var anim = obj.Cast<Animator>();
            var runtimeController = anim.runtimeAnimatorController;

            if (runtimeController != null)
            {
                foreach (var mitaName in mitaNames)
                {
                    if (runtimeController.name.Contains(mitaName) || obj.name.Contains(mitaName))
                    {
                        if (!mitaAnimators.Contains(anim.gameObject))
                        {
                            mitaAnimators.Add(anim.gameObject);
                            if (anim.gameObject.transform.parent.name.Contains("Car"))
                            {
                                mitaAnimators[mitaAnimators.Count - 1].name = "ChibiRacer";
                            }
                        }
                        break;
                    }
                }
            }
        }

        return mitaAnimators;
    }

    public static System.Collections.IEnumerator FindMitaCoroutine(string modName = "", bool disactivation = false)
    {
        var mitaAnimators = GetMitaAnimators();
        mitas.Clear();


        Transform player = null;

        if (GlobalTag.cameraPlayer != null)
        {
            player = GlobalTag.cameraPlayer.transform;
            mitaAnimators.Sort((x, y) => Vector3.Distance(player.position, x.transform.position).CompareTo(Vector3.Distance(player.position, y.transform.position)));
        }

        // Patch each Mita over multiple frames
        for (int i = 0; i < mitaNames.Length; ++i)
        {
            string mitaName = mitaNames[i];
            string fullName = mitaName;

            if (mitaAnimators.Count <= i || mitaAnimators[i] == null)
                continue;
            mitas.Add(mitaAnimators[i]);

            float distance = player != null ? Vector3.Distance(player.position, mitaAnimators[i].transform.position) : 0f;
            float maxFrameTime = distance <= 20 ? 1f / targetFrameRate : 1f / (targetFrameRate * Mathf.Log(distance - 19, 8));
            UnityEngine.Debug.Log(distance);
            yield return PatchMitaCoroutine(modName, mitaAnimators[i], false, disactivation, maxFrameTime);
        }
    }

    public static void FindMita(string modName = "", bool disactivation = false)
    {
        var mitaAnimators = GetMitaAnimators();
        mitas.Clear();

        // Patch each Mita over multiple frames
        for (int i = 0; i < mitaNames.Length; ++i)
        {
            string mitaName = mitaNames[i];
            string fullName = mitaName;

            if (mitaAnimators.Count <= i || mitaAnimators[i] == null)
                continue;
            mitas.Add(mitaAnimators[i]);
            PatchMita(modName, mitaAnimators[i], false, disactivation);
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


    public static void RestoreMeshBackup(
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
                var matches1 = Regex.Matches(line, @"[\""].+?[\""]|[^ ]+");
                var parts1 = matches1.Cast<Match>().Select(m => m.Value).ToArray();

                for (int i = 0; i < parts1.Length; i++)
                {
                    parts1[i] = parts1[i].Trim('\"');
                }
                if (parts1.Length > 2)
                {
                    switch (parts1[0])
                    {
                        case "create_static_appendix":
                            if (!staticRenderers.ContainsKey(parts1[2] + "_backup"))
                            {
                                staticAppendix.Add(parts1[2]);
                            }
                            else
                            {
                                replacedMeshes.Add(parts1[2]);
                            }
                            break;
                        case "create_skinned_appendix":
                            if (!skinnedRenderers.ContainsKey(parts1[2] + "_backup"))
                            {
                                skinnedAppendix.Add(parts1[2]);
                            }
                            else
                            {
                                replacedMeshes.Add(parts1[2]);
                            }
                            break;
                        case "resize_mesh":
                        case "move_mesh":
                        case "rotate_mesh":
                        case "shader_params":
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
        }

        UnityEngine.Debug.Log($"[INFO] Mod '{modName}' deactivated successfully.");
    }

    // Global dictionary to track applied commands per object
    public static Dictionary<GameObject, HashSet<string>> globalAppliedCommands = new();

    public static System.Collections.IEnumerator PatchMitaCoroutine(string modName, GameObject mita, bool recursive = false, bool disactivation = false, float maxFrameTime = 1f / 120f)
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
            // rename Mita True shorthair in the last chapter where she expose her skin
            if (renderer.transform.parent.name == "HairsManeken" && renderer.name == "Hair")
            {
                renderers["HairTrue"] = renderer;
            }
            else
            {
                renderers[renderer.name.Trim()] = renderer;
            }

        foreach (var renderer in staticRenderersList)
            staticRenderers[renderer.name.Trim()] = renderer;

        foreach (var command in ConsoleCommandHandler.assetCommands)
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
                    yield return Commands.ApplyReplaceMeshCommandCoroutine(command, mita, renderers, staticRenderers, null, maxFrameTime);
                    break;
                case "resize_mesh":
                    Commands.ApplyResizeMeshCommand(command, mita, renderers, staticRenderers);
                    break;
                case "move_mesh":
                    Commands.ApplyMoveMeshCommand(command, mita, renderers, staticRenderers);
                    break;
                case "rotate_mesh":
                    Commands.ApplyRotateMeshCommand(command, mita, renderers, staticRenderers);
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
            if ((Time.realtimeSinceStartup - frameStartTime) > maxFrameTime)
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

    public static void PatchMita(string modName, GameObject mita, bool recursive = false, bool disactivation = false)
    {
        var stopwatch = Stopwatch.StartNew();
        float frameStartTime = Time.realtimeSinceStartup;

        if (!globalAppliedCommands.ContainsKey(mita))
        {
            globalAppliedCommands[mita] = new HashSet<string>();
        }

        if (mita.name == "MitaTrue(Clone)" && !recursive)
        {
            PatchMita(modName, mita.transform.Find("MitaUsual").gameObject, true, disactivation);
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

        if (SceneHandler.currentSceneName == "SceneMenu")
        {
            CreateMeshBackup(renderers);
            if (disactivation)
            {
                RestoreMeshBackup(modName, renderers, staticRenderers);
            }
        }

        foreach (var command in ConsoleCommandHandler.assetCommands)
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
                    Commands.ApplyReplaceMeshCommand(command, mita, renderers, staticRenderers);
                    break;
                case "resize_mesh":
                    Commands.ApplyResizeMeshCommand(command, mita, renderers, staticRenderers);
                    break;
                case "move_mesh":
                    Commands.ApplyMoveMeshCommand(command, mita, renderers, staticRenderers);
                    break;
                case "rotate_mesh":
                    Commands.ApplyRotateMeshCommand(command, mita, renderers, staticRenderers);
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

        foreach (var command in ConsoleCommandHandler.assetCommands)
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
                        Commands.ApplyReplaceMeshCommand(command, player, renderers, staticRenderers, "Player");
                        break;
                    case "resize_mesh":
                        Commands.ApplyResizeMeshCommand(command, player, renderers, staticRenderers);
                        break;
                    case "move_mesh":
                        Commands.ApplyMoveMeshCommand(command, player, renderers, staticRenderers);
                        break;
                    case "rotate_mesh":
                        Commands.ApplyRotateMeshCommand(command, player, renderers, staticRenderers);
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
                    case "shader_params":
                        Commands.ApplyShaderParamsCommand(command, player, renderers, staticRenderers);
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

    public static System.Collections.IEnumerator FindPlayerCoroutine()
    {
        var animators = Reflection.FindObjectsOfType<Animator>(true);
        foreach (var animator in animators)
        {
            if (animator.name.Contains("Person") || animator.name.Contains("Player"))
            {
                GameObject personObject = animator.gameObject;
                UnityEngine.Debug.Log($"[INFO] Found 'Person' object with Animator: {(personObject == null ? "null" : personObject.name)}");
                if (personObject != null)
                {
                    yield return PatchPlayerCoroutine(personObject, 1 / targetFrameRate);
                }
            }
        }
    }

    public static System.Collections.IEnumerator PatchPlayerCoroutine(GameObject player, float maxFrameTime = 1f / 120f)
    {
        var stopwatch = Stopwatch.StartNew();
        float frameStartTime = Time.realtimeSinceStartup;

        if (player == null)
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Player object is null.");
            yield break;
        }

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

        foreach (var command in ConsoleCommandHandler.assetCommands)
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
                    Commands.ApplyReplaceMeshCommand(command, player, renderers, staticRenderers, "Player");
                    break;
                case "resize_mesh":
                    Commands.ApplyResizeMeshCommand(command, player, renderers, staticRenderers);
                    break;
                case "move_mesh":
                    Commands.ApplyMoveMeshCommand(command, player, renderers, staticRenderers);
                    break;
                case "rotate_mesh":
                    Commands.ApplyRotateMeshCommand(command, player, renderers, staticRenderers);
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
                case "shader_params":
                    Commands.ApplyShaderParamsCommand(command, player, renderers, staticRenderers);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"[WARNING] Unknown command: {command.name}");
                    break;

            }
            // Mark command as applied in the global dictionary
            Plugin.globalAppliedCommands[player].Add(commandKey);

            if ((Time.realtimeSinceStartup - frameStartTime) > maxFrameTime)
            {
                stopwatch.Stop(); // Pause the stopwatch
                yield return null; // Yield control back to Unity
                frameStartTime = Time.realtimeSinceStartup; // Reset the frame timer
                stopwatch.Start(); // Resume the stopwatch
            }

            // Wait for the next frame to avoid blocking the main thread
            yield return null;
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"[INFO] Patched '{player.name}' in {stopwatch.ElapsedMilliseconds}ms.");
    }

    VideoPlayer currentVideoPlayer = null;
    Action onCurrentVideoEnded = null;
    public static Image logo = null;

    void Update()
    {

        if (ConsoleMain.liteVersion)
        {
            ConsoleMain.liteVersion = false;
        }

        if (SceneHandler.currentSceneName != SceneManager.GetActiveScene().name)
        {
            if (SceneHandler.currentSceneName == "SceneMenu")
                SceneHandler.synch = false;
            SceneHandler.currentSceneName = SceneManager.GetActiveScene().name;
            SceneHandler.OnSceneChanged();
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
        {
            UtilityNamespace.LateCallUtility.Handler.StartCoroutine(AssetLoader.LoadAssetsForPatchCoroutine());
            globalAppliedCommands.Clear();
            if (SceneHandler.synch)
            {
                FindMita();
                FindPlayer();
            }
            else
            {
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(FindMitaCoroutine());
                UtilityNamespace.LateCallUtility.Handler.StartCoroutine(FindPlayerCoroutine());
            }
        }



        if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
        {
            GreenScreenHandler.ToggleGreenScreen();
        }

        GreenScreenHandler.HandleGreenScreenCameraMovement();

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

        if (SceneHandler.currentSceneName == "SceneMenu")
        {
            SceneHandler.HandleSceneMenu();
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
