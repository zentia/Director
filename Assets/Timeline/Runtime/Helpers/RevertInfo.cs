using System;
using System.Linq;
using System.Reflection;
using Assets.Plugins.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineRuntime
{
    /// <summary>
    ///     Holds info related to reverting objects to a former state.
    /// </summary>
    public class RevertInfo
    {
        private readonly Object Instance;
        private readonly MemberInfo[] MemberInfo;
        public readonly TimelineItem MonoBehaviour;
        private readonly Type Type;
        private readonly object value;
        private Action<Object, object> m_Callback;
        private Object param1;
        private object param2;

        public RevertInfo(TimelineItem monoBehaviour, Type type, string memberName, object value)
        {
            MonoBehaviour = monoBehaviour;
            Type = type;
            this.value = value;
            MemberInfo = ReflectionHelper.GetMemberInfo(type, memberName);
        }

        public RevertInfo(TimelineItem monoBehaviour, Object obj, string memberName, object value)
        {
            MonoBehaviour = monoBehaviour;
            Instance = obj;
            Type = obj.GetType();
            this.value = value;
            MemberInfo = ReflectionHelper.GetMemberInfo(Type, memberName);
        }

        public RevertInfo(TimelineItem monoBehaviour, Action<Object, object> callback, Object param1, object param2)
        {
            MonoBehaviour = monoBehaviour;
            m_Callback = callback;
            this.param1 = param1;
            this.param2 = param2;
        }

        /// <summary>
        ///     Revert the given object to its former state.
        /// </summary>
        public void Revert()
        {
            if (m_Callback != null)
            {
                m_Callback(param1, param2);
                return;
            }
            if (Instance == null)
            {
                return;
            }
            if (MemberInfo != null && MemberInfo.Length > 0)
            {
                if (MemberInfo[0] is FieldInfo)
                {
                    var fi = MemberInfo[0] as FieldInfo;
                    if (fi.IsStatic || (!fi.IsStatic && Instance != null)) fi.SetValue(Instance, value);
                }
                else if (MemberInfo[0] is PropertyInfo)
                {
                    var pi = MemberInfo[0] as PropertyInfo;
                    //if (Instance != null)
                    {
                        pi.SetValue(Instance, value, null);
                    }
                }
                else if (MemberInfo[0] is MethodInfo)
                {
                    Type[] paramTypes = { };

                    //Initialize array of parameter types
                    if (value.GetType().IsArray)
                    {
                        var values = (object[])value;

                        paramTypes = new Type[values.Length];
                        for (var i = 0; i < values.Length; i++) paramTypes[i] = values[i].GetType();
                    }
                    else if (value != null)
                    {
                        paramTypes = new[] { value.GetType() };
                    }

                    //Look for a match
                    var matchIndex = -1;
                    for (var i = 0; i < MemberInfo.Length; i++)
                    {
                        var pi = (MemberInfo[i] as MethodInfo).GetParameters();
                        var methodParams = new Type[pi.Length];
                        for (var j = 0; j < pi.Length; j++) methodParams[j] = pi[j].ParameterType;
                        if (paramTypes.SequenceEqual(methodParams))
                        {
                            matchIndex = i;
                            break;
                        }
                    }

                    if (matchIndex != -1)
                    {
                        //Invoke the matching MethodInfo
                        var mi = MemberInfo[matchIndex] as MethodInfo;

                        if (mi.IsStatic || (!mi.IsStatic && Instance != null))
                        {
                            if (value == null)
                                mi.Invoke(Instance, null);
                            else if (value.GetType().IsArray)
                                mi.Invoke(Instance, (object[])value);
                            else
                                mi.Invoke(Instance, new[] { value });
                        }
                    }
                    else
                    {
                        Log.LogE(LogTag.Timeline, "Error while reverting: Could not find method \" {0}\" that accepts parameters {{1}}.", MemberInfo[0].Name, string.Join(", ", paramTypes.Select(v => v.ToString()).ToArray()));
                    }
                }
            }
        }
    }
}
