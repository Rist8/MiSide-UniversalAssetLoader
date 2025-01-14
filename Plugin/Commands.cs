using System.Globalization;
using UnityEngine;
using UtilityNamespace;

public class Commands
{
    private static bool ShouldSkip(int start, (string name, string[] args) command, string mitaName)
    {
        int i = start;

        // Log only the reason for skipping or applying, keeping logs concise
        for (; i < command.args.Length && command.args[i] != "all"; i++)
        {
            string argsName = command.args[i];

            if (command.args[i].StartsWith("!"))
            {
                // Negative keyword check (e.g., "!Mita")
                if (command.args[i] == "!Mita")
                    argsName = "!MitaPerson Mita";

                if (mitaName.Contains(string.Join("", argsName.Skip(1))))
                {
                    UnityEngine.Debug.Log($"[INFO] Skipping command '{command.name}' on '{mitaName}' due to negative keyword '{argsName}'.");
                    return true;
                }

                continue;
            }
            else
            {
                // Positive keyword check
                if (command.args[i] == "Mita")
                    argsName = "MitaPerson Mita";

                if (!mitaName.Contains(argsName))
                {
                    UnityEngine.Debug.Log($"[INFO] Skipping command '{command.name}' on '{mitaName}' because keyword '{argsName}' was not found.");
                    return true;
                }

                break; // Positive match found; no need to check further
            }
        }

        if (i == command.args.Length)
        {
            UnityEngine.Debug.Log($"[INFO] Applying command '{command.name}' on '{mitaName}' as no exclusion keywords were matched.");
        }

        return false; // Do not skip
    }


