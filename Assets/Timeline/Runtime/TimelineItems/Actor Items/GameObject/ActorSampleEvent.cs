using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace TimelineRuntime
{
    [TimelineItem("Game Object", "Sample", TimelineItemGenre.ActorItem)]
    public class ActorSampleEvent : TimelineActorEvent, IRecoverableObject
    {
        [SerializeField]
        private float a;
        [SerializeField]
        private float b;
        [SerializeField]
        private float c;
        [SerializeField]
        private float d;
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;
        [TimelineType]
        public string typeName;
        [SerializeField, TimelineMember]
        private string memberName;
        [ReadOnly]
        public bool isProperty;
        [ReadOnly]
        public PropertyTypeInfo propertyTypeInfo;

        internal object GetCurrentValue(Component component)
        {
            if (component == null || memberName == string.Empty)
            {
                return null;
            }
            var type = component.GetType();
            object value = null;
            if (isProperty)
            {
                var propertyInfo = ReflectionHelper.GetProperty(type, memberName);
                if (propertyInfo != null)
                {
                    value = propertyInfo.GetValue(component, null);
                }
            }
            else
            {
                var fieldInfo = ReflectionHelper.GetField(type, memberName);
                if (fieldInfo != null)
                {
                    value = fieldInfo.GetValue(component);
                }
            }
            return value;
        }

        [Button]
        private void Sample()
        {
            var actors = GetActors();
            if (actors == null)
                return;
            foreach (var actor in actors)
            {
                if (actor != null)
                {
                    var component = actor.GetComponent(typeName);
                    if (component != null)
                    {
                        SaveValue(GetCurrentValue(component));
                    }
                    return;
                }
            }
        }
        public RevertInfo[] CacheState()
        {
            var actors = GetActors();
            if (actors == null)
                return null;
            var reverts = new List<RevertInfo>();
            foreach (var actor in actors)
            {
                if (actor != null)
                {
                    var component = actor.GetComponent(typeName);
                    if (component != null)
                    {
                        var info = new RevertInfo(this, component, memberName, GetCurrentValue(component));
                        reverts.Add(info);
                    }
                }
            }
            return reverts.ToArray();
        }

        private object Evaluate(Type t)
        {
            return propertyTypeInfo switch
            {
                PropertyTypeInfo.Color => new Color(a, b, c, d),
                PropertyTypeInfo.Double => a,
                PropertyTypeInfo.Float => a,
                PropertyTypeInfo.Int => (int)a,
                PropertyTypeInfo.Long => (int)a,
                PropertyTypeInfo.Bool => (int)a == 1,
                PropertyTypeInfo.Enum => Enum.ToObject(t, (int)a),
                PropertyTypeInfo.Quaternion => new Quaternion(a, b, c, d),
                PropertyTypeInfo.Vector2 => new Vector2(a, b),
                PropertyTypeInfo.Vector3 => new Vector3(a, b, c),
                PropertyTypeInfo.Vector4 => new Vector4(a, b, c, d),
                PropertyTypeInfo.None => throw new ArgumentException("Invalid argument"),
                _ => throw new ArgumentException("Invalid argument")
            };
        }

        private void SaveValue(object value)
        {
            switch (propertyTypeInfo)
            {
                case PropertyTypeInfo.Color:
                    var color = (Color)value;
                    a = color.r;
                    b = color.g;
                    c = color.b;
                    d = color.a;
                    return;
                case PropertyTypeInfo.Double:
                case PropertyTypeInfo.Float:
                    a = (float)value;
                    return;
                case PropertyTypeInfo.Int:
                case PropertyTypeInfo.Long:
                case PropertyTypeInfo.Enum:
                    a = (int)value;
                    return;
                case PropertyTypeInfo.Bool:
                    a = (bool)value ? 1 : 0;
                    return;
                case PropertyTypeInfo.Quaternion:
                    var q = (Quaternion)value;
                    a = q.x;
                    b = q.y;
                    c = q.z;
                    d = q.w;
                    return;
                case PropertyTypeInfo.Vector2:
                    var v2 = (Vector2)value;
                    a = v2.x;
                    b = v2.y;
                    return;
                case PropertyTypeInfo.Vector3:
                    var v3 = (Vector3)value;
                    a = v3.x;
                    b = v3.y;
                    c = v3.z;
                    return;
                case PropertyTypeInfo.Vector4:
                    var v4 = (Vector4)value;
                    a = v4.x;
                    b = v4.y;
                    c = v4.z;
                    d = v4.w;
                    return;
            }
        }

        public override void Trigger(GameObject actor)
        {
            var component = actor.GetComponent(typeName);
            if (component == null)
            {
                return;
            }
            var componentType = component.GetType();
            if (isProperty)
            {
                var propertyInfo = ReflectionHelper.GetProperty(componentType, memberName);
                if (propertyInfo == null)
                    return;
                var value = Evaluate(propertyInfo.PropertyType);
                if (propertyInfo.PropertyType == value.GetType())
                {
                    propertyInfo.SetValue(component, value, null);
                }
                else
                {
                    Debug.LogError($"Property {propertyInfo.Name} of type {componentType.Name} is not supported");
                }
            }
            else
            {
                var fieldInfo = ReflectionHelper.GetField(componentType, memberName);
                if (fieldInfo == null)
                {
                    return;
                }
                var value = Evaluate(fieldInfo.FieldType);
                if (fieldInfo.FieldType == value.GetType())
                {
                    fieldInfo.SetValue(component, value);
                }
                else
                {
                    Debug.LogError($"Field {fieldInfo.Name} of type {componentType.Name} is not supported");
                }
            }
        }

        public RevertMode RuntimeRevertMode
        {
            get => runtimeRevertMode;
            set => runtimeRevertMode = value;
        }
    }
}
