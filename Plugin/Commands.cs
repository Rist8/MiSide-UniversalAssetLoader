using EPOOutline;
using HarmonyLib;
using System.Globalization;
using UnityEngine;
using UtilityNamespace;

public class Commands
{
    public static List<string> BlendShapedSkinnedAppendix = new List<string>();

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
                    if (i == command.args.Length - 1)
                    {
                        UnityEngine.Debug.Log($"[INFO] Skipping command '{command.name}' on '{mitaName}' because keyword '{argsName}' was not found.");
                        return true;
                    }
                    continue;
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

    public static void RemoveOutlineTarget(Renderer RendererObject)
    {
        var outlinables = Reflection.FindObjectsOfType<Outlinable>(true)
                            .Where(mat => mat.gameObject.name.Contains("Mita") || mat.gameObject.name.Contains("Mila"))
                            .ToArray();
        if (outlinables.Length > 0)
        {
            for (int i = 0; i <= outlinables[0].outlineTargets.Count - 1; i++)
            {
                if (outlinables[0].outlineTargets[i].Renderer == RendererObject)
                {
                    outlinables[0].outlineTargets.RemoveAt(i--);
                }
            }

        }
    }

    public static void AddOutlineTarget(Renderer RendererObject)
    {
        var outlinables = Reflection.FindObjectsOfType<Outlinable>(true)
                        .Where(mat => mat.gameObject.name.Contains("Mita") || mat.gameObject.name.Contains("Mila"))
                        .ToArray();

        if (outlinables.Length > 0)
        {
            // Create an OutlineTarget instance
            OutlineTarget target = new OutlineTarget(RendererObject)
            {
                BoundsMode = BoundsMode.Manual,
                CullMode = UnityEngine.Rendering.CullMode.Off,
                Bounds = new Bounds(Vector3.forward, Vector3.one) { extents = Vector3.one }
            };

            // Add the OutlineTarget to the first matching outlinable
            outlinables[0].outlineTargets.Add(target);
        }
    }


