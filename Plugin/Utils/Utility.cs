using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine;

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
    }



    public static class LateCallUtility
    {
        public class CoroutineHandler : MonoBehaviour { }

        public static CoroutineHandler _handler;

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
