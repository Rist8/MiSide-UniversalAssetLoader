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

public class Plugin : MonoBehaviour{
	public static string? currentSceneName;

	private void Start(){
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

        Debug.Log("Console in: " + s);

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

    private static GameObject[] mitas = new GameObject[46];

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

    void LoadAssetsForPatch()
    {
        if (loadedModels != null) return;

        loadedModels = new Dictionary<string, Assimp.Mesh[]>();
        loadedTextures = new Dictionary<string, Texture2D>();
        loadedAudio = new Dictionary<string, AudioClip>();

        // Load audio files
        foreach (var file in AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "ogg"))
        {
            var audioFile = AssetLoader.LoadAudio(file);
            var filename = Path.GetFileNameWithoutExtension(file);
            if (!loadedAudio.ContainsKey(filename))
            {
                loadedAudio.Add(filename, audioFile);
                PluginInfo.Instance.Logger.LogInfo($"Loaded audio from file: '{filename}'");
            }
        }

        // Load mesh files
        foreach (var file in AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "fbx"))
        {
            var meshes = AssetLoader.LoadFBX(file);
            var filename = Path.GetFileNameWithoutExtension(file);
            if (!loadedModels.ContainsKey(filename))
            {
                loadedModels.Add(filename, meshes);
                PluginInfo.Instance.Logger.LogInfo($"Loaded meshes from file: '{filename}', {meshes.Length} meshes");
            }
        }

