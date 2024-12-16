using Assimp;
using Assimp.Configs;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Unity.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

public class AssetLoader
{
    public static Texture2D LoadTexture(string file) => LoadTexture(Path.GetFileNameWithoutExtension(file), File.OpenRead(file));
    public static Texture2D LoadTexture(string name, Stream stream)
    {
        Texture2D texture = new Texture2D(2, 2);
        var result = Reflection.ForceUseStaticMethod<bool>(typeof(ImageConversion), "LoadImage", texture, (Il2CppStructArray<byte>) ReadStream(stream));
        if (!result) return null;

        texture.name = name;
        texture.hideFlags = HideFlags.DontSave;
        return texture;
    }

    public static AudioClip LoadAudio(string file) => LoadAudio(Path.GetFileNameWithoutExtension(file), File.OpenRead(file));
    public static AudioClip LoadAudio(string name, Stream stream)
    {
        AudioClip audioClip = null;
        using (var vorbis = new NVorbis.VorbisReader( new MemoryStream( ReadStream(stream), false ) ) )
        {
            float[] _audioBuffer = new float[vorbis.TotalSamples]; // Just dump everything
            int read = vorbis.ReadSamples( _audioBuffer, 0, (int)vorbis.TotalSamples );
            audioClip = AudioClip.Create(name, (int)(vorbis.TotalSamples / vorbis.Channels), vorbis.Channels, vorbis.SampleRate, false);
            AudioClip.SetData(audioClip, _audioBuffer, ((System.Array)_audioBuffer).Length / audioClip.channels, 0);
        }
        if (audioClip == null) return null;

        audioClip.hideFlags = HideFlags.DontSave;
        return audioClip;
    }

    public static Assimp.Mesh[] LoadFBX(string file) => LoadFBX(File.OpenRead(file));
    public static Assimp.Mesh[] LoadFBX(Stream stream)
    {
        AssimpContext importer = new AssimpContext();
        importer.SetConfig(new NormalSmoothingAngleConfig(66.0f));
        var fbxFile = importer.ImportFileFromStream(stream, PostProcessSteps.Triangulate | PostProcessSteps.FlipWindingOrder);
        return fbxFile.Meshes.ToArray();
    }

    public class ArmatureData : Dictionary<string, (int index, Matrix4x4 bindpose)>
    {
        public ArmatureData(SkinnedMeshRenderer source)
        {
            // Traverse the full hierarchy starting from the root bone
            var allBones = new List<Transform>();
            CollectBonesRecursively(source.rootBone.transform, allBones);

            int nextIndex = source.bones.Length; // Start indexing additional bones after source.bones

            // Map each bone to its index and bindpose if it exists in SkinnedMeshRenderer
            for (int i = 0; i < source.bones.Length; i++)
            {
                string boneName = FixedBoneName(source.bones[i].name);
                Matrix4x4 bindPose = source.sharedMesh.bindposes[i];
                this[boneName] = (i, bindPose);
                Debug.Log("Bone name (active SkinnedMeshRenderer bone): " + boneName);
            }

            // Ensure additional bones are also included (like MagicaBoneCloth)
            foreach (var bone in allBones)
            {
                string boneName = FixedBoneName(bone.name);
                if (!this.ContainsKey(boneName))
                {
                    // Assign a valid index and default bindpose for additional bones
                    this[boneName] = (nextIndex, Matrix4x4.identity);
                    nextIndex++;
                    Debug.Log("Bone name (active in hierarchy): " + boneName);
                }
            }
            Debug.Log(nextIndex);
        }

        private void CollectBonesRecursively(Transform current, List<Transform> boneList)
        {
            if (current == null || !current.gameObject.activeSelf || current.name.Contains("Collider")) return; // Skip inactive bones

            Debug.Log("Adding active bone: " + current.name);
            // Add current bone to the list
            boneList.Add(current);

            // Recursively collect child bones
            for (int i = 0; i < current.childCount; ++i)
            {
                CollectBonesRecursively(current.GetChild(i), boneList);
            }
        }
        public ArmatureData(GameObject source)
        {
            foreach (var mr in source.GetComponentsInChildren<SkinnedMeshRenderer>())
                for (int i = 0; i < mr.bones.Length; i++) 
                    this[FixedBoneName(mr.bones[i].name)] = (i, mr.sharedMesh.bindposes[i]);
        }
    }

