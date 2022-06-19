using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CinemaDirector
{
    public static class ReflectionHelper
    {
        public static MemberInfo[] GetMemberInfo(Type type, string name)
        {
            return type.GetMember(name);
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetProperty(name);
        }

        public static void SetField(FieldInfo fieldInfo, object obj, object value)
        {
            if (fieldInfo == null)
            {
                Debug.LogError("NullReferenceException: Object reference not set to an instance of an object");
                return;
            }
            if (fieldInfo.FieldType == typeof(Quaternion))
            {
                var vec3 = (Vector3) value;
                fieldInfo.SetValue(obj, Quaternion.Euler(vec3));
            }
            else if (value is TemplateObject)
            {
                var templateObject = (TemplateObject) value;
                fieldInfo.SetValue(obj, templateObject.templateObject.id);
            }
            else if (value is List<object>)
            {
                var objects = (List<object>) value;
                if (fieldInfo.FieldType == typeof(string[]))
                {
                    fieldInfo.SetValue(obj, Array.ConvertAll(objects.ToArray(), v=>(string)v));    
                }
            }
            else
            {
                if (fieldInfo.FieldType == value.GetType())
                {
                    fieldInfo.SetValue(obj, value);    
                }
                else
                {
                    Debug.LogErrorFormat("source type = {0}, dest type = {1}", fieldInfo.FieldType.Name, value.GetType().Name);
                }
            }
        }
    }
}