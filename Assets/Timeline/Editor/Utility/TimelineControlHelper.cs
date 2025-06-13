using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace TimelineEditor
{
    public static class TimelineControlHelper
    {
        internal static Type[] GetAllSubTypes(Type ParentType)
        {
            List<Type> list = new List<Type>();

            var assembly = ParentType.Assembly;
            var types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(ParentType))
                {
                    list.Add(type);
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

        internal static int GetSubTypeDepth(Type type, Type parent)
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
    }
}
