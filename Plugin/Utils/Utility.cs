using BepInEx.Unity.IL2CPP.Utils;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;

namespace UtilityNamespace
{
    public static class Utility
    {

        public static Transform RecursiveFindChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName) return child;
                else
                {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null) return found;
                }
            }
            return null;
        }

        public static byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] compressedData)
        {
            using (var input = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        public static bool ReplaceSpriteInReferences(Sprite oldSprite, Sprite newSprite)
        {
            if (oldSprite == null || newSprite == null)
            {
                // Debug.LogError("Old Sprite or New Sprite is not assigned!");
                return false;
            }

            bool replacementSucceeded = false;

            // Replace references in all SpriteRenderer components
            foreach (SpriteRenderer renderer in Reflection.FindObjectsOfType<SpriteRenderer>(false))
            {
                if (!renderer.gameObject.activeSelf) continue;
                if (renderer.sprite != newSprite && renderer.sprite == oldSprite && renderer.sprite.name == oldSprite.name)
                {
                    renderer.sprite = newSprite;
                    Debug.Log($"Replaced sprite in SpriteRenderer on GameObject: {renderer.gameObject.name}");
                    replacementSucceeded = true;
                }
            }

            // Replace references in all UI Image components
            foreach (Image image in Reflection.FindObjectsOfType<Image>(false))
            {
                if (!image.gameObject.activeSelf) continue;
                if (image.activeSprite != newSprite && image.sprite == oldSprite && image.sprite.name == oldSprite.name)
                {
                    image.m_OverrideSprite = newSprite;
                    image.gameObject.SetActive(false);
                    image.gameObject.SetActive(true);
                    Debug.Log($"Replaced sprite in Image on GameObject: {image.gameObject.name}");
                    replacementSucceeded = true;
                }
            }

            return replacementSucceeded;
        }
    }




    public static class LateCallUtility
    {
        public class CoroutineHandler : MonoBehaviour { }

        private static CoroutineHandler _handler;

        // Ensures the CoroutineHandler exists
        public static CoroutineHandler Handler
        {
            get
            {
                if (_handler == null)
                {
                    var obj = new GameObject("LateCallHandler");
                    _handler = obj.AddComponent<CoroutineHandler>();
                    GameObject.DontDestroyOnLoad(obj); // Prevent destruction across scenes
                    obj.hideFlags = HideFlags.HideAndDontSave; // Hide from hierarchy
                }
                return _handler;
            }
        }

        // Queue for storing methods and delays
        public static readonly Queue<(Action method, float delay)> MethodQueue = new();

        // Static method to add a method to the queue with a delay
        public static void LateCall(Action methodToCall, float delay = 0.5f)
        {
            if (methodToCall == null) throw new ArgumentNullException(nameof(methodToCall));

            // Add method and delay to the queue
            MethodQueue.Enqueue((methodToCall, delay));

            // Start processing the queue if not already running
            if (MethodQueue.Count == 1)
            {
                Handler.StartCoroutine(ProcessQueue());
            }
        }

        // Coroutine to process the queue
        public static System.Collections.IEnumerator ProcessQueue()
        {
            while (MethodQueue.Count > 0)
            {
                var (method, delay) = MethodQueue.Dequeue();
                yield return new WaitForSeconds(delay);

                // Invoke the method
                method?.Invoke();
            }
        }
    }


}