    public static UnityEngine.Mesh BuildMesh(Assimp.Mesh fbxMesh, ArmatureData armature = null)
    {
        var mesh = new UnityEngine.Mesh()
        {
            indexFormat = (fbxMesh.VertexCount > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
        };

        mesh.name = fbxMesh.Name;

        // Set vertices
        var vertices = fbxMesh.Vertices.Select(vertex => new Vector3(-vertex.X, vertex.Y, vertex.Z)).ToArray();
        mesh.SetVertices(vertices);

        // Set normals
        if (fbxMesh.Normals.Count == fbxMesh.VertexCount)
            mesh.SetNormals(fbxMesh.Normals.Select(normal => new Vector3(-normal.X, normal.Y, normal.Z)).ToArray());

        // Set UVs (ensure counts match)
        if (fbxMesh.TextureCoordinateChannelCount >= 1 && fbxMesh.TextureCoordinateChannels[0].Count == fbxMesh.VertexCount)
            mesh.SetUVs(0, fbxMesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToArray());

        if (fbxMesh.TextureCoordinateChannelCount >= 2 && fbxMesh.TextureCoordinateChannels[1].Count == fbxMesh.VertexCount)
            mesh.SetUVs(1, fbxMesh.TextureCoordinateChannels[1].Select(uv => new Vector2(uv.X, uv.Y)).ToArray());

        // Set tangents
        if (fbxMesh.Tangents.Count == fbxMesh.VertexCount)
            mesh.SetTangents(fbxMesh.Tangents.Select(tangent => new Vector4(tangent.X, tangent.Y, tangent.Z)).ToArray());

        // Handle bones and weights
        if (armature != null)
        {
            var bonesPerVertex = new List<BoneWeight1>[vertices.Length];
            var bindposes = new List<Matrix4x4>(armature.Count);

            for (int i = 0; i < armature.Count; i++)
                bindposes.Add(Matrix4x4.identity);

            foreach (var bone in fbxMesh.Bones)
            {
                if (!armature.TryGetValue(FixedBoneName(bone.Name), out var armatureBone)) continue;

                foreach (var vertex in bone.VertexWeights)
                {
                    if (vertex.VertexID >= bonesPerVertex.Length) continue;

                    if (bonesPerVertex[vertex.VertexID] == null)
                        bonesPerVertex[vertex.VertexID] = new List<BoneWeight1>();

                    bonesPerVertex[vertex.VertexID].Add(new BoneWeight1()
                    {
                        boneIndex = armatureBone.index,
                        weight = vertex.Weight
                    });
                }

                if (armatureBone.index >= 0 && armatureBone.index < bindposes.Count)
                    bindposes[armatureBone.index] = armatureBone.bindpose;
            }

            var bonesPerVertexArray = new NativeArray<byte>(bonesPerVertex.Length, Allocator.Temp);
            List<BoneWeight1> weights = new List<BoneWeight1>();

            for (int i = 0; i < bonesPerVertex.Length; i++)
            {
                if (bonesPerVertex[i] != null)
                {
                    weights.AddRange(bonesPerVertex[i]);
                    bonesPerVertexArray[i] = (byte)bonesPerVertex[i].Count;
                }
                else
                {
                    weights.Add(new BoneWeight1() { boneIndex = 0, weight = 0 });
                    bonesPerVertexArray[i] = 1;
                }
            }

            var weightsArray = new NativeArray<BoneWeight1>(weights.Count, Allocator.Temp);
            for (int i = 0; i < weights.Count; i++)
                weightsArray[i] = weights[i];

            mesh.SetBoneWeights(bonesPerVertexArray, weightsArray);
            mesh.bindposes = bindposes.ToArray();
        }

        // Set triangles
        mesh.triangles = fbxMesh.GetIndices();

        // Recalculate bounds
        mesh.RecalculateBounds();

        return mesh;
    }



    static string FixedBoneName(string name) => name.Replace(" ", "_").Replace(".", "_");

    public static void CalculateBoneWeights(UnityEngine.Mesh sourceMesh, UnityEngine.Mesh targetMesh)
    {
        Vector3[] sourceVertices = sourceMesh.vertices;
        Vector3[] targetVertices = targetMesh.vertices;
        BoneWeight[] sourceBoneWeights = sourceMesh.boneWeights;
        BoneWeight[] targetBoneWeights = new BoneWeight[targetVertices.Length];
        List<(int key, float val)> minDistances = new List<(int key, float val)>();
        int searchLimit = 10;

        for (int i = 0; i < targetVertices.Length; i++)
        {
            Vector3 currentVertex = targetVertices[i];
            minDistances.Clear();

            for (int j = 0; j < sourceVertices.Length; j++)
            {
                float distance = Vector3.Distance(currentVertex, sourceVertices[j]);
                for (int k = 0; k < minDistances.Count; k++)
                    if (distance < minDistances[k].val)
                    {
                        minDistances.Insert(k, (j, distance));
                        while (minDistances.Count > searchLimit)
                            minDistances.RemoveAt(minDistances.Count - 1);
                    }
                if (minDistances.Count < searchLimit)
                    minDistances.Add((j, distance));
            }

            Dictionary<int, float> bonesAffecting = new Dictionary<int, float>();
            for (int l = 0; l < minDistances.Count; l++)
            {
                BoneWeight closestBoneWeight = sourceBoneWeights[minDistances[l].key];
                if (!bonesAffecting.ContainsKey(closestBoneWeight.boneIndex0)) bonesAffecting[closestBoneWeight.boneIndex0] = 0;
                if (!bonesAffecting.ContainsKey(closestBoneWeight.boneIndex1)) bonesAffecting[closestBoneWeight.boneIndex1] = 0;
                if (!bonesAffecting.ContainsKey(closestBoneWeight.boneIndex2)) bonesAffecting[closestBoneWeight.boneIndex2] = 0;
                if (!bonesAffecting.ContainsKey(closestBoneWeight.boneIndex3)) bonesAffecting[closestBoneWeight.boneIndex3] = 0;
                bonesAffecting[closestBoneWeight.boneIndex0] += closestBoneWeight.weight0;
                bonesAffecting[closestBoneWeight.boneIndex1] += closestBoneWeight.weight1;
                bonesAffecting[closestBoneWeight.boneIndex2] += closestBoneWeight.weight2;
                bonesAffecting[closestBoneWeight.boneIndex3] += closestBoneWeight.weight3;
            }
            List<(int key, float val)> sortedBones = new List<(int key, float val)>();
            foreach (var pair in bonesAffecting)
                sortedBones.Add((pair.Key, pair.Value));
            sortedBones = sortedBones.OrderByDescending(x => x.val).Take(4).ToList();
            while (sortedBones.Count < 4) sortedBones.Add((0,0));
            float sum = sortedBones.Select(item => item.val).Sum();

            BoneWeight averageBoneWeight = new BoneWeight();

            averageBoneWeight.boneIndex0 = sortedBones[0].key;
            averageBoneWeight.boneIndex1 = sortedBones[1].key;
            averageBoneWeight.boneIndex2 = sortedBones[2].key;
            averageBoneWeight.boneIndex3 = sortedBones[3].key;
            averageBoneWeight.weight0 = sortedBones[0].val / sum;
            averageBoneWeight.weight1 = sortedBones[1].val / sum;
            averageBoneWeight.weight2 = sortedBones[2].val / sum;
            averageBoneWeight.weight3 = sortedBones[3].val / sum;

            // Assign the average bone weights to the current vertex in targetMesh
            targetBoneWeights[i] = averageBoneWeight;
        }

        // Apply the changes to the second mesh
        targetMesh.boneWeights = targetBoneWeights;
    }

    public static Dictionary<string, object> ParseYAML(string text)
    {
        var lines = text.Split('\n');
        var result = new Dictionary<string, object>();
        var stack = new Stack<(int level, object container)>();
		string prevKey = null;

        stack.Push((0, result));

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
            var indent = line.TakeWhile(c => c == ' ').Count() / 2;
			string key = null, value = null;
			if (line.Contains(':'))
			{
				key = line.Substring(0, line.IndexOf(':')).Trim();
				value = line.Substring(line.IndexOf(':') + 1).Trim();
				if (string.IsNullOrWhiteSpace(value)) value = null;
			}
			else
				key = line.Trim();

			while (stack.Count > 0 && stack.Peek().level > indent) stack.Pop();

            if (key.StartsWith("-"))
            {
				if (!(stack.Peek().container is List<object>))
				{
                	var array = new List<object>();
                	(stack.Peek().container as Dictionary<string, object>)[prevKey] = array;
					stack.Push((indent, array));
				}
				
				var dict = new Dictionary<string, object>();
				(stack.Peek().container as List<object>).Add(dict);
                stack.Push((indent + 1, dict));
				key = key.Remove(0, 1).TrimStart();
            }
			else if (stack.Peek().container is List<object>)
				stack.Pop();
			
			(stack.Peek().container as Dictionary<string, object>)[key] = value;

			prevKey = key;
        }

        return result;
    }

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
        return extensions.SelectMany(extension => Directory.GetFiles(directory, "*." + extension, SearchOption.TopDirectoryOnly)).ToArray();
    }
}