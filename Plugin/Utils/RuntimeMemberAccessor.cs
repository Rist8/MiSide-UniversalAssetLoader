using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Globalization;
using Il2CppSystem.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class RuntimeMemberAccessor
{
    public static void SetRuntimeMember(GameObject gameObject,
        string typeName,
        string memberName,
        Il2CppSystem.Object value)
    {

        Type componentType = Type.GetType(typeName);

        if (componentType == null)
        {
            Console.WriteLine("Type not found");
            return;
        }

        Console.WriteLine($"Type found: {componentType}");

        Component componentInstance = gameObject.GetComponent(Il2CppType.From(componentType));

        Il2CppType.From(componentType).InvokeMember(memberName,
            BindingFlags.SetField | BindingFlags.SetProperty,
            null,
            componentInstance,
            (Il2CppReferenceArray<Il2CppSystem.Object>)(new Il2CppSystem.Object[] { value }),
            null,
            CultureInfo.CurrentCulture,
            null);
    }

    public static Il2CppSystem.Object GetRuntimeMember(GameObject gameObject,
        string typeName,
        string memberName)
    {

        Type componentType = Type.GetType(typeName);

        if (componentType == null)
        {
            Console.WriteLine("Type not found");
            return null;
        }

        Console.WriteLine($"Type found: {componentType}");

        Component componentInstance = gameObject.GetComponent(Il2CppType.From(componentType));

        return Il2CppType.From(componentType).InvokeMember(memberName,
            BindingFlags.GetField | BindingFlags.GetProperty,
            null,
            componentInstance,
            (Il2CppReferenceArray<Il2CppSystem.Object>)(new Il2CppSystem.Object[] { }),
            null,
            CultureInfo.CurrentCulture,
            null);
    }
}
