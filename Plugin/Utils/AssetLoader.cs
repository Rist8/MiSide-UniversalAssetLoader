using Assimp;
using Assimp.Configs;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Unity.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MagicaCloth;

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

        foreach ( var material in fbxFile.Materials )
        {
            Debug.Log( $"Material: {material.Name}, {material.ColorDiffuse}, {material.TextureDiffuse.FilePath}");
        }

        return fbxFile.Meshes.ToArray();
    }

    public class ArmatureData
    {
        public Dictionary<string, (int index, Matrix4x4 bindpose)> bones;
        public List<MagicaBoneCloth> clothNodes;
        public SkinnedMeshRenderer source;

        public ArmatureData(SkinnedMeshRenderer source)
        {
            this.source = source;
            bones = new Dictionary<string, (int index, Matrix4x4 bindpose)>();
            clothNodes = new List<MagicaBoneCloth>();

            for (int i = 0; i < source.bones.Length; i++)
            {
                bones[FixedBoneName(source.bones[i].name)] = (i, source.sharedMesh.bindposes[i]);
            }

            var allBones = new List<Transform>();
            CollectBonesRecursively(source.rootBone.transform, allBones);

            foreach (var bone in allBones)
            {
                if (bone.TryGetComponent<MagicaBoneCloth>(out var boneCloth))
                {
                    clothNodes.Add(boneCloth);
                }
            }
        }

        private void CollectBonesRecursively(Transform current, List<Transform> boneList)
        {
            if (current == null || !current.gameObject.activeSelf || current.name.Contains("Collider"))
            {
                return; // Skip inactive bones
            }

            boneList.Add(current);

            for (int i = 0; i < current.childCount; ++i)
            {
                CollectBonesRecursively(current.GetChild(i), boneList);
            }
        }
    }

    public static UnityEngine.Mesh BuildMesh(Assimp.Mesh fbxMesh, ArmatureData armature = null)
    {
        var mesh = new UnityEngine.Mesh()
        {
            indexFormat = (fbxMesh.VertexCount > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
        };

        mesh.name = fbxMesh.Name;

        var vertices = fbxMesh.Vertices.Select(vertex => new Vector3(-vertex.X, vertex.Y, vertex.Z)).ToArray();
        mesh.SetVertices(vertices);

        if (fbxMesh.Normals.Count == fbxMesh.VertexCount)
        {
            mesh.SetNormals(fbxMesh.Normals.Select(normal => new Vector3(-normal.X, normal.Y, normal.Z)).ToArray());
        }

        if (fbxMesh.TextureCoordinateChannelCount >= 1 && fbxMesh.TextureCoordinateChannels[0].Count == fbxMesh.VertexCount)
        {
            mesh.SetUVs(0, fbxMesh.TextureCoordinateChannels[0].Select(uv => new Vector2(uv.X, uv.Y)).ToArray());
        }

        if (fbxMesh.TextureCoordinateChannelCount >= 2 && fbxMesh.TextureCoordinateChannels[1].Count == fbxMesh.VertexCount)
        {
            mesh.SetUVs(1, fbxMesh.TextureCoordinateChannels[1].Select(uv => new Vector2(uv.X, uv.Y)).ToArray());
        }

        if (fbxMesh.Tangents.Count == fbxMesh.VertexCount)
        {
            mesh.SetTangents(fbxMesh.Tangents.Select(tangent => new Vector4(tangent.X, tangent.Y, tangent.Z)).ToArray());
        }

        if (armature != null)
        {
            var bonesPerVertex = new List<BoneWeight1>[vertices.Length];
            var bindposes = new List<Matrix4x4>(armature.bones.Count);

            for (int i = 0; i < armature.bones.Count; i++)
            {
                bindposes.Add(Matrix4x4.identity);
            }

            foreach (var bone in fbxMesh.Bones)
            {
                if (!armature.bones.TryGetValue(FixedBoneName(bone.Name), out var armatureBone))
                {
                    continue;
                }

                foreach (var vertex in bone.VertexWeights)
                {
                    if (vertex.VertexID >= bonesPerVertex.Length)
                    {
                        continue;
                    }

                    if (bonesPerVertex[vertex.VertexID] == null)
                    {
                        bonesPerVertex[vertex.VertexID] = new List<BoneWeight1>();
                    }

                    bonesPerVertex[vertex.VertexID].Add(new BoneWeight1()
                    {
                        boneIndex = armatureBone.index,
                        weight = vertex.Weight
                    });
                }

                for (int i = 0; i < bonesPerVertex.Length; i++)
                {
                    if (bonesPerVertex[i] == null)
                    {
                        continue;
                    }

                    bonesPerVertex[i].Sort((a, b) => b.weight.CompareTo(a.weight));

                    var oldWeights = bonesPerVertex[i];

                    bonesPerVertex[i] = new List<BoneWeight1>();
                    bonesPerVertex[i].AddRange(oldWeights.GetRange(0, Math.Min(oldWeights.Count, 4)));
                }

                if (armatureBone.index >= 0 && armatureBone.index < bindposes.Count)
                {
                    bindposes[armatureBone.index] = armatureBone.bindpose;
                }
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
            {
                weightsArray[i] = weights[i];
            }

            mesh.SetBoneWeights(bonesPerVertexArray, weightsArray);
            mesh.bindposes = bindposes.ToArray();

            foreach (var clothNode in armature.clothNodes)
            {
                clothNode.cullRendererList = new Il2CppSystem.Collections.Generic.List<Renderer> { };
                clothNode.cullRendererList.Add(armature.source);
            }
        }

        mesh.triangles = fbxMesh.GetIndices();
        mesh.RecalculateBounds();



        return mesh;
    }

    static string FixedBoneName(string name) => name.Replace(" ", "_").Replace(".", "_");

    public static byte[] ReadStream(Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    /*public static string[] GetAllFilesWithExtensions(string directory, params string[] extensions)
    {
        return extensions
            .SelectMany(extension => Directory.GetFiles(directory, "*." + extension, SearchOption.AllDirectories))
            .ToArray();
    }*/
   
}