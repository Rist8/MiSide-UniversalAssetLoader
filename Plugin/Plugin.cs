using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using Coffee.UIEffects;
using Colorful;
using Dummiesman;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LibCpp2IL;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using static MagicaReductionMesh.MeshData;
using System.Diagnostics;
using System.Collections.Concurrent;

public class Plugin : MonoBehaviour
{
    public static string? currentSceneName;
    public static bool startup = true;

    private void Start()
    {
        ReadAssetsConfig();
        LoadAssetsForPatch();
        ConsoleMain.active = true;
        ConsoleMain.eventEnter = new UnityEvent();
        ConsoleMain.eventEnter.AddListener((UnityAction)(() => { ConsoleEnter(ConsoleMain.codeEnter); }));
    }
    static GameObject greenScreenCameraObject = null;
    static Camera greenScreenCamera = null;
    public static Dictionary<string, bool> Active = new Dictionary<string, bool>();
    private static float dx = 0.0f, dy = 0.0f, dz = 0.0f, rdx = 0.0f, rdy = 0.0f;
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

        UnityEngine.Debug.Log("Console in: " + s);

        assetCommands.RemoveAll(command =>
            command.name == parts[0] && command.args.SequenceEqual(parts.Skip(1)));

        if (!s.StartsWith("-"))
        {
            assetCommands.Add((parts[0], parts.Skip(1).ToArray()));
        }

        FindMita();

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

