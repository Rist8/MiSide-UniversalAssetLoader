using Assimp;
using Assimp.Configs;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Unity.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json;
using MagicaCloth;
using UnityEngine.Rendering;
using System.Collections.Concurrent;
using System.Diagnostics;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System.Runtime.InteropServices;

public class AssetLoader
{
    public static Dictionary<string, Assimp.Mesh[]>? loadedModels;
    public static Dictionary<string, Texture2D>? loadedTextures;
    public static List<int> loadedTextureInstanceIds;
    public static Dictionary<string, Il2CppStructArray<byte>>? loadedTexturesRaw;
    public static Dictionary<string, byte[]>? loadedAudioData;
    public static Dictionary<string, AudioClip>? loadedAudio;
    public static List<int> loadedAudioInstanceIds;

    public static System.Collections.IEnumerator LoadAssetsForPatchCoroutine()
    {
        if (loadedModels != null) yield break;
        Plugin.loaded = false;

        loadedModels = new Dictionary<string, Assimp.Mesh[]>();
        loadedTextures = new Dictionary<string, Texture2D>();
        loadedTexturesRaw = new Dictionary<string, Il2CppStructArray<byte>>();
        loadedAudio = new Dictionary<string, AudioClip>();
        loadedAudioData = new Dictionary<string, byte[]>();

        loadedTextureInstanceIds = new List<int>();
        loadedAudioInstanceIds = new List<int>();

        PluginInfo.Instance.Logger.LogInfo($"Processor count : {Environment.ProcessorCount}");
        var stopwatch = Stopwatch.StartNew();
        float frameStartTime = Time.realtimeSinceStartup;

        var audioFiles = GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "ogg");
        foreach (var file in audioFiles)
        {
            // Load audio files
            AudioClip audioFile = null;
            string filename = Path.GetRelativePath(PluginInfo.AssetsFolder, file);
            filename = Path.ChangeExtension(filename, null);
            // use asset path location for FullPath parameter, for example if the files is found in Assets/Audio/AudioFile.ogg, the FullPath should be "Audio\AudioFile"
            yield return LoadAudioCoroutine(Path.GetFileNameWithoutExtension(file), filename, File.OpenRead(file), clip => audioFile = clip);
            audioFile.hideFlags = HideFlags.DontSave;


            if (!loadedAudio.ContainsKey(filename))
            {
                loadedAudio.Add(filename, audioFile);
                loadedAudioInstanceIds.Add(audioFile.GetInstanceID());
                PluginInfo.Instance.Logger.LogInfo($"Loaded audio from file: '{filename}'");
            }
        }
        PluginInfo.Instance.Logger.LogInfo($"Loaded all audio in {stopwatch.ElapsedMilliseconds}ms");

