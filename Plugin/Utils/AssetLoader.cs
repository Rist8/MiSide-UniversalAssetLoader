using Assimp;
using Assimp.Configs;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Unity.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json;
using MagicaCloth;
using UnityEngine.Rendering;

public class AssetLoader
{
    public static Texture2D LoadTexture(string file) => LoadTexture(Path.GetFileNameWithoutExtension(file), File.OpenRead(file));
    public static Texture2D LoadTexture(string name, Stream stream)
    {
        Texture2D texture = new Texture2D(2, 2);
        var result = Reflection.ForceUseStaticMethod<bool>(typeof(ImageConversion), "LoadImage", texture, (Il2CppStructArray<byte>)ReadStream(stream));
        if (!result) return null;

        texture.name = name;
        texture.hideFlags = HideFlags.DontSave;
        return texture;
    }

    public static AudioClip LoadAudio(string file) => LoadAudio(Path.GetFileNameWithoutExtension(file), File.OpenRead(file));
    public static AudioClip LoadAudio(string name, Stream stream)
    {
        AudioClip audioClip = null;
        using (var vorbis = new NVorbis.VorbisReader(new MemoryStream(ReadStream(stream), false)))
        {
            float[] _audioBuffer = new float[vorbis.Channels * vorbis.TotalSamples]; // Just dump everything
            int read = vorbis.ReadSamples(_audioBuffer, 0, _audioBuffer.Length);
            audioClip = AudioClip.Create(name, (int)vorbis.TotalSamples, vorbis.Channels, vorbis.SampleRate, false);
            AudioClip.SetData(audioClip, _audioBuffer, ((System.Array)_audioBuffer).Length / audioClip.channels, 0);
        }
        if (audioClip == null) return null;

        audioClip.hideFlags = HideFlags.DontSave;
        return audioClip;
    }
    public static UnityEngine.Mesh ConvertMeshToUnity(Assimp.Mesh aMesh, float scale = 1.0f)
    {
        var uMesh = new UnityEngine.Mesh()
        {
            name = aMesh.Name,
            indexFormat = IndexFormat.UInt16,
        };

        uMesh.SetVertices(aMesh.Vertices.Select(vertex => new Vector3(-vertex.X, vertex.Y, vertex.Z) * scale).ToArray());
        uMesh.SetNormals(aMesh.Normals.Select(normal => new Vector3(-normal.X, normal.Y, normal.Z)).ToArray());
        uMesh.SetUVs(0, aMesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToArray());
        uMesh.triangles = aMesh.GetIndices();
        uMesh.RecalculateBounds();

        return uMesh;
    }

    public static Assimp.Mesh[] LoadFBX(string file) => LoadFBX(File.OpenRead(file));
    public static Assimp.Mesh[] LoadFBX(Stream stream)
    {
        AssimpContext importer = new AssimpContext();
        importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
        var fbxFile = importer.ImportFileFromStream(stream, PostProcessSteps.Triangulate | PostProcessSteps.FlipWindingOrder);
        return fbxFile.Meshes.ToArray();
    }

    public class ArmatureData
    {
        public Dictionary<string, (int index, UnityEngine.Matrix4x4 bindpose, Transform bone)> bones;
        public List<MagicaBoneCloth> clothNodes;
        public SkinnedMeshRenderer source;

        public ArmatureData(SkinnedMeshRenderer source)
        {
            this.source = source;
            bones = new Dictionary<string, (int index, UnityEngine.Matrix4x4 bindpose, Transform bone)>(source.bones.Length);
            clothNodes = new List<MagicaBoneCloth>();

            var sharedMeshBindposes = source.sharedMesh.bindposes;
            for (int i = 0; i < source.bones.Length; i++)
            {
                var boneName = FixedBoneName(source.bones[i].name);

                if (!bones.ContainsKey(boneName))
                {
                    bones[boneName] = (i, sharedMeshBindposes[i], source.bones[i]);

                }
            }

            var allBones = CollectBonesIteratively(source.rootBone.transform);

            foreach (var bone in allBones)
            {
                if (bone.TryGetComponent<MagicaBoneCloth>(out var boneCloth))
                {
                    clothNodes.Add(boneCloth);
                }
            }
        }

