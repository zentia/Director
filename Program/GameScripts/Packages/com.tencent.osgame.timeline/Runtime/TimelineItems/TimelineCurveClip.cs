using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    public abstract class TimelineCurveClip : TimelineActorAction
    {
        [SerializeField]
        private List<MemberCurveClipData> curveData = new();

        public List<MemberCurveClipData> CurveData => curveData;

        public virtual object GetCurrentValue(Component component, MemberCurveClipData property, bool isProperty)
        {
            var propertyName = property.PropertyName;
            if (component == null || propertyName == string.Empty)
                return null;

            var type = component.GetType();
            object value;
            if (isProperty)
            {
                var propertyInfo = ReflectionHelper.GetProperty(type, propertyName);
                value = propertyInfo.GetValue(component, null);
            }
            else
            {
                var fieldInfo = ReflectionHelper.GetField(type, propertyName);
                value = fieldInfo.GetValue(component);
            }

            return value;
        }

        protected virtual bool InitializeClipCurves(MemberCurveClipData data, Component component)
        {
            var value = GetCurrentValue(component, data, data.IsProperty);
            var typeInfo = data.PropertyType;
            var startTime = Firetime;
            var endTime = Firetime + Duration;

            if (typeInfo == PropertyTypeInfo.Int || typeInfo == PropertyTypeInfo.Long || typeInfo == PropertyTypeInfo.Float || typeInfo == PropertyTypeInfo.Double)
            {
                var x = (float) value;

                if (float.IsInfinity(x) || float.IsNaN(x))
                    return false;

                data.Curve1 = AnimationCurve.Linear(startTime, x, endTime, x);
            }
            else if (typeInfo == PropertyTypeInfo.Bool)
            {
                var x = (bool) value ? 1 : 0;
                data.Curve1 = AnimationCurve.Linear(startTime, x, endTime, x);
            }
            else if (typeInfo == PropertyTypeInfo.Enum)
            {
                var x = (int) value;
                data.Curve1 = AnimationCurve.Linear(startTime, x, endTime, x);
            }
            else if (typeInfo == PropertyTypeInfo.Vector2)
            {
                var vec2 = (Vector2)value;

                if (float.IsInfinity(vec2.x) || float.IsNaN(vec2.x) ||
                    float.IsInfinity(vec2.y) || float.IsNaN(vec2.y))
                    return false;

                data.Curve1 = AnimationCurve.Linear(startTime, vec2.x, endTime, vec2.x);
                data.Curve2 = AnimationCurve.Linear(startTime, vec2.y, endTime, vec2.y);
            }
            else if (typeInfo == PropertyTypeInfo.Vector3)
            {
                var vec3 = (Vector3)value;

                if (float.IsInfinity(vec3.x) || float.IsNaN(vec3.x) ||
                    float.IsInfinity(vec3.y) || float.IsNaN(vec3.y) ||
                    float.IsInfinity(vec3.z) || float.IsNaN(vec3.z))
                    return false;

                data.Curve1 = AnimationCurve.Linear(startTime, vec3.x, endTime, vec3.x);
                data.Curve2 = AnimationCurve.Linear(startTime, vec3.y, endTime, vec3.y);
                data.Curve3 = AnimationCurve.Linear(startTime, vec3.z, endTime, vec3.z);
            }
            else if (typeInfo == PropertyTypeInfo.Vector4)
            {
                var vec4 = (Vector4)value;

                if (float.IsInfinity(vec4.x) || float.IsNaN(vec4.x) ||
                    float.IsInfinity(vec4.y) || float.IsNaN(vec4.y) ||
                    float.IsInfinity(vec4.z) || float.IsNaN(vec4.z) ||
                    float.IsInfinity(vec4.w) || float.IsNaN(vec4.w))
                    return false;

                data.Curve1 = AnimationCurve.Linear(startTime, vec4.x, endTime, vec4.x);
                data.Curve2 = AnimationCurve.Linear(startTime, vec4.y, endTime, vec4.y);
                data.Curve3 = AnimationCurve.Linear(startTime, vec4.z, endTime, vec4.z);
                data.Curve4 = AnimationCurve.Linear(startTime, vec4.w, endTime, vec4.w);
            }
            else if (typeInfo == PropertyTypeInfo.Quaternion)
            {
                var quaternion = (Quaternion)value;

                if (float.IsInfinity(quaternion.x) || float.IsNaN(quaternion.x) ||
                    float.IsInfinity(quaternion.y) || float.IsNaN(quaternion.y) ||
                    float.IsInfinity(quaternion.z) || float.IsNaN(quaternion.z) ||
                    float.IsInfinity(quaternion.w) || float.IsNaN(quaternion.w))
                    return false;

                data.Curve1 = AnimationCurve.Linear(startTime, quaternion.x, endTime, quaternion.x);
                data.Curve2 = AnimationCurve.Linear(startTime, quaternion.y, endTime, quaternion.y);
                data.Curve3 = AnimationCurve.Linear(startTime, quaternion.z, endTime, quaternion.z);
                data.Curve4 = AnimationCurve.Linear(startTime, quaternion.w, endTime, quaternion.w);
            }
            else if (typeInfo == PropertyTypeInfo.Color)
            {
                var color = (Color)value;

                if (float.IsInfinity(color.r) || float.IsNaN(color.r) ||
                    float.IsInfinity(color.g) || float.IsNaN(color.g) ||
                    float.IsInfinity(color.b) || float.IsNaN(color.b) ||
                    float.IsInfinity(color.a) || float.IsNaN(color.a))
                    return false;

                data.Curve1 = AnimationCurve.Linear(startTime, color.r, endTime, color.r);
                data.Curve2 = AnimationCurve.Linear(startTime, color.g, endTime, color.g);
                data.Curve3 = AnimationCurve.Linear(startTime, color.b, endTime, color.b);
                data.Curve4 = AnimationCurve.Linear(startTime, color.a, endTime, color.a);
            }

            return true;
        }

        public void AddClipCurveData(Component component, string name, bool isProperty, Type type)
        {
            var data = new MemberCurveClipData();
            if (component.GetType().Namespace == "UnityEngine")
            {
                data.Type = component.GetType().Name;
            }
            else
            {
                data.Type = component.GetType().FullName;
            }
            data.PropertyName = name;
            data.IsProperty = isProperty;
            data.PropertyType = UnityPropertyTypeInfo.GetMappedType(type);
            if (InitializeClipCurves(data, component))
            {
                curveData.Add(data);
            }
            else
            {
                Debug.LogError("Could not initialize curve curveClip, invalid initial values.");
            }
        }

        public void DeleteClipCurveData(string componentType, string propertyName)
        {
            curveData.RemoveAll(item => item.Type == componentType && item.PropertyName == propertyName);
        }

        public static object Evaluate(MemberCurveClipData memberCurveData, float time, Type type)
        {
            object value = null;
            switch (memberCurveData.PropertyType)
            {
                case PropertyTypeInfo.Color:
                    Color c;
                    c.r = memberCurveData.Curve1.Evaluate(time);
                    c.g = memberCurveData.Curve2.Evaluate(time);
                    c.b = memberCurveData.Curve3.Evaluate(time);
                    c.a = memberCurveData.Curve4.Evaluate(time);
                    value = c;
                    break;

                case PropertyTypeInfo.Double:
                case PropertyTypeInfo.Float:
                    value = memberCurveData.Curve1.Evaluate(time);
                    break;
                case PropertyTypeInfo.Int:
                case PropertyTypeInfo.Long:
                    value = (int)memberCurveData.Curve1.Evaluate(time);
                    break;
                case PropertyTypeInfo.Bool:
                    return (int)memberCurveData.Curve1.Evaluate(time) == 1;
                case PropertyTypeInfo.Enum:
                    return Enum.ToObject(type, (int)memberCurveData.Curve1.Evaluate(time));
                case PropertyTypeInfo.Quaternion:
                    Quaternion q;
                    q.x = memberCurveData.Curve1.Evaluate(time);
                    q.y = memberCurveData.Curve2.Evaluate(time);
                    q.z = memberCurveData.Curve3.Evaluate(time);
                    q.w = memberCurveData.Curve4.Evaluate(time);
                    value = q;
                    break;

                case PropertyTypeInfo.Vector2:
                    Vector2 v2;
                    v2.x = memberCurveData.Curve1.Evaluate(time);
                    v2.y = memberCurveData.Curve2.Evaluate(time);
                    value = v2;
                    break;

                case PropertyTypeInfo.Vector3:
                    Vector3 v3;
                    v3.x = memberCurveData.Curve1.Evaluate(time);
                    v3.y = memberCurveData.Curve2.Evaluate(time);
                    v3.z = memberCurveData.Curve3.Evaluate(time);
                    value = v3;
                    break;

                case PropertyTypeInfo.Vector4:
                    Vector4 v4;
                    v4.x = memberCurveData.Curve1.Evaluate(time);
                    v4.y = memberCurveData.Curve2.Evaluate(time);
                    v4.z = memberCurveData.Curve3.Evaluate(time);
                    v4.w = memberCurveData.Curve4.Evaluate(time);
                    value = v4;
                    break;
            }
            return value;
        }

        public static Color EvaluateColor(MemberCurveClipData memberCurveData, float time)
        {
            Color c;
            c.r = memberCurveData.Curve1.Evaluate(time);
            c.g = memberCurveData.Curve2.Evaluate(time);
            c.b = memberCurveData.Curve3.Evaluate(time);
            c.a = memberCurveData.Curve4.Evaluate(time);
            return c;
        }

        public static Quaternion EvaluateQuaternion(MemberCurveClipData memberCurveData, float time)
        {
            Quaternion q;
            q.x = memberCurveData.Curve1.Evaluate(time);
            q.y = memberCurveData.Curve2.Evaluate(time);
            q.z = memberCurveData.Curve3.Evaluate(time);
            q.w = memberCurveData.Curve4.Evaluate(time);
            return q;
        }

        public static Vector3 EvaluateVector3(MemberCurveClipData memberCurveData, float time)
        {
            Vector3 v3;
            v3.x = memberCurveData.Curve1.Evaluate(time);
            v3.y = memberCurveData.Curve2.Evaluate(time);
            v3.z = memberCurveData.Curve3.Evaluate(time);
            return v3;
        }

        public static float EvaluateFloat(MemberCurveClipData memberCurveData, float time)
        {
            return memberCurveData.Curve1.Evaluate(time);
        }

        public void TranslateCurves(float amount)
        {
            Firetime += amount;
            for (int i = 0; i < curveData.Count; i++)
            {
                int curveCount = UnityPropertyTypeInfo.GetCurveCount(curveData[i].PropertyType);
                for (int j = 0; j < curveCount; j++)
                {
                    var animationCurve = curveData[i].GetCurve(j);
                    if (amount > 0)
                    {
                        for (int k = animationCurve.length - 1; k >= 0; k--)
                        {
                            var kf = animationCurve.keys[k];
                            var newKeyframe = new Keyframe(kf.time + amount, kf.value, kf.inTangent, kf.outTangent);
                            animationCurve.MoveKey(k, newKeyframe);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < animationCurve.length; k++)
                        {
                            var kf = animationCurve.keys[k];
                            var newKeyframe = new Keyframe(kf.time + amount, kf.value, kf.inTangent, kf.outTangent);
                            animationCurve.MoveKey(k, newKeyframe);
                        }
                    }
                }
            }
        }
    }
}
