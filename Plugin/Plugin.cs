using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
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

    public static GameObject[] mitas = new GameObject[70];

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


    // Global dictionary to track applied commands per object
    public static Dictionary<GameObject, HashSet<string>> globalAppliedCommands = new();

    public static void PatchMita(GameObject mita, bool recursive = false)
    {
        var stopwatch = Stopwatch.StartNew();

        // Ensure the object is tracked in the global dictionary
        if (!globalAppliedCommands.ContainsKey(mita))
        {
            globalAppliedCommands[mita] = new HashSet<string>();
        }

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

        foreach (var command in assetCommands)
        {
            if (command.args.Length == 0 || command.args[0] != "Mita")
                continue;

            string commandKey = $"{command.name} {string.Join(" ", command.args)}";

            // Check the globalAppliedCommands dictionary to skip already applied commands
            if (globalAppliedCommands[mita].Contains(commandKey))
            {
                UnityEngine.Debug.Log($"[INFO] Skipping already applied command: {commandKey} on '{mita.name}'");
                continue;
            }

            try
            {
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
                    default:
                        UnityEngine.Debug.LogWarning($"[WARNING] Unknown command: {command.name}");
                        break;
                }

                // Mark command as applied in the global dictionary
                globalAppliedCommands[mita].Add(commandKey);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ERROR] Error processing command: {commandKey} on '{mita.name}'\n{e}");
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
            // this is "Player"
            if (animator.name == "Person")
            {
                GameObject personObject = animator.gameObject;
                UnityEngine.Debug.Log($"[INFO] Found 'Person' object with Animator: {personObject.name}");
                PatchPlayer(personObject);
                return;
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
            renderers[player.name + renderer.name.Trim()] = renderer;

        var meshRenderers = player.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
            staticRenderers[player.name + renderer.name.Trim()] = renderer;

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
                        Commands.ApplyReplaceMeshCommand(command, player, renderers, staticRenderers);
                        break;
                    case "create_skinned_appendix":
                        Commands.ApplyCreateSkinnedAppendixCommand(command, player, renderers);
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
        }
    }
    void OnSceneChanged()
    {
        try
        {
            UnityEngine.Debug.Log($"[INFO] Scene changed to: {currentSceneName}.");
            LoadAssetsForPatch();
            globalAppliedCommands.Clear();
            FindMita();
            FindPlayer();
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
