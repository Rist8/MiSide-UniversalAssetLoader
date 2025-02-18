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
using UtilityNamespace;


public static class RuntimeMemberAccessor
{
    public static void SetRuntimeMember(GameObject gameObject,
        string typeName,
        string memberName,
        string Inputvalue)
    {

        // find hardcoded types first
        var Component = UtilityNamespace.Utility.FindGameObjectComponentType(gameObject, typeName);
        if (Component != null)
        {
            var property = Component.GetType().GetProperty(memberName);
            if (property == null)
            {
                Console.WriteLine($"[ERROR] Property {memberName} not found in {typeName}");
                return;
            }
            var convertedValue = Convert.ChangeType(Inputvalue, property.PropertyType);
            property.SetValue(Component, convertedValue);

            Console.WriteLine($"[INFO] Set member {memberName} of type {typeName} to {Inputvalue}");
            return;
        }

        // if not found, use invoke member to set the value, but Il2Cpp doesn't support all members, so it may fail
        try
        {
            Il2CppSystem.Object value = UtilityNamespace.Utility.ParseStringToValue(Inputvalue);

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

            Console.WriteLine($"[INFO] Set member {memberName} of type {typeName} to {value}");

        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR] Failed to set member {memberName} of type {typeName} to {Inputvalue}");
            Console.WriteLine(e);
        }
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
