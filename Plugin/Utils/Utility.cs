using BepInEx.Unity.IL2CPP.Utils;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

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

    public class ConfigParser
    {
        private static readonly Regex entryPattern = new Regex(
            "^(\\w+)\\s*=\\s*(.+)$",
            RegexOptions.Compiled);

        private static readonly Regex vector3Pattern = new Regex(
            "^Vector3\\((-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?\\)$",
            RegexOptions.Compiled);

        private static readonly Regex vector2Pattern = new Regex(
            "^Vector2\\((-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?\\)$",
            RegexOptions.Compiled);

        private static readonly Regex vector4Pattern = new Regex(
            "^Vector4\\((-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?\\)$",
            RegexOptions.Compiled);

        private static readonly Regex colorPattern = new Regex(
            "^Color\\((-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?;\\s*(-?\\d*\\.?\\d+)[fF]?\\)$",
            RegexOptions.Compiled);

        public static object ParseValue(string value)
        {
            if (int.TryParse(value, out int intValue)) return intValue;
            if (float.TryParse(value.TrimEnd('f', 'F'), NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue)) return floatValue;
            if (bool.TryParse(value, out bool boolValue)) return boolValue;
            if (value.StartsWith("\"") && value.EndsWith("\"")) return value.Trim('"');

            var vector3Match = vector3Pattern.Match(value);
            if (vector3Match.Success)
            {
                return new Vector3(
                    float.Parse(vector3Match.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(vector3Match.Groups[2].Value, CultureInfo.InvariantCulture),
                    float.Parse(vector3Match.Groups[3].Value, CultureInfo.InvariantCulture));
            }

            var vector2Match = vector2Pattern.Match(value);
            if (vector2Match.Success)
            {
                return new Vector2(
                    float.Parse(vector2Match.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(vector2Match.Groups[2].Value, CultureInfo.InvariantCulture));
            }

            var vector4Match = vector4Pattern.Match(value);
            if (vector4Match.Success)
            {
                return new Vector4(
                    float.Parse(vector4Match.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(vector4Match.Groups[2].Value, CultureInfo.InvariantCulture),
                    float.Parse(vector4Match.Groups[3].Value, CultureInfo.InvariantCulture),
                    float.Parse(vector4Match.Groups[4].Value, CultureInfo.InvariantCulture));
            }

            var colorMatch = colorPattern.Match(value);
            if (colorMatch.Success)
            {
                return new Color(
                    float.Parse(colorMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(colorMatch.Groups[2].Value, CultureInfo.InvariantCulture),
                    float.Parse(colorMatch.Groups[3].Value, CultureInfo.InvariantCulture),
                    float.Parse(colorMatch.Groups[4].Value, CultureInfo.InvariantCulture));
            }

            return value;
        }
    }

}
