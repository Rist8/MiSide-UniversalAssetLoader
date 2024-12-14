using System.Collections;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Networking;

public class Reflection
{
    protected static object? ForceUseMethod(Type type, string name, UnityEngine.Object context, params object[] parameters)
    {
        //PluginInfo.Instance.Logger.LogInfo($"Attempt to call {type.Name}:{name}");
		foreach (var method in type.GetMethods())
		{
			if (method.Name == name && method.GetParameters().Length == parameters.Length)
			{
                for (int i = 0; i < parameters.Length; i++)
                    if (parameters[i] is Type)
                        parameters[i] = Il2CppInterop.Runtime.Il2CppType.From((Type) parameters[i]);

				var result = method.Invoke(context, parameters);
                return result;
			}
		}
        return null;
    }

    public static TResultType ForceUseStaticMethod<TResultType>(Type type, string name, params object[] parameters)
    {
        var result = ForceUseMethod(type, name, null, parameters);
        try {
            return (TResultType) result;
        }
        catch (Exception e)
        {
            PluginInfo.Instance.Logger.LogError("Cannot convert " + result.GetType() + " to " + typeof(TResultType));
            return default(TResultType);
        }
    }

    public static void ForceUseStaticMethod(Type type, string name, params object[] parameters) =>
        ForceUseMethod(type, name, null, parameters);

    public static TResultType ForceUseMethod<TResultType>(UnityEngine.Object context, string name, params object[] parameters) =>
        (TResultType) ForceUseMethod(context.GetType(), name, context, parameters);

    public static void ForceUseMethod(UnityEngine.Object context, string name, params object[] parameters) =>
        ForceUseMethod(context.GetType(), name, context, parameters);

    public static T GetComponent<T>(GameObject obj)
        where T : Il2CppSystem.Object
    {
        foreach (var comp in obj.GetComponents<Component>())
            if (comp.GetIl2CppType().Name == typeof(T).Name) return comp.Cast<T>();
        return default(T);
    }

    public static T GetComponent<T>(Component obj)
        where T : Il2CppSystem.Object
        => GetComponent<T>(obj.gameObject);

    public static T[] GetComponentsInChildren<T>(GameObject obj, bool includeInactive = false)
        where T : Il2CppSystem.Object
    {
        return ForceUseMethod<Il2CppReferenceArray<UnityEngine.Component>>(
			obj,
			"GetComponentsInChildren",
			typeof(T),
			includeInactive
		).Select(obj => obj.Cast<T>()).ToArray();
    }

    public static T[] GetComponentsInChildren<T>(Component obj, bool includeInactive = false) 
        where T : Il2CppSystem.Object
        => GetComponentsInChildren<T>(obj.gameObject, includeInactive);

    public static T[] FindObjectsOfType<T>(bool includeInactive = false) 
        where T : Component
    {
        return ForceUseStaticMethod<Il2CppReferenceArray<UnityEngine.Object>>(
			typeof(UnityEngine.Object),
			"FindObjectsOfType",
			typeof(T),
			true
		).Select(obj => obj.Cast<T>()).ToArray();
    }

}