        // Load texture files
        foreach (var file in AssetLoader.GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "png", "jpg", "jpeg"))
        {
            var texture = AssetLoader.LoadTexture(file);
            if (texture != null)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if (!loadedTextures.ContainsKey(filename))
                {
                    loadedTextures.Add(filename, texture);
                    PluginInfo.Instance.Logger.LogInfo($"Loaded texture from file: '{filename}'");
                }
            }
        }
    }
    public static string[] mitaNames = { "Usual", "MitaTrue", "ShortHairs", "Kind", "Cap",
    "Little", "Maneken", "Black", "Dreamer", "Mila",
    "Creepy", "Core", "MitaGame", "MitaPerson Mita", "Dream", "Future", "Broken", "Mita", "Mita", "Mita", "Mita", "Mita", "Mita" };

    public static void FindMita()
    {
        var animators = Reflection.FindObjectsOfType<Animator>(true);
        GameObject[] mitaAnimators = new GameObject[46];
        Array.Clear(mitaAnimators, 0, mitaAnimators.Length);

        foreach (var obj in animators)
        {
            var anim = obj.Cast<Animator>();
            var runtimeController = anim.runtimeAnimatorController;

            if (runtimeController != null)
            {
                Debug.Log($"Found animator |{runtimeController.name}|");

                for (int i = 0; i < 23; ++i)
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
                        if (mitaAnimators[i + 23] != null)
                            continue;
                        mitaAnimators[i + 23] = anim.gameObject;
                        break;
                    }
                }
            }
        }

        // Explicit assignment for specific cases
        mitaAnimators[13] = GameObject.Find("MitaPerson Mita");
        mitaAnimators[14] = GameObject.Find("Mita Dream");
        mitaAnimators[15] = GameObject.Find("Mita Future");
        mitaAnimators[17] = GameObject.Find("MitaPerson Future");
        mitaAnimators[36] = GameObject.Find("MitaPerson Mita(Clone)");
        mitaAnimators[37] = GameObject.Find("Mita Dream(Clone)");
        mitaAnimators[39] = GameObject.Find("Mita Future(Clone)");
        mitaAnimators[40] = GameObject.Find("MitaPerson Future(Clone)");

        for (int i = 0; i < 46; ++i)
        {
            string mitaName = mitaNames[i % 23];
            string suffix = (i >= 23) ? "(Clone)" : string.Empty;
            string fullName = mitaName + suffix;

            if (mitaAnimators[i] == null)
            {
                Debug.Log($"Found no animators for {fullName} to patch");
                mitas[i] = null;
            }
            else
            {
                mitas[i] = mitaAnimators[i];
                Debug.Log($"Patching Mita {fullName}");
                PatchMita(mitas[i]);
                Debug.Log($"Patching Mita {fullName} completed");
            }
        }
    }


    public static void PatchMita(GameObject mita, bool recursive = false){
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

        foreach (var command in assetCommands){
            if (command.args.Length == 0 || command.args[0] != "Mita")
                continue;
            try
            {
                Debug.Log("Mita's name=" + mita.name + '|');
                Debug.Log("She already has: " + staticRenderers.Keys.ToStringEnumerable());
                if (command.name == "remove")
                {
                    if (command.args.Length > 2)
                    {
                        if (command.args[2].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[2].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[2].Skip(1)))) continue;
                        }
                    }
                    if (renderers.ContainsKey(mita.name + command.args[1]))
                    {
                        renderers[mita.name + command.args[1]].gameObject.SetActive(false);
                        Debug.Log("Removed skinned appendix " + mita.name + command.args[1]);
                    }
                    else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                    {
                        staticRenderers[mita.name + command.args[1]].gameObject.SetActive(false);
                        Debug.Log("Removed static appendix " + mita.name + command.args[1]);
                    }
                    else
                        Debug.Log(mita.name + command.args[1] + " not found");
                }
                else if (command.name == "recover")
                {
                    if(command.args.Length > 2)
                    {
                        if (command.args[2].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[2].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[2].Skip(1)))) continue;
                        }
                    }
                    if (renderers.ContainsKey(mita.name + command.args[1]))
                    {
                        renderers[mita.name + command.args[1]].gameObject.SetActive(true);
                        Debug.Log("Recovered skinned appendix " + mita.name + command.args[1]);
                    }
                    else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                    {
                        staticRenderers[mita.name + command.args[1]].gameObject.SetActive(true);
                        Debug.Log("Recovered static appendix " + mita.name + command.args[1]);
                    }
                    else
                        Debug.Log(mita.name + command.args[1] + " not found");
                }
                else if (command.name == "replace_tex")
                {
                    if (command.args.Length > 3 && command.args[3] != "all")
                    {
                        if (command.args[3].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[3].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[3].Skip(1)))) continue;
                        }
                    }
                    if (renderers.ContainsKey(mita.name + command.args[1]))
                    {
                        renderers[mita.name + command.args[1]].material.mainTexture = loadedTextures[command.args[2]];
                        Debug.Log("Replaced texture of skinned " + mita.name + command.args[1]);
                    }
                    else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                    {
                        staticRenderers[mita.name + command.args[1]].material.mainTexture = loadedTextures[command.args[2]];
                        Debug.Log("Replaced texture of static " + mita.name + command.args[1]);
                    }
                    else
                        Debug.Log(mita.name + command.args[1] + " not found");
                }
                else if (command.name == "replace_mesh")
                {
                    if (command.args.Length > 4 && command.args[4] != "all")
                    {
                        if (command.args[4].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[4].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[4].Skip(1)))) continue;
                        }
                    }
                    Assimp.Mesh meshData = null;
                    if (command.args[2] != "null")
                    {
                        meshData = loadedModels[command.args[2]].First(mesh =>
                            mesh.Name == (command.args.Length >= 4 ? command.args[3] : command.args[2]));

                    }
                    if (renderers.ContainsKey(mita.name + command.args[1]))
                    {
                        if (renderers[mita.name + command.args[1]] is SkinnedMeshRenderer sk)
                        {
                            if (command.args[2] == "null" && command.args[3] == "null")
                                sk.sharedMesh = new Mesh();
                            else
                                sk.sharedMesh = AssetLoader.BuildMesh(meshData, new AssetLoader.ArmatureData(sk));
                            Debug.Log("Replaced mesh of skinned " + mita.name + command.args[1]);
                        }
                        else
                        {
                            if (command.args[2] == "null" && command.args[3] == "null")
                                renderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = new Mesh();
                            else
                                renderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData);
                            Debug.Log("Replaced mesh of skinned(static method) " + mita.name + command.args[1]);
                        }
                    }
                    else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
                    {
                        if (command.args[2] == "null" && command.args[3] == "null")
                            staticRenderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = new Mesh();
                        else
                            staticRenderers[mita.name + command.args[1]].GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData);
                        Debug.Log("Replaced mesh of static " + mita.name + command.args[1]);
                    }
                    else
                        Debug.Log(mita.name + command.args[1] + " not found");
                }
                else if (command.name == "create_skinned_appendix")
                {
                    if (command.args.Length > 3 && command.args[3] != "all")
                    {
                        if (command.args[3].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[3].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[3].Skip(1)))) continue;
                        }
                    }
                    var parent = renderers[mita.name + command.args[2]];
                    if (renderers.ContainsKey(mita.name + command.args[1]))
                    {
                        if (renderers[mita.name + command.args[1]].gameObject.active == false)
                            renderers[mita.name + command.args[1]].gameObject.SetActive(true);
                        continue;
                    }
                    SkinnedMeshRenderer obj = UnityEngine.Object.Instantiate(
                        parent,
                        parent.transform.position,
                        parent.transform.rotation,
                        parent.transform.parent).Cast<SkinnedMeshRenderer>();
                    obj.name = command.args[1];
                    obj.material = new Material(parent.material);
                    obj.transform.localEulerAngles = new Vector3(-90f, 0, 0);
                    obj.gameObject.SetActive(true);
                    renderers[mita.name + command.args[1]] = obj;
                    Debug.Log("Added skinned appendix " + obj.name);
                }
                else if (command.name == "create_static_appendix")
                {
                    if (command.args.Length > 3 && command.args[3] != "all")
                    {
                        if (command.args[3].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[3].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[3].Skip(1)))) continue;
                        }
                    }
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
                    Debug.Log("Added static appendix " + mita.name + obj.name);
                }
                else if (command.name == "set_scale")
                {
                    if (command.args.Length > 5 && command.args[5] != "all")
                    {
                        if (command.args[5].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[5].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[5].Skip(1)))) continue;
                        }
                    }
                    Transform obj = RecursiveFindChild(mita.transform, command.args[1]);
                    if (obj)
                    {
                        obj.localScale = new Vector3(float.Parse(
                            command.args[2].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(command.args[3].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(command.args[4].Replace(',', '.'), CultureInfo.InvariantCulture)
                        );

                        Debug.Log("Set scale of " + mita.name + ' '
                            + command.args[1] + " to " + command.args[2]
                            + ", " + command.args[3] + ", " + command.args[4]);
                    }
                }
                else if (command.name == "change_position")
                {
                    if (command.args.Length > 5 && command.args[5] != "all")
                    {
                        if (command.args[5].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[5].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[5].Skip(1)))) continue;
                        }
                    }
                    Transform obj = RecursiveFindChild(mita.transform, command.args[1]);
                    if (obj)
                    {
                        obj.localPosition += new Vector3(float.Parse(command.args[2]),
                            float.Parse(command.args[3].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(command.args[4].Replace(',', '.'), CultureInfo.InvariantCulture));
                        Debug.Log("Changed the position of " + mita.name + ' '
                            + command.args[1] + " by " + command.args[2]
                            + ", " + command.args[3] + ", " + command.args[4]);
                    }
                }
                else if (command.name == "set_rotation")
                {
                    if (command.args.Length > 6 && command.args[6] != "all")
                    {
                        if (command.args[6].StartsWith("!"))
                        {
                            if (mita.name.Contains(string.Join("", command.args[6].Skip(1)))) continue;
                        }
                        else
                        {
                            if (!mita.name.Contains(string.Join("", command.args[6].Skip(1)))) continue;
                        }
                    }
                    Transform obj = RecursiveFindChild(mita.transform, command.args[1]);
                    if (obj)
                    {
                        obj.localRotation = new Quaternion(
                            float.Parse(command.args[2].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(command.args[3].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(command.args[4].Replace(',', '.'), CultureInfo.InvariantCulture),
                            float.Parse(command.args[5].Replace(',', '.'), CultureInfo.InvariantCulture)
                        );

                        Debug.Log("Changed the position of " + mita.name + ' '
                            + command.args[1] + " by " + command.args[2]
                            + ", " + command.args[3] + ", " + command.args[4] 
                            + ", " + command.args[5]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while processing command " + command.name + " " + string.Join(' ', command.args) + "\n" + e.ToString());
            }
        }
    }

	static Transform RecursiveFindChild(Transform parent, string childName){
		for (int i = 0; i < parent.childCount; i++){
			var child = parent.GetChild(i);
			if(child.name == childName) return child;
			else {
				Transform found = RecursiveFindChild(child, childName);
				if (found != null) return found;
			}
		}
		return null;
	}

	VideoPlayer currentVideoPlayer = null;
	Action onCurrentVideoEnded = null;
	Image logo = null;

	void PatchMenuScene(){
		Debug.Log("Patching game scene");
		var command = assetCommands.FirstOrDefault<(string? name, string[]? args)>(item => item.name == "menu_logo", (null, null));
		if (command.name != null){
			var animators = Reflection.FindObjectsOfType<Animator>(true);
			GameObject gameName = null;
			foreach (var obj in animators) 
				if (obj.name == "NameGame"){ 
					gameName = obj.Cast<Animator>().gameObject; 
					Destroy(obj);
					break;
				}

			for (int i = 0; i < gameName.transform.childCount; i++){
				var tr = gameName.transform.GetChild(i);
				if (tr.name == "Background"){
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
		if (command.name != null){
			var musicSources = Reflection.FindObjectsOfType<AudioSource>(true);
			foreach (var source in musicSources)
				if (source.name == "Music"){
					source.clip = loadedAudio[command.args[0]];
					source.volume = 1;
					source.Play();
					break;
				}
		}

        ClothesMenuPatcher.Run();

		Debug.Log("Patching completed");
	}

    private static float baseMovementSpeed = 0.03f;
    private static float maxMovementSpeed = 0.1f;
    private static float mouseSensitivity = 0.7f;
    private static int unlocked = 0;

    void Update(){
        if (unlocked == 1)
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }
        else if(unlocked == 2)
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            unlocked = 0;
        }
        if (ConsoleMain.liteVersion)
        {
            ConsoleMain.liteVersion = false;
        }
        if (currentSceneName != SceneManager.GetActiveScene().name){
			currentSceneName = SceneManager.GetActiveScene().name;
            OnSceneChanged();
		}

        if (UnityEngine.Input.GetKeyDown(KeyCode.U))
        {
            if (unlocked == 1)
            {
                unlocked = 2;
            }
            else
            {
                unlocked = 1;
            }
        }

        if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
		{
            LoadAssetsForPatch();
			FindMita();
		}

        if (UnityEngine.Input.GetKeyDown(KeyCode.Slash))
        {
            if (Reflection.FindObjectsOfType<ConsoleCall>(true).Length > 0)
            {
                ConsoleCall camerafly = Reflection.FindObjectsOfType<ConsoleCall>(true)[0];
                if (camerafly)
                {
                    camerafly.CameraFly();
                }
            }
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
        {
            if (Time.timeScale != 0.0f)
                Time.timeScale = 0.0f;
            else
                Time.timeScale = 1.0f;
        }
        if (UnityEngine.Input.GetKeyDown(KeyCode.F10)){
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

        if (currentVideoPlayer != null){
			if ((ulong) currentVideoPlayer.frame + 5 > currentVideoPlayer.frameCount){
				Debug.Log("Video ended");
				Destroy(currentVideoPlayer.transform.parent.gameObject);
				currentVideoPlayer = null;
				onCurrentVideoEnded?.Invoke();
				onCurrentVideoEnded = null;
			}
		}
		if (currentSceneName == "SceneMenu"){
			if (logo != null)
				logo.color = Color.white;
		}
	}
	void OnSceneChanged(){
		try{
			Debug.Log("Scene changed to " + currentSceneName);
            LoadAssetsForPatch();
            FindMita();
			if (currentSceneName == "SceneMenu")
				PatchMenuScene();
        } catch (Exception e){
			Debug.Log(e.ToString());
			enabled = false;
		}
	}

	void PlayFullscreenVideo(Action onVideoEnd){
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
		Debug.Log("Video started");
	}
}