        // Load model files
        stopwatch.Restart();
        var modelFiles = GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "fbx");
        var loadedModelsLocal = new ConcurrentDictionary<string, Assimp.Mesh[]>();

        int maxParallelism = Math.Max(Environment.ProcessorCount - 1, 1);

        foreach (var fileBatch in SplitIntoBatches(modelFiles, maxParallelism))
        {
            Parallel.ForEach(fileBatch, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, file =>
            {
                var meshes = LoadFBX(file);
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
        var textureFiles = GetAllFilesWithExtensions(PluginInfo.AssetsFolder, "png", "jpg", "jpeg");
        foreach (var file in textureFiles)
        {
            var texture = LoadTexture(file, out var rawData);
            if (texture != null)
            {
                string filename = Path.GetRelativePath(PluginInfo.AssetsFolder, file);
                filename = Path.ChangeExtension(filename, null);
                if (!loadedTextures.ContainsKey(filename))
                {
                    loadedTextures.Add(filename, texture);
                    loadedTextureInstanceIds.Add(texture.GetInstanceID());
                    var compressedData = LZ4Pickler.Pickle(rawData); // Compress rawData using LZ4
                    loadedTexturesRaw.Add(filename, compressedData);
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
        Plugin.loaded = true;
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
    public static Texture2D LoadTexture(string file, out Il2CppStructArray<byte> rawData) => LoadTexture(Path.GetFileNameWithoutExtension(file), File.OpenRead(file), out rawData);
    public static Texture2D LoadTexture(string name, Stream stream, out Il2CppStructArray<byte> rawData)
    {
        Texture2D texture = new Texture2D(2, 2);
        rawData = ReadStream(stream);
        var result = Reflection.ForceUseStaticMethod<bool>(typeof(ImageConversion), "LoadImage", texture, rawData);
        if (!result) return null;
        texture.name = name;
        texture.hideFlags = HideFlags.DontSave;
        return texture;
    }


    public static System.Collections.IEnumerator LoadAudioCoroutine(string name, string fullpath, Stream stream, Action<AudioClip> onAudioLoaded)
    {
        float[] audioData = null;

        using (var vorbis = new NVorbis.VorbisReader(new MemoryStream(ReadStream(stream), false)))
        {
            audioData = new float[vorbis.Channels * vorbis.TotalSamples];
            int chunkSize = 4096 * 32; // Read in small chunks (e.g., 1024 samples at a time)
            int totalSamplesRead = 0;

            // Read the audio in chunks, simulating async loading in parts
            while (totalSamplesRead < audioData.Length)
            {
                int samplesToRead = Mathf.Min(chunkSize, audioData.Length - totalSamplesRead);
                int readSamples = vorbis.ReadSamples(audioData, totalSamplesRead, samplesToRead);
                totalSamplesRead += readSamples;

                // Yield to wait until the next frame
                yield return null;
            }
        }

        // Once the audio data is loaded, create the AudioClip on the main thread
        if (audioData != null)
        {
            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(audioData.AsSpan());

            // Compress the byteSpan using LZ4.Stream
            using (var compressedStream = new MemoryStream())
            {
                using (var lz4Stream = LZ4Stream.Encode(compressedStream))
                {
                    lz4Stream.Write(byteSpan);
                }
                loadedAudioData[fullpath] = compressedStream.ToArray();
            }

            // Create the AudioClip on the main thread
            var audio = AudioClip.Create(name, (int)(audioData.Length / 2), 2, 44100, false); // 2 channels, 44.1kHz sample rate
            AudioClip.SetData(audio, audioData, ((System.Array)audioData).Length / audio.channels, 0);
            onAudioLoaded?.Invoke(audio);
        }
        else
        {
            onAudioLoaded?.Invoke(null);
        }
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
        var fbxFile = importer.ImportFileFromStream(stream, PostProcessSteps.Triangulate |
            PostProcessSteps.FlipWindingOrder |
            PostProcessSteps.JoinIdenticalVertices);
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
                if (source.bones[i] == null)
                {
                    UnityEngine.Debug.LogError($"Bone at index {i} is null!");
                    continue;
                }

                var boneName = FixedBoneName(source.bones[i].name);

                if (!bones.ContainsKey(boneName) && i < sharedMeshBindposes.Count)
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

    void SetupBoneCloth(GameObject character, List<string> rootBonesNames)
    {

        var obj = new GameObject("BoneCloth");
        obj.transform.SetParent(character.transform, false);

        // add Magica Cloth
        var cloth = obj.AddComponent<MagicaCloth2.MagicaCloth>();
        var sdata = cloth.SerializeData;

        // bone cloth
        sdata.clothType = MagicaCloth2.ClothProcess.ClothType.BoneCloth;

        foreach (var rootBoneName in rootBonesNames)
        {
            sdata.rootBones.Add(UtilityNamespace.Utility.RecursiveFindChild(character.transform, rootBoneName).transform);
        }

        // setup parameters
        sdata.gravity = 3.0f;
        sdata.damping.SetValue(0.05f);
        sdata.angleRestorationConstraint.stiffness.SetValue(0.15f, 1.0f, 0.15f, true);
        sdata.angleRestorationConstraint.velocityAttenuation = 0.6f;
        sdata.tetherConstraint.distanceCompression = 0.5f;
        sdata.inertiaConstraint.particleSpeedLimit.SetValue(true, 3.0f);
        sdata.colliderCollisionConstraint.mode = MagicaCloth2.ColliderCollisionConstraint.Mode.None;

        // start build
        cloth.BuildAndRun();
    }

    public static void ResizeMesh(ref UnityEngine.Mesh mesh, float scale)
    {
        var vertices = mesh.vertices;
        mesh.SetVertices(vertices.Select(v => v * scale).ToArray());
    }

    public static void MoveMesh(ref UnityEngine.Mesh mesh, Vector3 offset)
    {
        var vertices = mesh.vertices;
        mesh.SetVertices(vertices.Select(v => v + offset).ToArray());
    }

    public static void RotateMesh(ref UnityEngine.Mesh mesh, Vector3 rotation)
    {
        var vertices = mesh.vertices;
        var rotationMatrix = Matrix4x4.TRS(Vector3.zero, UnityEngine.Quaternion.Euler(rotation), Vector3.one);
        mesh.SetVertices(vertices.Select(v => rotationMatrix.MultiplyPoint3x4(v)).ToArray());
    }

    struct BindPoseData
    {
        public float[] elements; // 16 elements for a 4x4 matrix
    }

    struct BoneWeightData
    {
        public int boneIndex;
        public float weight;
    }

    public static System.Collections.IEnumerator BuildMeshCoroutine(
        Assimp.Mesh fbxMesh,
        ArmatureData armature = null,
        bool addBlendShape = false,
        string blendShapeName = "Mita",
        float maxFrameTime = 1f / 240f // max time per frame in seconds
    )
    {
        blendShapeName = blendShapeName.Replace("MitaPerson ", "").Replace("MilaPerson ", "").Replace("(Clone)", "").Trim();

        float frameStartTime = Time.realtimeSinceStartup;

        // Step 1: Convert the mesh to Unity format (main thread only)
        var mesh = ConvertMeshToUnity(fbxMesh);

        if (armature == null)
        {
            yield return mesh;
            yield break;
        }

        int boneCount = armature.bones.Count;
        int bonesPerVertexLength = fbxMesh.VertexCount;

        // Step 2: Prepare bindposes and bone weights in a background thread

        var bindposesTask = Task.Run(() =>
        {
            var bindposesData = new BindPoseData[boneCount];
            // Parallel.For(0, boneCount, i =>
            for (int i = 0; i < boneCount; i++)
            {
                bindposesData[i] = new BindPoseData
                {
                    elements = new float[16] // Initialize identity matrix
                    {
                        1, 0, 0, 0,
                        0, 1, 0, 0,
                        0, 0, 1, 0,
                        0, 0, 0, 1
                    }
                };
            }

            // Parallel.ForEach(fbxMesh.Bones, bone =>
            foreach (var bone in fbxMesh.Bones)
            {
                if (!armature.bones.TryGetValue(FixedBoneName(bone.Name), out var armatureBone))
                {
                    // return;
                    continue;
                }

                int armatureBoneIndex = armatureBone.index;

                if (armatureBoneIndex >= 0 && armatureBoneIndex < boneCount)
                {
                    var bindpose = armatureBone.bindpose;
                    bindposesData[armatureBoneIndex] = new BindPoseData
                    {
                        elements = new float[16]
                        {
                            bindpose.m00, bindpose.m01, bindpose.m02, bindpose.m03,
                            bindpose.m10, bindpose.m11, bindpose.m12, bindpose.m13,
                            bindpose.m20, bindpose.m21, bindpose.m22, bindpose.m23,
                            bindpose.m30, bindpose.m31, bindpose.m32, bindpose.m33
                        }
                    };
                }
            }

            return bindposesData;
        });



        var bonesPerVertexTask = Task.Run(() =>
        {

            var bonesPerVertex = new List<List<BoneWeightData>>(bonesPerVertexLength);
            for (int i = 0; i < bonesPerVertexLength; i++)
            {
                bonesPerVertex.Add(new List<BoneWeightData>());
            }

            // Parallel.ForEach(fbxMesh.Bones, bone =>
            foreach (var bone in fbxMesh.Bones)
            {
                if (!armature.bones.TryGetValue(FixedBoneName(bone.Name), out var armatureBone))
                {
                    // return;
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

                    bonesPerVertex[vertexID].Add(new BoneWeightData()
                    {
                        boneIndex = armatureBoneIndex,
                        weight = vertex.Weight
                    });
                }
            }

            return bonesPerVertex;
        });

        // Wait for both tasks to complete
        while (!bindposesTask.IsCompleted || !bonesPerVertexTask.IsCompleted)
        {
            yield return null;
        }

        var bindposesData = bindposesTask.Result;
        var bonesPerVertex = bonesPerVertexTask.Result;

        // Step 3: Process bindposes and bone weights on the main thread
        var bindposes = new UnityEngine.Matrix4x4[boneCount];
        for (int i = 0; i < bindposesData.Length; i++)
        {
            var data = bindposesData[i];
            bindposes[i] = new UnityEngine.Matrix4x4(
                new Vector4(data.elements[0], data.elements[4], data.elements[8], data.elements[12]),
                new Vector4(data.elements[1], data.elements[5], data.elements[9], data.elements[13]),
                new Vector4(data.elements[2], data.elements[6], data.elements[10], data.elements[14]),
                new Vector4(data.elements[3], data.elements[7], data.elements[11], data.elements[15])
            );
        }

        if ((Time.realtimeSinceStartup - frameStartTime) > maxFrameTime)
        {
            yield return null;
            frameStartTime = Time.realtimeSinceStartup;
        }

        var bonesPerVertexArray = new NativeArray<byte>(bonesPerVertexLength, Allocator.Temp);
        var weights = new List<BoneWeight1>();

        for (int i = 0; i < bonesPerVertex.Count; i++)
        {
            var boneList = bonesPerVertex[i];
            if (boneList != null && boneList.Count > 0)
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
                weights.AddRange(boneList.Select(bw => new BoneWeight1()
                {
                    boneIndex = bw.boneIndex,
                    weight = bw.weight
                }));
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

        // Apply bone weights and bindposes
        mesh.SetBoneWeights(bonesPerVertexArray, weightsArray);
        mesh.bindposes = bindposes;

        if ((Time.realtimeSinceStartup - frameStartTime) > maxFrameTime)
        {
            yield return null;
            frameStartTime = Time.realtimeSinceStartup;
        }

        foreach (var clothNode in armature.clothNodes)
        {
            clothNode.cullRendererList ??= new Il2CppSystem.Collections.Generic.List<Renderer> { };
            clothNode.cullRendererList.Add(armature.source);
        }

        // Step 4: Process blendshapes if necessary
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

                var potentialMatches = blendShapeOrders.Keys
                    .Where(key => blendShapeName.Contains(key) || key.Contains(blendShapeName))
                    .ToList();

                if (potentialMatches.Count > 0)
                {
                    string closestMatch = potentialMatches.First();
                    blendShapeOrder = blendShapeOrders[closestMatch];
                    UnityEngine.Debug.LogWarning($"[WARNING] No exact blendshape order found for {blendShapeName}, using closest match {closestMatch} order");
                }
                else
                {
                    blendShapeOrder = blendShapeOrders["Mita"];
                    UnityEngine.Debug.LogWarning($"[WARNING] No blendshape order found for {blendShapeName}, using default Mita order");
                }
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


            if ((Time.realtimeSinceStartup - frameStartTime) > maxFrameTime)
            {
                yield return null;
                frameStartTime = Time.realtimeSinceStartup;
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


                if ((Time.realtimeSinceStartup - frameStartTime) > maxFrameTime)
                {
                    yield return null;
                    frameStartTime = Time.realtimeSinceStartup;
                }
            }
            UnityEngine.Debug.Log($"[INFO] Blendshapes loaded");
        }

        // Final recalculations
        mesh.RecalculateBoundsImpl(MeshUpdateFlags.Default);
        yield return mesh;
    }

    public static UnityEngine.Mesh BuildMesh(Assimp.Mesh fbxMesh, ArmatureData armature = null, bool addBlendShape = false, string blendShapeName = "Mita")
    {
        blendShapeName = blendShapeName.Replace("MitaPerson ", "").Replace("MilaPerson ", "").Replace("(Clone)", "").Trim();

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

                var potentialMatches = blendShapeOrders.Keys
                    .Where(key => blendShapeName.Contains(key) || key.Contains(blendShapeName))
                    .ToList();

                if (potentialMatches.Count > 0)
                {
                    string closestMatch = potentialMatches.First();
                    blendShapeOrder = blendShapeOrders[closestMatch];
                    UnityEngine.Debug.LogWarning($"[WARNING] No exact blendshape order found for {blendShapeName}, using closest match {closestMatch} order");
                }
                else
                {
                    blendShapeOrder = blendShapeOrders["Mita"];
                    UnityEngine.Debug.LogWarning($"[WARNING] No blendshape order found for {blendShapeName}, using default Mita order");
                }
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
            UnityEngine.Debug.Log($"[INFO] Blendshapes loaded");
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
