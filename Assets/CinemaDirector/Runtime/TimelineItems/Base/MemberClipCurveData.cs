using System;
using System.Reflection;
using UnityEngine;

namespace CinemaDirector
{
    [Serializable]
    public class MemberClipCurveData
    {
        public string Type;
        public string PropertyName;
        public bool IsProperty = true;
        public PropertyTypeInfo PropertyType = PropertyTypeInfo.None;

        public AnimationCurve Curve1 = new AnimationCurve();
        public AnimationCurve Curve2 = new AnimationCurve();
        public AnimationCurve Curve3 = new AnimationCurve();
        public AnimationCurve Curve4 = new AnimationCurve();
        
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

        public void Initialize(TimelineTrack Actor)
        {
        }

        internal void Reset(TimelineTrack Actor)
        {
        }

        internal object getCurrentValue(DirectorObject component)
        {
            if (component == null || PropertyName == string.Empty)
            {
                return null;
            }
            Type type = component.GetType();
            object value = null;
            FieldInfo fieldInfo = type.GetField(PropertyName);
            return fieldInfo.GetValue(component);
        }
    }
}