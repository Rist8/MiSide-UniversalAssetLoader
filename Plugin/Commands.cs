using EPOOutline;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UtilityNamespace;
using static AssetLoader;
using Il2CppInterop.Runtime;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class Commands
{
    public static List<string> BlendShapedSkinnedAppendix = new List<string>();

    private static bool ShouldSkip(int start, (string name, string[] args) command, GameObject mita)
    {
        if (mita == null)
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Mita object is null, skipping...");
            return true;
        }
        string mitaName = mita.name;
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

    public static void RemoveOutlineTarget(Renderer RendererObject, GameObject mita)
    {
        var outlinables = Reflection.FindObjectsOfType<Outlinable>(true)
            .Where(mat => mat.gameObject.name.Contains("Interactive") ||
                          mat.gameObject.name.Contains("Mita") ||
                          mat.gameObject.name.Contains("Mila") ||
                          mat.gameObject.name.Contains(mita.name))
            .ToArray();

        if (outlinables.Length == 0)
        {
            return;
        }

        // Find all Outlinables closest to the RendererObject's transform within the threshold
        List<Outlinable> closestOutlinables = new List<Outlinable>();
        float closestDistance = float.MaxValue;
        Vector3 rendererPosition = RendererObject.transform.position;

        foreach (var outlinable in outlinables)
        {
            Vector3 outlinablePosition = outlinable.outlineTargets[0].Renderer.gameObject.transform.position;
            float distanceXZ = Vector2.Distance(new Vector2(rendererPosition.x, rendererPosition.z),
                                                 new Vector2(outlinablePosition.x, outlinablePosition.z));
            if (distanceXZ < closestDistance)
            {
                closestDistance = distanceXZ;
                closestOutlinables.Clear();
                closestOutlinables.Add(outlinable);
            }
            else if (Mathf.Abs(distanceXZ - closestDistance) <= 0.05f)
            {
                closestOutlinables.Add(outlinable);
            }
        }

        if (closestOutlinables.Count > 0)
        {
            // Create an OutlineTarget instance for each closest Outlinable
            OutlineTarget target = new OutlineTarget(RendererObject)
            {
                BoundsMode = BoundsMode.Manual,
                CullMode = UnityEngine.Rendering.CullMode.Off,
                Bounds = new Bounds(Vector3.forward, Vector3.one) { extents = Vector3.one }
            };

            foreach (var closestOutlinable in closestOutlinables)
            {
                for (int i = 0; i <= closestOutlinable.outlineTargets.Count - 1; i++)
                {
                    if (closestOutlinable.outlineTargets[i].Renderer == RendererObject)
                    {
                        closestOutlinable.outlineTargets.RemoveAt(i--);
                    }
                }
            }
        }
    }

    public static void AddOutlineTarget(Renderer rendererObject, GameObject mita)
    {
        var outlinables = Reflection.FindObjectsOfType<Outlinable>(true)
            .Where(mat => mat.gameObject.name.Contains("Interactive") ||
                          mat.gameObject.name.Contains("Mita") ||
                          mat.gameObject.name.Contains("Mila") ||
                          mat.gameObject.name.Contains(mita.name))
            .ToArray();

        if (outlinables.Length == 0)
        {
            return;
        }

        // Find all Outlinables closest to the RendererObject's transform within the threshold
        List<Outlinable> closestOutlinables = new List<Outlinable>();
        float closestDistance = float.MaxValue;
        Vector3 rendererPosition = rendererObject.transform.position;

        foreach (var outlinable in outlinables)
        {
            Vector3 outlinablePosition = outlinable.outlineTargets[0].Renderer.gameObject.transform.position;
            float distanceXZ = Vector2.Distance(new Vector2(rendererPosition.x, rendererPosition.z),
                                                 new Vector2(outlinablePosition.x, outlinablePosition.z));
            if (distanceXZ < closestDistance)
            {
                closestDistance = distanceXZ;
                closestOutlinables.Clear();
                closestOutlinables.Add(outlinable);
            }
            else if (Mathf.Abs(distanceXZ - closestDistance) <= 0.05f)
            {
                closestOutlinables.Add(outlinable);
            }
        }

        if (closestOutlinables.Count > 0)
        {
            // Create an OutlineTarget instance for each closest Outlinable
            OutlineTarget target = new OutlineTarget(rendererObject)
            {
                BoundsMode = BoundsMode.Manual,
                CullMode = UnityEngine.Rendering.CullMode.Off,
                Bounds = new Bounds(Vector3.forward, Vector3.one) { extents = Vector3.one }
            };

            foreach (var outlinable in closestOutlinables)
            {
                outlinable.outlineTargets.Add(target);
            }
        }
    }



    public static void ApplyRemoveCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(2, command, mita))
            return;

        if (renderers != null && renderers.ContainsKey(command.args[1]))
        {
            RemoveOutlineTarget(renderers[command.args[1]], mita);
            if (BlendShapedSkinnedAppendix.Contains(command.args[1]))
                BlendShapedSkinnedAppendix.Remove(command.args[1]);

            renderers[command.args[1]].gameObject.SetActive(false);
            UnityEngine.Debug.Log($"[INFO] Removed skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(command.args[1]))
        {
            RemoveOutlineTarget(staticRenderers[command.args[1]], mita);
            staticRenderers[command.args[1]].gameObject.SetActive(false);
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

        if (ShouldSkip(2, command, mita))
            return;

        if (renderers != null && renderers.ContainsKey(command.args[1]))
        {
            renderers[command.args[1]].gameObject.SetActive(true);
            UnityEngine.Debug.Log($"[INFO] Recovered skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(command.args[1]))
        {
            staticRenderers[command.args[1]].gameObject.SetActive(true);
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
        if (ShouldSkip(3, command, mita))
            return;

        string textureKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');

        if (renderers.ContainsKey(command.args[1]))
        {
            var materials = renderers[command.args[1]].materials;
            foreach (var mat in materials)
            {
                mat.mainTexture = AssetLoader.loadedTextures[textureKey];
                mat.SetFloat("_EnableTextureTransparent", 1.0f);
            }

            UnityEngine.Debug.Log($"[INFO] Replaced texture for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            // Material material = staticRenderers[command.args[1]].material;
            // material.mainTexture = AssetLoader.loadedTextures[textureKey];
            var materials = staticRenderers[command.args[1]].materials;
            foreach (var mat in materials)
            {
                mat.mainTexture = AssetLoader.loadedTextures[textureKey];
                mat.SetFloat("_EnableTextureTransparent", 1.0f);
            }
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
        if (ShouldSkip(4, command, mita))
            return;
        if (blendShapeKey == null)
            blendShapeKey = mita.name;
        if (blendShapeKey == "Player" && command.args[1] == "Arms")
            blendShapeKey = "PlayerArms";

        string meshKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');
        string subMeshName = command.args.Length >= 4 ? command.args[3] : Path.GetFileNameWithoutExtension(command.args[2]);
        Assimp.Mesh meshData = AssetLoader.loadedModels[meshKey].FirstOrDefault(mesh => mesh.Name == subMeshName);

        if (renderers.ContainsKey(command.args[1]))
        {
            var skinnedRenderer = renderers[command.args[1]];
            skinnedRenderer.sharedMesh = AssetLoader.BuildMesh(meshData, new AssetLoader.ArmatureData(skinnedRenderer),
                ((command.args[1] == "Head") || BlendShapedSkinnedAppendix.Contains(command.args[1]) || blendShapeKey == "PlayerArms"), blendShapeKey);
            UnityEngine.Debug.Log($"[INFO] Replaced mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            var staticRenderer = staticRenderers[command.args[1]];
            staticRenderer.GetComponent<MeshFilter>().mesh = AssetLoader.BuildMesh(meshData, null,
                ((command.args[1] == "Head") || BlendShapedSkinnedAppendix.Contains(command.args[1]) || blendShapeKey == "PlayerArms"), blendShapeKey);
            UnityEngine.Debug.Log($"[INFO] Replaced mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh replacement.");
        }
    }

    public static System.Collections.IEnumerator ApplyReplaceMeshCommandCoroutine(
    (string name, string[] args) command,
    GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers,
    Dictionary<string, MeshRenderer> staticRenderers,
    string blendShapeKey = null,
    float maxFrameTime = 1f / 240f // max time per frame in seconds
)
    {
        if (ShouldSkip(4, command, mita))
            yield break;

        if (blendShapeKey == null)
            blendShapeKey = mita.name;
        if (blendShapeKey == "Player" && command.args[1] == "Arms")
            blendShapeKey = "PlayerArms";

        string meshKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');
        string subMeshName = command.args.Length >= 4 ? command.args[3] : Path.GetFileNameWithoutExtension(command.args[2]);

        Assimp.Mesh meshData = AssetLoader.loadedModels[meshKey].FirstOrDefault(mesh => mesh.Name == subMeshName);

        if (renderers.ContainsKey(command.args[1]))
        {
            var skinnedRenderer = renderers[command.args[1]];

            var meshCoroutine = BuildMeshCoroutine(
                meshData,
                new ArmatureData(skinnedRenderer),
                (command.args[1] == "Head") || BlendShapedSkinnedAppendix.Contains(command.args[1]) || blendShapeKey == "PlayerArms",
                blendShapeKey,
                maxFrameTime
            );

            while (meshCoroutine.MoveNext())
            {
                if (meshCoroutine.Current is UnityEngine.Mesh resultMesh)
                {
                    skinnedRenderer.sharedMesh = resultMesh;
                    UnityEngine.Debug.Log($"[INFO] Replaced mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
                    yield break;
                }
                yield return null;
            }
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            var staticRenderer = staticRenderers[command.args[1]];

            var meshCoroutine = BuildMeshCoroutine(
                meshData,
                null,
                (command.args[1] == "Head") || BlendShapedSkinnedAppendix.Contains(command.args[1]) || blendShapeKey == "PlayerArms",
                blendShapeKey,
                maxFrameTime
            );

            while (meshCoroutine.MoveNext())
            {
                if (meshCoroutine.Current is UnityEngine.Mesh resultMesh)
                {
                    staticRenderer.GetComponent<MeshFilter>().mesh = resultMesh;
                    UnityEngine.Debug.Log($"[INFO] Replaced mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
                    yield break;
                }
                yield return null;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh replacement.");
        }
    }

    public static void ApplyCreateSkinnedAppendixCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, bool Player = false)
    {

        if (ShouldSkip(3, command, mita))
            return;

        if (!renderers.ContainsKey(command.args[2]))
        {
            UnityEngine.Debug.Log($"[WARNING] Parent renderer '{command.args[2]}' not found: skipping command {command.name} on '{mita.name}'.");
            return;
        }
        var parent = renderers[command.args[2]];
        var objSkinned = UnityEngine.Object.Instantiate(parent, parent.transform.position, parent.transform.rotation, parent.transform.parent);
        objSkinned.name = command.args[1];
        objSkinned.material.renderQueue = 2499;
        objSkinned.transform.localEulerAngles = new Vector3(-90f, 0, 0);
        objSkinned.gameObject.SetActive(true);

        {
            var materials = Reflection.FindObjectsOfType<Material_ColorVariables>(true)
                .Where(mat => mat.gameObject.name.Contains("Mita") || mat.gameObject.name.Contains("MenuGame"))
                .ToArray();

            foreach (var material in materials)
            {
                if (material.gameObject.transform.FindChild(mita.name) == null && material.gameObject != mita)
                {
                    continue;
                }

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

        if (command.args[2] == "Body" && SceneHandler.currentSceneName == "Scene 10 - ManekenWorld")
        {
            var location10 = mita.GetComponent<Location10_MitaInShadow>();
            if (location10 != null)
            {
                location10.rend = objSkinned;
            }
        }

        AddOutlineTarget(objSkinned, mita);

        renderers[command.args[1]] = objSkinned;

        if ((command.args[2] == "Head" || (command.args[2] == "Arms" && Player)) && !BlendShapedSkinnedAppendix.Contains(command.args[1]))
            BlendShapedSkinnedAppendix.Add(command.args[1]);

        UnityEngine.Debug.Log($"[INFO] Created skinned appendix '{command.args[1]}' on '{mita.name}'.");
    }

    public static void ApplyCreateStaticAppendixCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(3, command, mita))
            return;

        if (staticRenderers.ContainsKey(command.args[1]))
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
        obj.material.renderQueue = 2499;
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

            foreach (var material in materials)
            {
                if (!material.gameObject.name.Contains("MenuGame") &&
                    material.gameObject.transform.GetChild(0).gameObject != mita &&
                    material.gameObject != mita
                )
                {
                    continue;
                }

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

        AddOutlineTarget(obj, mita);

        staticRenderers[command.args[1]] = obj;
        UnityEngine.Debug.Log($"[INFO] Created static appendix {obj.name} on '{mita.name}'.");
    }

    private static Material RecursiveFindMaterial(GameObject mita)
    {
        return mita.GetComponentInChildren<SkinnedMeshRenderer>()?.material ?? new Material(Shader.Find("Standard"));
    }

    public static void ApplySetScaleCommand((string name, string[] args) command, GameObject mita)
    {
        if (ShouldSkip(5, command, mita))
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
    public static void ApplyResizeMeshCommand((string name, string[] args) command, GameObject mita,
        Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(3, command, mita))
            return;

        if (renderers.ContainsKey(command.args[1]))
        {
            var skinnedRenderer = renderers[command.args[1]];
            var mesh = skinnedRenderer.sharedMesh;
            AssetLoader.ResizeMesh(ref mesh, float.Parse(command.args[2], CultureInfo.InvariantCulture));
            skinnedRenderer.sharedMesh = mesh;
            UnityEngine.Debug.Log($"[INFO] Resized mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            var staticRenderer = staticRenderers[command.args[1]];
            var mesh = staticRenderer.GetComponent<MeshFilter>().mesh;
            AssetLoader.ResizeMesh(ref mesh, float.Parse(command.args[2], CultureInfo.InvariantCulture));
            staticRenderer.GetComponent<MeshFilter>().mesh = mesh;
            UnityEngine.Debug.Log($"[INFO] Resized mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh resizement.");
        }
    }

    public static void ApplyMoveMeshCommand((string name, string[] args) command, GameObject mita,
        Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(5, command, mita))
            return;

        if (renderers.ContainsKey(command.args[1]))
        {
            var skinnedRenderer = renderers[command.args[1]];
            var mesh = skinnedRenderer.sharedMesh;
            AssetLoader.MoveMesh(ref mesh,
                new Vector3(
                    float.Parse(command.args[2], CultureInfo.InvariantCulture),
                    float.Parse(command.args[3], CultureInfo.InvariantCulture),
                    float.Parse(command.args[4], CultureInfo.InvariantCulture)
                )
            );
            UnityEngine.Debug.Log($"[INFO] Moved mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            var staticRenderer = staticRenderers[command.args[1]];
            var mesh = staticRenderer.GetComponent<MeshFilter>().mesh;
            AssetLoader.MoveMesh(ref mesh,
                new Vector3(
                    float.Parse(command.args[2], CultureInfo.InvariantCulture),
                    float.Parse(command.args[3], CultureInfo.InvariantCulture),
                    float.Parse(command.args[4], CultureInfo.InvariantCulture)
                )
            );
            UnityEngine.Debug.Log($"[INFO] Moved mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh movement.");
        }
    }

    public static void ApplyRotateMeshCommand((string name, string[] args) command, GameObject mita,
        Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(5, command, mita))
            return;

        if (renderers.ContainsKey(command.args[1]))
        {
            var skinnedRenderer = renderers[command.args[1]];
            var mesh = skinnedRenderer.sharedMesh;
            AssetLoader.RotateMesh(ref mesh,
                new Vector3(
                    float.Parse(command.args[2], CultureInfo.InvariantCulture),
                    float.Parse(command.args[3], CultureInfo.InvariantCulture),
                    float.Parse(command.args[4], CultureInfo.InvariantCulture)
                )
            );
            skinnedRenderer.sharedMesh = mesh;
            UnityEngine.Debug.Log($"[INFO] Rotated mesh for skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            var staticRenderer = staticRenderers[command.args[1]];
            var mesh = staticRenderer.GetComponent<MeshFilter>().mesh;
            AssetLoader.RotateMesh(ref mesh,
                new Vector3(
                    float.Parse(command.args[2], CultureInfo.InvariantCulture),
                    float.Parse(command.args[3], CultureInfo.InvariantCulture),
                    float.Parse(command.args[4], CultureInfo.InvariantCulture)
                )
            );
            staticRenderer.GetComponent<MeshFilter>().mesh = mesh;
            UnityEngine.Debug.Log($"[INFO] Rotated mesh for static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for mesh rotation.");
        }
    }

    public static void ApplyMovePositionCommand((string name, string[] args) command, GameObject mita)
    {
        if (ShouldSkip(5, command, mita))
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
        if (ShouldSkip(6, command, mita))
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
        if (ShouldSkip(3, command, mita))
            return;

        string txtKey = command.args[2].Replace(@"\\", @"\").TrimStart('.', '\\');

        if (renderers.ContainsKey(command.args[1]))
        {
            ReadShaderParams(renderers[command.args[1]].material, txtKey);
            UnityEngine.Debug.Log($"[INFO] Set shader parameter '{command.args[2]}' on '{renderers[command.args[1]].name}' .");
        }
        else if (staticRenderers.ContainsKey(command.args[1]))
        {
            ReadShaderParams(staticRenderers[command.args[1]].material, txtKey);
            UnityEngine.Debug.Log($"[INFO] Set shader parameter '{command.args[2]}' on '{renderers[command.args[1]].name}' .");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}' for shader parameter setting.");
        }
    }

    public static void ApplyRemoveOutlineCommand((string name, string[] args) command, GameObject mita,
    Dictionary<string, SkinnedMeshRenderer> renderers, Dictionary<string, MeshRenderer> staticRenderers)
    {
        if (ShouldSkip(2, command, mita))
            return;

        if (renderers != null && renderers.ContainsKey(command.args[1]))
        {
            RemoveOutlineTarget(renderers[command.args[1]], mita);
            UnityEngine.Debug.Log($"[INFO] Removed outline from skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(command.args[1]))
        {
            RemoveOutlineTarget(staticRenderers[command.args[1]], mita);
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
        if (ShouldSkip(2, command, mita))
            return;

        if (renderers != null && renderers.ContainsKey(command.args[1]))
        {
            AddOutlineTarget(renderers[command.args[1]], mita);
            UnityEngine.Debug.Log($"[INFO] Added outline to skinned renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else if (staticRenderers != null && staticRenderers.ContainsKey(command.args[1]))
        {
            AddOutlineTarget(staticRenderers[command.args[1]], mita);
            UnityEngine.Debug.Log($"[INFO] Added outline to static renderer '{command.args[1]}' on '{mita.name}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Renderer '{command.args[1]}' not found on '{mita.name}'.");
        }
    }

    public static void ApplyReplace2DCommand((string name, string[] args) command, Dictionary<string, Texture2D> textures)
    {
        UnityEngine.Debug.Log($"[INFO] Replacing Texture 2D '{command.args[0]}' with '{command.args[1]}'.");
        if (textures.ContainsKey(command.args[0]))
        {
            if (command.args[0] == "Cursor")
            {
                Cursor.SetCursor(loadedTextures[command.args[1]], Vector2.zero, CursorMode.Auto);
                UnityEngine.Debug.Log($"[INFO] Replaced Cursor with '{command.args[1]}'.");
                return;
            }
            string textureKey = command.args[1].Replace(@"\\", @"\").TrimStart('.', '\\');
            var imageData = LZ4Pickler.Unpickle(loadedTexturesRaw[textureKey]);
            textures[command.args[0]].LoadImage(imageData);
            UnityEngine.Debug.Log($"[INFO] Replaced Texture 2D '{command.args[0]}' with '{textureKey}'.");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Texture 2D '{command.args[0]}' not found in the game.");
        }
    }
    public static System.Collections.IEnumerator ApplyReplaceSprite((string name, string[] args) command, Dictionary<string, Sprite> sprites)
    {
        UnityEngine.Debug.Log($"[INFO] Replacing Sprite '{command.args[0]}' with '{command.args[1]}'.");

        if (!sprites.ContainsKey(command.args[0]))
        {
            UnityEngine.Debug.LogWarning($"[WARNING] Sprite '{command.args[0]}' not found.");
            yield break;
        }

        string textureKey = command.args[1].Replace(@"\\", @"\").TrimStart('.', '\\');

        Sprite newSprite = null;
        bool success = false;
        while (!success || SceneHandler.currentSceneName == "Scene 18 - 2D")
        {
            if (loadedTextures.TryGetValue(textureKey, out var texture) && newSprite == null)
            {
                newSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                newSprite.name = command.args[1];
            }

            success = Utility.ReplaceSpriteInReferences(sprites[command.args[0]], newSprite);

            yield return new WaitForSeconds(0.1f);
        }

        UnityEngine.Debug.Log($"[INFO] Successfully replaced Sprite '{command.args[0]}' with '{textureKey}'.");
    }

    public static void ApplyReplaceAudioCommand((string name, string[] args) command, Dictionary<string, AudioClip> audioClips)
    {
        UnityEngine.Debug.Log($"[INFO] Replacing AudioClip '{command.args[0]}' with '{command.args[1]}'.");
        if (!audioClips.ContainsKey(command.args[0]))
        {
            UnityEngine.Debug.LogWarning($"[WARNING] AudioClip '{command.args[0]}' not found.");
            return;
        }

        string audioKey = command.args[1].Replace(@"\\", @"\").TrimStart('.', '\\');

        if (!loadedAudio.ContainsKey(audioKey))
        {
            UnityEngine.Debug.LogWarning($"[WARNING] AudioClip '{audioKey}' not found in loaded audio.");
            return;
        }

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        byte[] decompressedData;
        using (var compressedStream = new MemoryStream(loadedAudioData[audioKey]))
        using (var lz4Stream = LZ4Stream.Decode(compressedStream))
        {
            using (var decompressedStream = new MemoryStream())
            {
                lz4Stream.CopyTo(decompressedStream);
                decompressedData = decompressedStream.ToArray();
            }
        }

        // Convert back to float array
        float[] audioData = MemoryMarshal.Cast<byte, float>(decompressedData.AsSpan()).ToArray();

        var targetClip = audioClips[command.args[0]];
        int totalSamples = audioData.Length;
        int channels = targetClip.channels;
        totalSamples /= channels;

        if (targetClip.samples < totalSamples)
        {
            UnityEngine.Debug.Log("[INFO] AudioClip samples are less than the new audio data, cropping the audio data.");
            totalSamples = targetClip.samples;
            float[] batch = new float[totalSamples * channels];
            Array.Copy(audioData, 0, batch, 0, totalSamples * channels);
            audioData = batch;
        }

        AudioClip.SetData(targetClip, audioData, totalSamples, 0);

        stopwatch.Stop();
        UnityEngine.Debug.Log($"[INFO] Successfully replaced AudioClip '{command.args[0]}' with '{audioKey}' in {stopwatch.ElapsedMilliseconds}ms.");
    }
}
