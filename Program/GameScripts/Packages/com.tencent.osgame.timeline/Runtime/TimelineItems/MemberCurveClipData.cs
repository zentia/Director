using System;
using UnityEngine;

namespace TimelineRuntime
{
    [Serializable]
    public class MemberCurveClipData
    {
        public string Type;
        public string PropertyName;
        public bool IsProperty = true;
        public PropertyTypeInfo PropertyType = PropertyTypeInfo.None;

        public AnimationCurve Curve1 = new();
        public AnimationCurve Curve2 = new();
        public AnimationCurve Curve3 = new();
        public AnimationCurve Curve4 = new();

        public AnimationCurve GetCurve(int i)
        {
            if (i == 1) return Curve2;
            if (i == 2) return Curve3;
            if (i == 3) return Curve4;
            return Curve1;
        }

        public void SetCurve(int i, AnimationCurve newCurve)
        {
            if (i == 1)
            {
                Curve2 = newCurve;
            }
            else if (i == 2)
            {
                Curve3 = newCurve;
            }
            else if (i == 3)
            {
                Curve4 = newCurve;
            }
            else
            {
                Curve1 = newCurve;
            }
        }

        internal object GetCurrentValue(Component component)
        {
            if (component == null || PropertyName == string.Empty)
            {
                return null;
            }
            Type type = component.GetType();
            object value = null;
            if (IsProperty)
            {
                var propertyInfo = ReflectionHelper.GetProperty(type, PropertyName);
                if (propertyInfo != null)
                {
                    value = propertyInfo.GetValue(component, null);
                }
            }
            else
            {
                var fieldInfo = ReflectionHelper.GetField(type, PropertyName);
                if (fieldInfo != null)
                {
                    value = fieldInfo.GetValue(component);
                }
            }
            return value;
        }

        internal void Reset(GameObject actor)
        {
        }
    }
}
