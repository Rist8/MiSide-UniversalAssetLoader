using Assimp;
using UnityEngine;

public static class AnimationLoader
{
    /// <summary>
    /// Function to load animations from FBX file and add them to given animation as animationClips
    /// </summary>
    /// <param name="filePath">path to FBX file containing the animations</param>
    /// <param name="animation">animation to which animationClips will be added</param>
    public static void LoadAnimationFromFBX(string filePath, UnityEngine.Animation animation)
    {
        AssimpContext importer = new AssimpContext();
        var stream = File.OpenRead(filePath);
        Scene scene = importer.ImportFileFromStream(stream, PostProcessSteps.Triangulate |
            PostProcessSteps.FlipWindingOrder |
            PostProcessSteps.JoinIdenticalVertices);

        if (scene == null || !scene.HasAnimations)
        {
            Debug.LogError("No animations found in the file!");
            return;
        }

        Debug.Log($"Found {scene.Animations.Count} animations");

        foreach (var anim in scene.Animations)
        {
            AnimationClip clip = ConvertAssimpAnimationToUnity(anim);
            ApplyAnimationClip(clip, animation);
        }
    }

    static AnimationClip ConvertAssimpAnimationToUnity(Assimp.Animation anim)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = anim.Name;
        clip.legacy = true; // Use legacy animation if necessary

        foreach (var channel in anim.NodeAnimationChannels)
        {
            string boneName = channel.NodeName;
            Debug.Log($"Processing bone: {boneName}");

            AnimationCurve posX = new AnimationCurve();
            AnimationCurve posY = new AnimationCurve();
            AnimationCurve posZ = new AnimationCurve();

            AnimationCurve rotX = new AnimationCurve();
            AnimationCurve rotY = new AnimationCurve();
            AnimationCurve rotZ = new AnimationCurve();
            AnimationCurve rotW = new AnimationCurve();

            for (int i = 0; i < channel.PositionKeys.Count; i++)
            {
                var posKey = channel.PositionKeys[i];
                var rotKey = channel.RotationKeys[i];

                float time = (float)posKey.Time;

                // Position
                posX.AddKey(time, posKey.Value.X);
                posY.AddKey(time, posKey.Value.Y);
                posZ.AddKey(time, posKey.Value.Z);

                // Rotation (Quaternion)
                rotX.AddKey(time, rotKey.Value.X);
                rotY.AddKey(time, rotKey.Value.Y);
                rotZ.AddKey(time, rotKey.Value.Z);
                rotW.AddKey(time, rotKey.Value.W);
            }

            var type = Il2CppInterop.Runtime.Il2CppType.From(typeof(Transform));
            clip.SetCurve(boneName, type, "localPosition.x", posX);
            clip.SetCurve(boneName, type, "localPosition.y", posY);
            clip.SetCurve(boneName, type, "localPosition.z", posZ);

            clip.SetCurve(boneName, type, "localRotation.x", rotX);
            clip.SetCurve(boneName, type, "localRotation.y", rotY);
            clip.SetCurve(boneName, type, "localRotation.z", rotZ);
            clip.SetCurve(boneName, type, "localRotation.w", rotW);
        }

        return clip;
    }

    static void ApplyAnimationClip(AnimationClip clip, UnityEngine.Animation anim)
    {
        anim.AddClip(clip, clip.name);
        anim.Play(clip.name);
    }
}
