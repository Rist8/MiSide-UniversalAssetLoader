using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Globalization;
using Il2CppSystem.Reflection;
using System;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeMemberAccessor
{
    public static void SetRuntimeMember(GameObject gameObject, string typeName, string memberName, object value)
    {
        Type componentType = Type.GetType(typeName);

        if (componentType == null)
        {
            Console.WriteLine("Type not found");
            return;
        }

        switch (componentType)
        {
            case Type _ when componentType == typeof(AudioSource):
                SetProperty(gameObject.GetComponent<AudioSource>(), memberName, value);
                break;
            case Type _ when componentType == typeof(Text):
                SetProperty(gameObject.GetComponent<Text>(), memberName, value);
                break;
            case Type _ when componentType == typeof(Image):
                SetProperty(gameObject.GetComponent<Image>(), memberName, value);
                break;
            case Type _ when componentType == typeof(RawImage):
                SetProperty(gameObject.GetComponent<RawImage>(), memberName, value);
                break;
            case Type _ when componentType == typeof(Button):
                SetProperty(gameObject.GetComponent<Button>(), memberName, value);
                break;
            case Type _ when componentType == typeof(Toggle):
                SetProperty(gameObject.GetComponent<Toggle>(), memberName, value);
                break;
            case Type _ when componentType == typeof(RectTransform):
                SetProperty(gameObject.GetComponent<RectTransform>(), memberName, value);
                break;
            default:
                var componentInstance = gameObject.GetComponent(Il2CppType.From(componentType));
                if (componentInstance == null)
                {
                    Debug.LogWarning($"[WARNING] Component '{componentType.FullName}' not found on '{gameObject.name}'.");
                    return;
                }

                try
                {
                    Il2CppType.From(componentType).InvokeMember(memberName,
                        BindingFlags.SetField | BindingFlags.SetProperty,
                        null,
                        componentInstance,
                        (Il2CppReferenceArray<Il2CppSystem.Object>)(new Il2CppSystem.Object[] { (Il2CppSystem.Object)value }),
                        null,
                        CultureInfo.CurrentCulture,
                        null);
                    Debug.Log($"[INFO] Set property '{memberName}' to '{value}' on '{componentType.FullName}'.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ERROR] Failed to set property '{memberName}' on '{componentType.FullName}': {ex.Message}");
                }
                break;
        }
    }

    public static object GetRuntimeMember(GameObject gameObject, string typeName, string memberName)
    {
        Type componentType = Type.GetType(typeName);

        if (componentType == null)
        {
            Console.WriteLine("Type not found");
            return null;
        }

        switch (componentType)
        {
            case Type _ when componentType == typeof(AudioSource):
                return GetProperty(gameObject.GetComponent<AudioSource>(), memberName);
            case Type _ when componentType == typeof(Text):
                return GetProperty(gameObject.GetComponent<Text>(), memberName);
            case Type _ when componentType == typeof(Image):
                return GetProperty(gameObject.GetComponent<Image>(), memberName);
            case Type _ when componentType == typeof(RawImage):
                return GetProperty(gameObject.GetComponent<RawImage>(), memberName);
            case Type _ when componentType == typeof(Button):
                return GetProperty(gameObject.GetComponent<Button>(), memberName);
            case Type _ when componentType == typeof(Toggle):
                return GetProperty(gameObject.GetComponent<Toggle>(), memberName);
            default:
                var componentInstance = gameObject.GetComponent(Il2CppType.From(componentType));
                if (componentInstance == null)
                {
                    Debug.LogWarning($"[WARNING] Component '{componentType.FullName}' not found on '{gameObject.name}'.");
                    return null;
                }

                try
                {
                    return Il2CppType.From(componentType).InvokeMember(memberName,
                        BindingFlags.GetField | BindingFlags.GetProperty,
                        null,
                        componentInstance,
                        (Il2CppReferenceArray<Il2CppSystem.Object>)(new Il2CppSystem.Object[] { }),
                        null,
                        CultureInfo.CurrentCulture,
                        null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ERROR] Failed to get property '{memberName}' on '{componentType.FullName}': {ex.Message}");
                    return null;
                }
        }
    }

    private static void SetProperty<T>(T component, string memberName, object value) where T : Component
    {
        if (component == null)
        {
            Debug.LogWarning($"[WARNING] Component '{typeof(T).FullName}' not found.");
            return;
        }
    
        var prop = typeof(T).GetProperty(memberName);
        if (prop != null && prop.CanWrite)
        {
            try
            {
                object convertedValue;
                if (prop.PropertyType == typeof(Quaternion))
                {
                    convertedValue = new Quaternion(
                        ((Vector4)value).x,
                        ((Vector4)value).y,
                        ((Vector4)value).z,
                        ((Vector4)value).w
                    );
                }
                else
                {
                    convertedValue = Convert.ChangeType(value, prop.PropertyType);
                }
    
                prop.SetValue(component, convertedValue);
                Debug.Log($"[INFO] Set property '{memberName}' to '{value}' on '{typeof(T).FullName}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR] Failed to set property '{memberName}' on '{typeof(T).FullName}': {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[WARNING] Property '{memberName}' not found or not writable on '{typeof(T).FullName}'.");
        }
    }

    private static object GetProperty<T>(T component, string memberName) where T : Component
    {
        if (component == null)
        {
            Debug.LogWarning($"[WARNING] Component '{typeof(T).FullName}' not found.");
            return null;
        }

        var prop = typeof(T).GetProperty(memberName);
        if (prop != null && prop.CanRead)
        {
            try
            {
                object value = prop.GetValue(component);
                Debug.Log($"[INFO] Retrieved property '{memberName}' from '{typeof(T).FullName}': {value}");
                return value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR] Failed to get property '{memberName}' on '{typeof(T).FullName}': {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[WARNING] Property '{memberName}' not found or not readable on '{typeof(T).FullName}'.");
        }

        return null;
    }
}