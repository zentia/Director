using System;
using System.Collections.Generic;
using System.Reflection;

namespace TimelineRuntime
{
    public static class ReflectionHelper
    {
        public static Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        public static Type[] GetTypes(Assembly assembly)
        {
            return assembly.GetTypes();
        }

        public static bool IsSubclassOf(Type type, Type c)
        {
            return type.IsSubclassOf(c);
        }

        public static MemberInfo[] GetMemberInfo(Type type, string name)
        {
            Dictionary<string, MemberInfo[]> memberInfos;
            if (s_CacheMemberInfos.TryGetValue(type, out memberInfos))
            {
                if (memberInfos.ContainsKey(name))
                {
                    return memberInfos[name];
                }
            }
            var members = type.GetMember(name);
            if (memberInfos == null)
            {
                memberInfos = new Dictionary<string, MemberInfo[]>();
                s_CacheMemberInfos.Add(type, memberInfos);
            }
            memberInfos.Add(name, members);
            return members;
        }

        public static FieldInfo GetField(Type type, string name)
        {
            return type.GetField(name);
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetProperty(name);
        }

        public static T[] GetCustomAttributes<T>(Type type, bool inherited) where T : Attribute
        {
            return (T[])type.GetCustomAttributes(typeof(T), inherited);
        }

        private static readonly Dictionary<Type, Dictionary<string, MemberInfo[]>> s_CacheMemberInfos = new();
    }
}
