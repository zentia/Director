using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

internal class DirectorControlHelper
{
    internal static System.Type[] GetAllSubTypes(System.Type ParentType)
    {
        List<System.Type> list = new List<System.Type>();
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type[] types = new System.Type[0];
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                Debug.LogError($"Cinema Director: Could not load types from assembly "{assembly.GetName()}"
{exception.Message}
{exception.StackTrace}");
                continue;
            }
            foreach (System.Type type in types)
            {
                if (type.IsSubclassOf(ParentType))
                {
                    list.Add(type);
                }
            }
        }
        return list.ToArray();
    }

    public static string GetNameForDuplicate(Behaviour behaviour, string name)
    {
        string str = name;
        string s = Regex.Match(name, @"(\d+)$").Value;
        int result = 1;
        if (int.TryParse(s, out result))
        {
            result++;
            return (name.Substring(0, name.Length - s.Length) + result.ToString());
        }
        result = 1;
        return (name.Substring(0, name.Length - s.Length) + " " + result.ToString());
    }

    internal static int GetSubTypeDepth(System.Type type, System.Type parent)
    {
        if (type == null)
        {
            return 0x3e8;
        }
        if (parent == type)
        {
            return 0;
        }
        return (GetSubTypeDepth(type.BaseType, parent) + 1);
    }

    public static string GetUserFriendlyName(string componentName, string memberName)
    {
        string str = memberName;
        if (componentName == "Transform")
        {
            if (memberName == "localPosition")
            {
                return "Position";
            }
            if (memberName == "localEulerAngles")
            {
                return "Rotation";
            }
            if (memberName == "localScale")
            {
                str = "Scale";
            }
        }
        return str;
    }

    public static string GetUserFriendlyName(Component component, MemberInfo memberInfo) => 
        GetUserFriendlyName(component.GetType().Name, memberInfo.Name);
}