    private static void HandleAddonConfig(string s)
    {
        string filePath = Path.Combine(PluginInfo.AssetsFolder, "addons_config.txt");

        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("*"))
                {
                    string command = line.Substring(1).ToLower();
                    if (s == command)
                    {
                        ClothesMenuPatcher.LogOnClick(command);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    private static Dictionary<string, Assimp.Mesh[]>? loadedModels;
    private static Dictionary<string, Texture2D>? loadedTextures;
    private static Dictionary<string, AudioClip>? loadedAudio;
    public static List<(string name, string[] args)> assetCommands;

    private static GameObject[] mitas = new GameObject[70];

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

    void LoadAssetsForPatch()
    {
        if (loadedModels != null) return;

        loadedModels = new Dictionary<string, Assimp.Mesh[]>();
        loadedTextures = new Dictionary<string, Texture2D>();
        loadedAudio = new Dictionary<string, AudioClip>();

        PluginInfo.Instance.Logger.LogInfo($"Processor count : {Environment.ProcessorCount}");
        var stopwatch = Stopwatch.StartNew();

        // Load audio files
        foreach (var file in AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "ogg"))
        {
            var audioFile = AssetLoader.LoadAudio(file);
            string filename = Path.GetRelativePath(PluginInfo.AssetsFolder, file);
            filename = Path.ChangeExtension(filename, null);
            if (!loadedAudio.ContainsKey(filename))
            {
                loadedAudio.Add(filename, audioFile);
                PluginInfo.Instance.Logger.LogInfo($"Loaded audio from file: '{filename}'");
            }
        }
        stopwatch.Stop();
        PluginInfo.Instance.Logger.LogInfo($"Loaded all audio in {stopwatch.ElapsedMilliseconds}ms");

        // Load model files
        stopwatch.Restart();
        var loadedModelsLocal = new ConcurrentDictionary<string, Assimp.Mesh[]>();

        Parallel.ForEach(AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "fbx"),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount - 1
            }
        , file =>
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
        stopwatch.Stop();
        PluginInfo.Instance.Logger.LogInfo($"Loaded all meshes in {stopwatch.ElapsedMilliseconds}ms");

        // Load texture files
        stopwatch.Restart();
        foreach (var file in AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "png", "jpg", "jpeg"))
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
        }
        stopwatch.Stop();
        PluginInfo.Instance.Logger.LogInfo($"Loaded all textures in {stopwatch.ElapsedMilliseconds}ms");
    }
    public static string[] mitaNames = { "Usual", "MitaTrue", "ShortHairs", "Kind", "Cap",
    "Little", "Maneken", "Black", "Dreamer", "Mila",
    "Creepy", "Core", "MitaGame", "MitaPerson Mita", "Dream",
    "Future", "Broke", "Glasses", "MitaPerson Future", "CreepyMita",
    "Old", "MitaPerson Old", "Mita", "Mita", "Mita",
    "Mita", "Mita", "Mita", "Mita", "Mita",
    "Mita", "Mita", "Mita", "Mita", "Mita"};

    public static void FindMita()
    {
        var animators = Reflection.FindObjectsOfType<Animator>(true);
        GameObject[] mitaAnimators = new GameObject[70];
        Array.Clear(mitaAnimators, 0, mitaAnimators.Length);

        foreach (var obj in animators)
        {
            var anim = obj.Cast<Animator>();
            var runtimeController = anim.runtimeAnimatorController;

            if (runtimeController != null)
            {
                //UnityEngine.Debug.Log($"[INFO] Animator Found: |{runtimeController.name}|");

                for (int i = 0; i < 35; ++i)
                {
                    string mitaName = mitaNames[i];
                    string cloneName = mitaName + "(Clone)";

                    if (runtimeController.name.Contains(mitaName))
                    {
                        if (mitaAnimators[i] != null)
                            continue;
                        mitaAnimators[i] = anim.gameObject;
                        break;
                    }
                    else if (runtimeController.name.Contains(cloneName))
                    {
                        if (mitaAnimators[i + 35] != null)
                            continue;
                        mitaAnimators[i + 35] = anim.gameObject;
                        break;
                    }
                }
            }
        }

        // Explicit assignment for specific cases
        mitaAnimators[13] = GameObject.Find("MitaPerson Mita");
        mitaAnimators[14] = GameObject.Find("Mita Dream");
        mitaAnimators[15] = GameObject.Find("Mita Future");
        mitaAnimators[18] = GameObject.Find("MitaPerson Future");
        mitaAnimators[19] = GameObject.Find("CreepyMita");
        mitaAnimators[21] = GameObject.Find("MitaPerson Old");
        mitaAnimators[48] = GameObject.Find("MitaPerson Mita(Clone)");
        mitaAnimators[49] = GameObject.Find("Mita Dream(Clone)");
        mitaAnimators[50] = GameObject.Find("Mita Future(Clone)");
        mitaAnimators[53] = GameObject.Find("MitaPerson Future(Clone)");
        mitaAnimators[54] = GameObject.Find("CreepyMita(Clone)");
        mitaAnimators[56] = GameObject.Find("MitaPerson Old(Clone)");

        for (int i = 0; i < 70; ++i)
        {
            string mitaName = mitaNames[i % 35];
            string suffix = (i >= 35) ? "(Clone)" : string.Empty;
            string fullName = mitaName + suffix;

            if (mitaAnimators[i] == null)
            {
                //UnityEngine.Debug.Log($"[WARNING] No animators found for {fullName} to patch.");
                mitas[i] = null;
            }
            else
            {
                mitas[i] = mitaAnimators[i];
                //UnityEngine.Debug.Log($"[INFO] Starting to patch Mita: {fullName}");
                PatchMita(mitas[i]);
                //UnityEngine.Debug.Log($"[INFO] Finished patching Mita: {fullName}.");
            }
        }
    }

    private static bool ShouldSkip(int start, string[] args, string mitaName)
    {
        bool skip = false;
        for (int i = start; i < args.Length && args[i] != "all"; i++)
        {
            string argsName = args[i];
            if (args[i].StartsWith("!"))
            {
                if (args[i] == "!Mita") argsName = "!MitaPerson Mita";
                if (mitaName.Contains(string.Join("", argsName.Skip(1))))
                {
                    skip = true;
                    break;
                }
                continue;
            }
            else
            {
                if (args[i] == "Mita") argsName = "MitaPerson Mita";
                if (!mitaName.Contains(argsName))
                {
                    skip = true;
                    continue;
                }
                skip = false;
            }
            break;
        }
        return skip;
    }

    public static void PatchMita(GameObject mita, bool recursive = false)
    {
        var stopwatch = Stopwatch.StartNew();

        if (mita.name == "MitaTrue(Clone)" && !recursive)
        {
            PatchMita(mita.transform.Find("MitaUsual").gameObject, true);
            mita = mita.transform.Find("MitaTrue").gameObject;
        }
        var renderersList = Reflection.GetComponentsInChildren<SkinnedMeshRenderer>(mita, true);
        var staticRenderersList = Reflection.GetComponentsInChildren<MeshRenderer>(mita, true);
        var renderers = new Dictionary<string, SkinnedMeshRenderer>();
        var staticRenderers = new Dictionary<string, MeshRenderer>();
        foreach (var renderer in renderersList)
            renderers[mita.name + renderer.name.Trim()] = renderer;
        foreach (var renderer in staticRenderersList)
            staticRenderers[mita.name + renderer.name.Trim()] = renderer;

        for (int c = 0; c < assetCommands.Count; ++c)
        {
            var command = assetCommands[c];
            if (command.args.Length == 0 || command.args[0] != "Mita")
                continue;
            try
            {
                UnityEngine.Debug.Log($"[INFO] Mita's name: {mita.name} |");
                //UnityEngine.Debug.Log($"[INFO] Static renderers already present: {staticRenderers.Keys.ToStringEnumerable()}.");

                bool skip = false;
                switch (command.name)
                {
                    case "remove":
                        if (ShouldSkip(2, command.args, mita.name)) continue;
                        if (renderers.ContainsKey(mita.name + command.args[1]))
                        {
                            renderers[mita.name + command.args[1]].gameObject.SetActive(false);
                            UnityEngine.Debug.Log($"[INFO] Removed skinned appendix: {mita.name} {command.args[1]}.");
                        }
                        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                        {
                            staticRenderers[mita.name + command.args[1]].gameObject.SetActive(false);
                            UnityEngine.Debug.Log($"[INFO] Removed static appendix: {mita.name} {command.args[1]}.");
                        }
                        else
                            UnityEngine.Debug.Log($"[ERROR] {mita.name} {command.args[1]} not found.");
                        break;
                    case "recover":
                        if (ShouldSkip(2, command.args, mita.name)) continue;
                        if (renderers.ContainsKey(mita.name + command.args[1]))
                        {
                            renderers[mita.name + command.args[1]].gameObject.SetActive(true);
                            UnityEngine.Debug.Log($"[INFO] Recovered skinned appendix: {mita.name} {command.args[1]}.");
                        }
                        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                        {
                            staticRenderers[mita.name + command.args[1]].gameObject.SetActive(true);
                            UnityEngine.Debug.Log($"[INFO] Recovered static appendix: {mita.name} {command.args[1]}.");
                        }
                        else
                            UnityEngine.Debug.Log($"[ERROR] {mita.name} {command.args[1]} not found.");
                        break;
                    case "replace_tex":
                        if (ShouldSkip(3, command.args, mita.name)) continue;
                        command.args[2] = command.args[2].Replace(@"\\", @"\");
                        command.args[2] = string.Join("", command.args[2].Replace(@".\", string.Empty).SkipWhile(c => c == '.' || c == '\\'));

                        if (renderers.ContainsKey(mita.name + command.args[1]))
                        {
                            renderers[mita.name + command.args[1]].material.mainTexture = loadedTextures[command.args[2]];
                            UnityEngine.Debug.Log($"[INFO] Replaced texture of skinned: {mita.name} {command.args[1]}.");
                        }
                        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                        {
                            staticRenderers[mita.name + command.args[1]].material.mainTexture = loadedTextures[command.args[2]];
                            UnityEngine.Debug.Log($"[INFO] Replaced texture of static: {mita.name} {command.args[1]}.");
                        }
                        else
                            UnityEngine.Debug.Log($"[ERROR] {mita.name} {command.args[1]} not found.");
                        break;
                    case "replace_mesh":
                        if (ShouldSkip(4, command.args, mita.name)) continue;
                        command.args[2] = command.args[2].Replace(@"\\", @"\");
                        command.args[2] = string.Join("", command.args[2].Replace(@".\", string.Empty).SkipWhile(c => c == '.' || c == '\\'));

                        Assimp.Mesh meshData = null;
                        if (command.args[2] != "null")
                        {
                            meshData = loadedModels[command.args[2]].First(mesh =>
                                mesh.Name == (command.args.Length >= 4 ? command.args[3] : Path.GetFileNameWithoutExtension(command.args[2])));

                        }
                        if (renderers.ContainsKey(mita.name + command.args[1]))
                        {
                            if (renderers[mita.name + command.args[1]] is SkinnedMeshRenderer sk)
                            {
                                if (command.args[2] == "null" && command.args[3] == "null")
                                    sk.sharedMesh = new Mesh();
                                else
                                    sk.sharedMesh = AssetLoader.BuildMesh(meshData, new AssetLoader.ArmatureData(sk));
                                UnityEngine.Debug.Log($"[INFO] Replaced mesh of skinned: {mita.name} {command.args[1]}.");
                            }
                            else
                            {
                                if (command.args[2] == "null" && command.args[3] == "null")
                                    renderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = new Mesh();
                                else
                                    renderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData);
                                UnityEngine.Debug.Log($"[INFO] Replaced mesh of skinned (static method): {mita.name} {command.args[1]}.");
                            }
                        }
                        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                        {
                            if (command.args[2] == "null" && command.args[3] == "null")
                                staticRenderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = new Mesh();
                            else
                                staticRenderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData);
                            UnityEngine.Debug.Log($"[INFO] Replaced mesh of static: {mita.name} {command.args[1]}.");
                        }
                        else
                            UnityEngine.Debug.Log($"[ERROR] {mita.name} {command.args[1]} not found.");
                        break;
                    case "create_skinned_appendix":
                        if (ShouldSkip(3, command.args, mita.name)) continue;
                        var parent = renderers[mita.name + command.args[2]];
                        if (renderers.ContainsKey(mita.name + command.args[1]))
                        {
                            if (renderers[mita.name + command.args[1]].gameObject.active == false)
                                renderers[mita.name + command.args[1]].gameObject.SetActive(true);
                            continue;
                        }
                        SkinnedMeshRenderer objSkinned = UnityEngine.Object.Instantiate(
                            parent,
                            parent.transform.position,
                            parent.transform.rotation,
                            parent.transform.parent).Cast<SkinnedMeshRenderer>();
                        objSkinned.name = command.args[1];
                        objSkinned.material = new Material(parent.material);
                        objSkinned.transform.localEulerAngles = new Vector3(-90f, 0, 0);
                        objSkinned.gameObject.SetActive(true);
                        renderers[mita.name + command.args[1]] = objSkinned;
                        UnityEngine.Debug.Log($"[INFO] Added skinned appendix: {objSkinned.name}.");
                        break;
                    case "create_static_appendix":
                        if (ShouldSkip(3, command.args, mita.name)) continue;
                        if (staticRenderers.ContainsKey(mita.name + command.args[1]) && mita.name != "MitaPerson Mita")
                        {
                            if (staticRenderers[mita.name + command.args[1]].gameObject.active == false)
                                staticRenderers[mita.name + command.args[1]].gameObject.SetActive(true);
                            continue;
                        }
                        else if (mita.name == "MitaPerson Mita")
                        {
                            if (RecursiveFindChild(mita.transform.Find("Armature"), command.args[1]))
                            {
                                if (RecursiveFindChild(mita.transform.Find("Armature"), command.args[1]).gameObject.active == false)
                                    RecursiveFindChild(mita.transform.Find("Armature"), command.args[1]).gameObject.SetActive(true);
                                continue;
                            }
                        }
                        MeshRenderer obj = new GameObject().AddComponent<MeshRenderer>();
                        obj.name = command.args[1];
                        if (mita.transform.Find("Attribute"))
                        {
                            obj.material = new Material(mita.transform.Find("Attribute").GetComponent<SkinnedMeshRenderer>().material);
                        }
                        else if (mita.transform.Find("Body"))
                        {
                            //obj.material = new Material(origMaterial);
                            obj.material = new Material(mita.transform.Find("Body").GetComponent<SkinnedMeshRenderer>().material);
                        }
                        else if (mita.transform.Find("Head"))
                        {
                            //obj.material = new Material(origMaterial);
                            obj.material = new Material(mita.transform.Find("Head").GetComponent<SkinnedMeshRenderer>().material);
                        }
                        else
                        {
                            MeshRenderer.Destroy(obj);
                            continue;
                        }

                        obj.gameObject.AddComponent<MeshFilter>();

                        if (mita.name == "NewVersionMita Head")
                        {
                            obj.transform.parent = RecursiveFindChild(mita.transform.Find("ArmatureHead"), command.args[2]);
                        }
                        else if (RecursiveFindChild(mita.transform.Find("Armature"), command.args[2]))
                        {
                            obj.transform.parent = RecursiveFindChild(mita.transform.Find("Armature"), command.args[2]);
                        }
                        else
                        {
                            MeshRenderer.Destroy(obj.gameObject);
                            continue;
                        }

                        if (obj.transform.parent == null)
                        {
                            MeshRenderer.Destroy(obj.gameObject);
                            continue;
                        }

                        obj.transform.localPosition = Vector3.zero;
                        obj.transform.localScale = Vector3.one;
                        obj.transform.localEulerAngles = new Vector3(-90f, 0, 0);
                        obj.gameObject.SetActive(true);
                        staticRenderers[mita.name + command.args[1]] = obj;
                        UnityEngine.Debug.Log($"[INFO] Added static appendix: {mita.name} {obj.name}.");
                        break;
                    case "set_scale":
                        if (ShouldSkip(5, command.args, mita.name)) continue;
                        Transform objRecrusive = RecursiveFindChild(mita.transform, command.args[1]);
                        if (objRecrusive)
                        {
                            objRecrusive.localScale = new Vector3(float.Parse(
                                command.args[2].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[3].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[4].Replace(',', '.'), CultureInfo.InvariantCulture)
                            );

                            UnityEngine.Debug.Log($"[INFO] Set scale of {mita.name} {command.args[1]}" +
                                $" to ({command.args[2]}, {command.args[3]}, {command.args[4]}).");
                        }
                        break;
                    case "move_position":
                        if (ShouldSkip(5, command.args, mita.name)) continue;
                        Transform objMoved = RecursiveFindChild(mita.transform, command.args[1]);
                        if (objMoved)
                        {
                            objMoved.localPosition += new Vector3(
                                float.Parse(command.args[2].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[3].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[4].Replace(',', '.'), CultureInfo.InvariantCulture));
                            UnityEngine.Debug.Log($"[INFO] Changed position of {mita.name} {command.args[1]} " +
                                $"by ({command.args[2]}, {command.args[3]}, {command.args[4]}).");
                        }
                        break;
                    case "set_rotation":
                        if (ShouldSkip(6, command.args, mita.name)) continue;

                        Transform objRotate = RecursiveFindChild(mita.transform, command.args[1]);
                        if (objRotate)
                        {
                            objRotate.localRotation = new Quaternion(
                                float.Parse(command.args[2].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[3].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[4].Replace(',', '.'), CultureInfo.InvariantCulture),
                                float.Parse(command.args[5].Replace(',', '.'), CultureInfo.InvariantCulture)
                            );

                            UnityEngine.Debug.Log($"[INFO] Changed position of {mita.name} {command.args[1]} " +
                                $"by ({command.args[2]}, {command.args[3]}, {command.args[4]}, {command.args[5]}).");
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ERROR] Error while processing command: {command.name} {string.Join(' ', command.args)}\n{e}");
            }
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"[INFO] Patched Mita in {stopwatch.ElapsedMilliseconds}ms.");
    }

    static Transform RecursiveFindChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName) return child;
            else
            {
                Transform found = RecursiveFindChild(child, childName);
                if (found != null) return found;
            }
        }
        return null;
    }

    VideoPlayer currentVideoPlayer = null;
    Action onCurrentVideoEnded = null;
    Image logo = null;

    void PatchMenuScene()
    {
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
            LoadAssetsForPatch();
            FindMita();
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
        }
    }
    void OnSceneChanged()
    {
        try
        {
            UnityEngine.Debug.Log($"[INFO] Scene changed to: {currentSceneName}.");
            LoadAssetsForPatch();
            FindMita();
            if (currentSceneName == "SceneMenu")
                PatchMenuScene();
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