    public static void ApplyRemoveCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(2, command, mita.name))
            return;

        if (renderers != null && renderers.ContainsKey(mita.name + command.args[1]))
        {
            RemoveOutlineTarget(renderers[mita.name + command.args[1]]);
            if (BlendShapedSkinnedAppendix.Contains(command.args[1]))
                BlendShapedSkinnedAppendix.Remove(command.args[1]);

            renderers[mita.name + command.args[1]].gameObject.SetActive(false);
            UnityEngine.Debug.Log($"[INFO] Removed skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            RemoveOutlineTarget(staticRenderers[mita.name + command.args[1]]);
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
            material.mainTexture = Plugin.loadedTextures[textureKey];
            material.SetFloat("_EnableTextureTransparent", 1.0f);
            UnityEngine.Debug.Log($"[INFO] Replaced texture for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            Material material = staticRenderers[mita.name + command.args[1]].material;
            material.mainTexture = Plugin.loadedTextures[textureKey];
            material.SetFloat("_EnableTextureTransparent", 1.0f);
            UnityEngine.Debug.Log($"[INFO] Replaced texture for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for texture replacement.");
        }
    }

    public static void ApplyReplaceMeshCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers, string blendShapeKey = null)
    {
        if (ShouldSkip(4, command, mita.name))
            return;
        if (blendShapeKey == null)
            blendShapeKey = mita.name;
        if (blendShapeKey == "Player" && command.args[1] == "Arms")
            blendShapeKey = "PlayerArms";

        string meshKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');
        string subMeshName = command.args.Length >= 4 ? command.args[3] : Path.GetFileNameWithoutExtension(command.args[2]);
        Assimp.Mesh meshData = Plugin.loadedModels[meshKey].FirstOrDefault(mesh => mesh.Name == subMeshName);

        if (renderers.ContainsKey(mita.name + command.args[1]))
        {
            var skinnedRenderer = renderers[mita.name + command.args[1]];
            skinnedRenderer.sharedMesh = AssetLoader.BuildMesh(meshData, new AssetLoader.ArmatureData(skinnedRenderer),
                ((command.args[1] == "Head") || BlendShapedSkinnedAppendix.Contains(command.args[1]) || blendShapeKey == "PlayerArms"), blendShapeKey);
            UnityEngine.Debug.Log($"[INFO] Replaced mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            var staticRenderer = staticRenderers[mita.name + command.args[1]];
            staticRenderer.GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData, null,
                ((command.args[1] == "Head") || BlendShapedSkinnedAppendix.Contains(command.args[1]) || blendShapeKey == "PlayerArms"), blendShapeKey);
            UnityEngine.Debug.Log($"[INFO] Replaced mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh replacement.");
        }
    }

    public static void ApplyCreateSkinnedAppendixCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, bool Player = false)
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

        {
            var materials = Reflection.FindObjectsOfType<Material_ColorVariables>(true)
                .Where(mat => mat.gameObject.name.Contains("Mita") || mat.gameObject.name.Contains("MenuGame"))
                .ToArray();

            if (materials.Length > 0)
            {
                var material = materials[0];
                var meshes = material.meshes;

                // Create a new array with one additional slot
                var newMeshes = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GameObject>(meshes.Length + 1);

                // Copy the existing meshes to the new array
                for (int i = 0; i < meshes.Length; i++)
                {
                    newMeshes[i] = meshes[i];
                }

                // Add the new object to the end of the array
                newMeshes[meshes.Length] = objSkinned.gameObject;

                // Assign the updated meshes array back to the material
                material.meshes = newMeshes;
            }
        }

        AddOutlineTarget(objSkinned);

        renderers[mita.name + command.args[1]] = objSkinned;

        if ((command.args[2] == "Head" || (command.args[2] == "Arms" && Player)) && !BlendShapedSkinnedAppendix.Contains(command.args[1]))
            BlendShapedSkinnedAppendix.Add(command.args[1]);

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

        {
            var materials = Reflection.FindObjectsOfType<Material_ColorVariables>(true)
                .Where(mat => mat.gameObject.name.Contains("Mita") || mat.gameObject.name.Contains("MenuGame"))
                .ToArray();

            if (materials.Length > 0)
            {
                var material = materials[0];
                var meshes = material.meshes;

                // Create a new array with one additional slot
                var newMeshes = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GameObject>(meshes.Length + 1);

                // Copy the existing meshes to the new array
                for (int i = 0; i < meshes.Length; i++)
                {
                    newMeshes[i] = meshes[i];
                }

                // Add the new object to the end of the array
                newMeshes[meshes.Length] = obj.gameObject;

                // Assign the updated meshes array back to the material
                material.meshes = newMeshes;
            }
        }

        AddOutlineTarget(obj);

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

    private static void ReadShaderParams(Material material, string shaderParamPath)
    {
        string filePath = PluginInfo.AssetsFolder + "/" + shaderParamPath + ".txt";
        try
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;

                    string[] parts = line.Split(' ');
                    try
                    {
                        switch (parts[0])
                        {
                            case "EnableKeyword":
                                material.EnableKeyword(parts[1]);
                                break;
                            case "DisableKeyword":
                                material.DisableKeyword(parts[1]);
                                break;
                            case "SetFloat":
                                material.SetFloat(parts[1], float.Parse(parts[2], CultureInfo.InvariantCulture));
                                break;
                            case "SetInt":
                                material.SetInt(parts[1], int.Parse(parts[2]));
                                break;
                            case "SetVector":
                                material.SetVector(parts[1], new Vector4(
                                    float.Parse(parts[2].Split(',')[0].TrimStart('('), CultureInfo.InvariantCulture),
                                    float.Parse(parts[2].Split(',')[1], CultureInfo.InvariantCulture),
                                    float.Parse(parts[2].Split(',')[2], CultureInfo.InvariantCulture),
                                    float.Parse(parts[2].Split(',')[3].TrimEnd(')'), CultureInfo.InvariantCulture)
                                ));
                                break;
                            case "SetColor":
                                Color color = new Color(
                                    float.Parse(parts[2].Split(',')[0].TrimStart('('), CultureInfo.InvariantCulture),
                                    float.Parse(parts[2].Split(',')[1], CultureInfo.InvariantCulture),
                                    float.Parse(parts[2].Split(',')[2], CultureInfo.InvariantCulture),
                                    float.Parse(parts[2].Split(',')[3].TrimEnd(')'), CultureInfo.InvariantCulture)
                                );
                                if (parts.Length == 4)
                                {
                                    color *= float.Parse(parts[3], CultureInfo.InvariantCulture);
                                }

                                material.SetColor(parts[1], color);
                                break;
                            default:
                                UnityEngine.Debug.LogWarning($"[WARNING] Unknown shader parameter '{parts[0]}' in shader '{material.shader.name}'.");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"[WARNING] Shader parameter '{parts[0]}' not found in shader '{material.shader.name}'.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Shader parameter file '{filePath}' not found.");
            return;
        }
    }

    public static void ApplyShaderParamsCommand((string name, string[] args) command, GameObject mita, Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(3, command, mita.name))
            return;

        string txtKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');

        if (renderers.ContainsKey(mita.name + command.args[1]))
        {
            ReadShaderParams(renderers[mita.name + command.args[1]].material, txtKey);
            UnityEngine.Debug.Log($"[INFO] Set shader parameter '{command.args[2]}' on '{renderers[mita.name + command.args[1]].name}' .");
        }
        else if (staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            ReadShaderParams(staticRenderers[mita.name + command.args[1]].material, txtKey);
            UnityEngine.Debug.Log($"[INFO] Set shader parameter '{command.args[2]}' on '{renderers[mita.name + command.args[1]].name}' .");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for shader parameter setting.");
        }
    }

    public static void ApplyRemoveOutlineCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(2, command, mita.name))
            return;

        if (renderers != null && renderers.ContainsKey(mita.name + command.args[1]))
        {
            RemoveOutlineTarget(renderers[mita.name + command.args[1]]);
            UnityEngine.Debug.Log($"[INFO] Removed outline from skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            RemoveOutlineTarget(staticRenderers[mita.name + command.args[1]]);
            UnityEngine.Debug.Log($"[INFO] Removed outline from static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}'.");
        }

    }

    public static void ApplyAddOutlineCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(2, command, mita.name))
            return;

        if (renderers != null && renderers.ContainsKey(mita.name + command.args[1]))
        {
            AddOutlineTarget(renderers[mita.name + command.args[1]]);
            UnityEngine.Debug.Log($"[INFO] Added outline to skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(mita.name + command.args[1]))
        {
            AddOutlineTarget(staticRenderers[mita.name + command.args[1]]);
            UnityEngine.Debug.Log($"[INFO] Added outline to static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}'.");
        }
    }
}