    public static void ApplyRemoveCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(2, command, mita.name))
            return;

        if (renderers != null && renderers.ContainsKey(mita.name + command.args[1]))
        {
            renderers[mita.name + command.args[1]].gameObject.SetActive(false);
            UnityEngine.Debug.Log($"[INFO] Removed skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            staticRenderers[mita.name + command.args[1]].gameObject.SetActive(false);
            UnityEngine.Debug.Log($"[INFO] Removed static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}'.");
        }
    }

    public static void ApplyRecoverCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {

        if (ShouldSkip(2, command, mita.name))
            return;

        if (renderers != null && renderers.ContainsKey(mita.name + command.args[1]))
        {
            renderers[mita.name + command.args[1]].gameObject.SetActive(true);
            UnityEngine.Debug.Log($"[INFO] Recovered skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            staticRenderers[mita.name + command.args[1]].gameObject.SetActive(true);
            UnityEngine.Debug.Log($"[INFO] Recovered static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}'.");
        }
    }

    public static void ApplyReplaceTexCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(3, command, mita.name))
            return;

        string textureKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');

        if (renderers.ContainsKey(mita.name + command.args[1]))
        {
            Material material = renderers[mita.name + command.args[1]].material;
            material.mainTexture = Plugin.loadedTextures[command.args[2]];
            material.SetFloat("_EnableTextureTransparent", 1.0f);
            UnityEngine.Debug.Log($"[INFO] Replaced texture for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            Material material = staticRenderers[mita.name + command.args[1]].material;
            material.mainTexture = Plugin.loadedTextures[command.args[2]];
            material.SetFloat("_EnableTextureTransparent", 1.0f);
            UnityEngine.Debug.Log($"[INFO] Replaced texture for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for texture replacement.");
        }
    }

    public static void ApplyReplaceMeshCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(4, command, mita.name))
            return;

        string meshKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');
        string subMeshName = command.args.Length >= 4 ? command.args[3] : Path.GetFileNameWithoutExtension(command.args[2]);
        Assimp.Mesh meshData = Plugin.loadedModels[meshKey].FirstOrDefault(mesh => mesh.Name == subMeshName);

        if (renderers.ContainsKey(mita.name + command.args[1]))
        {
            var skinnedRenderer = renderers[mita.name + command.args[1]];
            skinnedRenderer.sharedMesh = AssetLoader.BuildMesh(meshData, new AssetLoader.ArmatureData(skinnedRenderer), (command.args[1] == "Head"), mita.name);
            UnityEngine.Debug.Log($"[INFO] Replaced mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            var staticRenderer = staticRenderers[mita.name + command.args[1]];
            staticRenderer.GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData, null, (command.args[1] == "Head"), mita.name);
            UnityEngine.Debug.Log($"[INFO] Replaced mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh replacement.");
        }
    }

    public static void ApplyCreateSkinnedAppendixCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers)
    {
        if (ShouldSkip(3, command, mita.name))
            return;

        if (!renderers.ContainsKey(mita.name + command.args[2]))
        {
            UnityEngine.Debug.Log($"[WARNING] Parent renderer '{command.args[2]}' not found: skipping command {command.name} on '{mita.name}'.");
            return;
        }

        var parent = renderers[mita.name + command.args[2]];
        var objSkinned = UnityEngine.Object.Instantiate(parent, parent.transform.position, parent.transform.rotation, parent.transform.parent);
        objSkinned.name = command.args[1];
        objSkinned.material = new Material(parent.material);
        objSkinned.transform.localEulerAngles = new Vector3(-90f, 0, 0);
        objSkinned.gameObject.SetActive(true);

        renderers[mita.name + command.args[1]] = objSkinned;
        UnityEngine.Debug.Log($"[INFO] Created skinned appendix '{command.args[1]}' on '{mita.name}'.");
    }

    public static void ApplyCreateStaticAppendixCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(3, command, mita.name))
            return;

        if (staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            UnityEngine.Debug.Log($"[INFO] Found existing static renderer '{command.args[1]}' recovering it.");
            string[] args = command.args
                .Where((arg, index) => index != 2) // Exclude the element at index 2
                .ToArray();
            ApplyRecoverCommand(("recover", args), mita, null, staticRenderers);
            return;
        }

        var obj = new GameObject().AddComponent<MeshRenderer>();
        obj.name = command.args[1];
        obj.material = new Material(RecursiveFindMaterial(mita));
        obj.gameObject.AddComponent<MeshFilter>();

        obj.transform.parent = Utility.RecursiveFindChild(mita.transform, command.args[2]);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;
        obj.transform.localEulerAngles = new Vector3(-90f, 0, 0);
        obj.gameObject.SetActive(true);

        staticRenderers[mita.name + command.args[1]] = obj;
        UnityEngine.Debug.Log($"[INFO] Created static appendix {obj.name} on '{mita.name}'.");
    }

    private static Material RecursiveFindMaterial(GameObject mita)
    {
        return mita.GetComponentInChildren<SkinnedMeshRenderer>()?.material ?? new Material(Shader.Find("Standard"));
    }

    public static void ApplySetScaleCommand((string name, string[] args) command, GameObject mita)
    {
        if (ShouldSkip(5, command, mita.name))
            return;

        var obj = Utility.RecursiveFindChild(mita.transform, command.args[1]);
        if (obj)
        {
            obj.localScale = new Vector3(
                float.Parse(command.args[2], CultureInfo.InvariantCulture),
                float.Parse(command.args[3], CultureInfo.InvariantCulture),
                float.Parse(command.args[4], CultureInfo.InvariantCulture)
            );
            UnityEngine.Debug.Log($"[INFO] Set scale of '{command.args[1]}' on '{mita.name}' .");
        }
    }

    public static void ApplyMovePositionCommand((string name, string[] args) command, GameObject mita)
    {
        if (ShouldSkip(5, command, mita.name))
            return;

        var obj = Utility.RecursiveFindChild(mita.transform, command.args[1]);
        if (obj)
        {
            obj.localPosition += new Vector3(
                float.Parse(command.args[2], CultureInfo.InvariantCulture),
                float.Parse(command.args[3], CultureInfo.InvariantCulture),
                float.Parse(command.args[4], CultureInfo.InvariantCulture)
            );
            UnityEngine.Debug.Log($"[INFO] Moved position of '{command.args[1]}' on '{mita.name}'.");
        }
    }

    public static void ApplySetRotationCommand((string name, string[] args) command, GameObject mita)
    {
        if (ShouldSkip(6, command, mita.name))
            return;

        var obj = Utility.RecursiveFindChild(mita.transform, command.args[1]);
        if (obj)
        {
            obj.localRotation = new Quaternion(
                float.Parse(command.args[2], CultureInfo.InvariantCulture),
                float.Parse(command.args[3], CultureInfo.InvariantCulture),
                float.Parse(command.args[4], CultureInfo.InvariantCulture),
                float.Parse(command.args[5], CultureInfo.InvariantCulture)
            );
            UnityEngine.Debug.Log($"[INFO] Set rotation of '{command.args[1]}' on '{mita.name}'.");
        }
    }

}