        private List<Transform> CollectBonesIteratively(Transform root)
        {
            var boneList = new List<Transform>();
            if (root == null) return boneList;

            var stack = new Stack<Transform>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current == null || !current.gameObject.activeSelf || current.name.Contains("Collider"))
                {
                    continue;
                }

                boneList.Add(current);

                for (int i = 0; i < current.childCount; ++i)
                {
                    stack.Push(current.GetChild(i));
                }
            }

            return boneList;
        }

    }

    public static void ResizeMesh(ref UnityEngine.Mesh mesh, float scale)
    {
        var vertices = mesh.vertices;
        mesh.SetVertices(vertices.Select(v => v * scale).ToArray());
    }


    public static UnityEngine.Mesh BuildMesh(Assimp.Mesh fbxMesh, ArmatureData armature = null, bool addBlendShape = false, string blendShapeName = "Mita")
    {
        blendShapeName = blendShapeName.Replace("MitaPerson ", "").Replace("MilaPerson ", "");

        var mesh = ConvertMeshToUnity(fbxMesh);

        if (armature == null)
        {
            return mesh;
        }

        int boneCount = armature.bones.Count;
        int bonesPerVertexLength = fbxMesh.VertexCount;

        var bindposes = new UnityEngine.Matrix4x4[boneCount];
        for (int i = 0; i < boneCount; i++)
        {
            bindposes[i] = UnityEngine.Matrix4x4.identity;
        }

        var bonesPerVertex = new List<BoneWeight1>[bonesPerVertexLength];

        foreach (var bone in fbxMesh.Bones)
        {
            if (!armature.bones.TryGetValue(FixedBoneName(bone.Name), out var armatureBone))
            {
                continue;
            }

            int armatureBoneIndex = armatureBone.index;

            foreach (var vertex in bone.VertexWeights)
            {
                int vertexID = vertex.VertexID;

                if (vertexID >= bonesPerVertexLength)
                {
                    continue;
                }

                bonesPerVertex[vertexID] ??= new List<BoneWeight1>();

                bonesPerVertex[vertexID].Add(new BoneWeight1()
                {
                    boneIndex = armatureBoneIndex,
                    weight = vertex.Weight
                });
            }

            if (armatureBoneIndex >= 0 && armatureBoneIndex < boneCount)
            {
                bindposes[armatureBoneIndex] = armatureBone.bindpose;
            }
        }

        var bonesPerVertexArray = new NativeArray<byte>(bonesPerVertexLength, Allocator.Temp);
        var weights = new List<BoneWeight1>();

        for (int i = 0; i < bonesPerVertex.Length; i++)
        {
            var boneList = bonesPerVertex[i];
            if (boneList != null)
            {
                boneList.Sort((a, b) => b.weight.CompareTo(a.weight));

                if (boneList.Count > 4)
                {
                    boneList.RemoveRange(4, boneList.Count - 4);
                }

                float totalWeight = boneList.Sum(bw => bw.weight);
                for (int j = 0; j < boneList.Count; j++)
                {
                    var boneWeight = boneList[j];
                    boneWeight.weight /= totalWeight;
                    boneList[j] = boneWeight;
                }

                bonesPerVertexArray[i] = (byte)boneList.Count;
                weights.AddRange(boneList);
            }
            else
            {
                weights.Add(new BoneWeight1() { boneIndex = 0, weight = 0 });
                bonesPerVertexArray[i] = 1;
            }
        }

        var weightsArray = new NativeArray<BoneWeight1>(weights.Count, Allocator.Temp);
        for (int i = 0; i < weights.Count; i++)
        {
            weightsArray[i] = weights[i];
        }

        mesh.SetBoneWeights(bonesPerVertexArray, weightsArray);
        mesh.bindposes = bindposes;

        foreach (var clothNode in armature.clothNodes)
        {
            clothNode.cullRendererList ??= new Il2CppSystem.Collections.Generic.List<Renderer> { };
            clothNode.cullRendererList.Add(armature.source);
        }

        // Add Blendshape Processing
        if (addBlendShape || fbxMesh.HasMeshAnimationAttachments)
        {
            UnityEngine.Debug.Log($"[INFO] Blendshape name: {blendShapeName}");

            string blendShapeOrdersPath = Path.Combine(PluginInfo.AssetsFolder, "blendshape_orders.json");

            var blendShapeOrders = new Dictionary<string, List<string>>();

            if (File.Exists(blendShapeOrdersPath))
            {
                string jsonContent = File.ReadAllText(blendShapeOrdersPath);
                blendShapeOrders = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonContent);
            }

            if (!blendShapeOrders.TryGetValue(blendShapeName, out var blendShapeOrder) || blendShapeOrder.Count == 0)
            {
                // UnityEngine.Debug.LogWarning($"[WARNING] No blendshape order found for {blendShapeName}, using default fbx order");
                // blendShapeOrder = fbxMesh.MeshAnimationAttachments.Select(bs => bs.Name).ToList();
                // use Mita as default
                blendShapeOrder = blendShapeOrders["Mita"];
                UnityEngine.Debug.LogWarning($"[WARNING] No blendshape order found for {blendShapeName}, using default Mita order");
            }

            var blendShapeIndex = blendShapeOrder
                .Select((name, index) => new { name, index })
                .ToDictionary(x => x.name, x => x.index);

            var blendShapes = fbxMesh.MeshAnimationAttachments
                .Where(bs => blendShapeIndex.ContainsKey(bs.Name))
                .OrderBy(bs => blendShapeIndex[bs.Name])
                .ToList();

            int vertexCount = mesh.vertexCount;
            int normalCount = mesh.normals.Length;

            var deltaVerts = new Vector3[vertexCount];
            var deltaNormals = new Vector3[normalCount];

            var vertices = mesh.vertices;
            var normals = mesh.normals;

            int missingBlendShapesCount = blendShapeOrder.Count - blendShapes.Count;
            for (int i = 0; i < missingBlendShapesCount; i++)
            {
                blendShapes.Add(new MeshAnimationAttachment { Name = $"DummyBlendShape_{i}" });
            }

            foreach (var blendShape in blendShapes)
            {
                string name = blendShape.Name;

                Array.Clear(deltaVerts, 0, vertexCount);
                Array.Clear(deltaNormals, 0, normalCount);

                int maxCount = Math.Max(blendShape.VertexCount, blendShape.Normals.Count);

                for (int i = 0; i < maxCount; i++)
                {
                    if (blendShape.HasVertices && i < blendShape.VertexCount)
                    {
                        var blendVertex = blendShape.Vertices[i];
                        deltaVerts[i] = new Vector3(-blendVertex.X, blendVertex.Y, blendVertex.Z) - vertices[i];
                    }

                    if (blendShape.HasNormals && i < blendShape.Normals.Count)
                    {
                        var blendNormal = blendShape.Normals[i];
                        deltaNormals[i] = new Vector3(-blendNormal.X, blendNormal.Y, blendNormal.Z) - normals[i];
                    }
                }

                mesh.AddBlendShapeFrame(name, 100.0f, deltaVerts, deltaNormals, null);
                //Debug.Log($"[INFO] New blendshape loaded: {name}");
            }
            Debug.Log($"[INFO] Blendshapes loaded");
        }

        // Recalculations
        mesh.RecalculateBoundsImpl(MeshUpdateFlags.Default);
        return mesh;
    }

    public static string FixedBoneName(string name) => name.Replace(" ", "_").Replace(".", "_");

    public static byte[] ReadStream(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static string[] GetAllFilesWithExtensions(string directory, params string[] extensions)
    {
        return extensions.SelectMany(extension => Directory.GetFiles(directory, "*." + extension, SearchOption.AllDirectories)).ToArray();
    }
}
