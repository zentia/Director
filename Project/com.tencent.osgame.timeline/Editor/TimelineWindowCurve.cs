using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TimelineEditorInternal
{
    internal class TimelineWindowCurve : IComparable<TimelineWindowCurve>, IEquatable<TimelineWindowCurve>
    {
        public const float timeEpsilon = 0.00001f;
        public List<TimelineWindowKeyframe> m_Keyframes;
        private EditorCurveBinding m_Binding;
        public string propertyName { get { return m_Binding.propertyName; } }
        public string path { get { return m_Binding.path; } }
        public System.Type type { get { return m_Binding.type; } }
        int ComparePaths(string otherPath)
        {
            var thisPath = path.Split('/');
            var objPath = otherPath.Split('/');

            int smallerLength = Math.Min(thisPath.Length, objPath.Length);
            for (int i = 0; i < smallerLength; ++i)
            {
                int compare = string.Compare(thisPath[i], objPath[i], StringComparison.Ordinal);
                if (compare == 0)
                {
                    continue;
                }

                return compare;
            }

            if (thisPath.Length < objPath.Length)
            {
                return -1;
            }

            return 1;
        }

        public int CompareTo(TimelineWindowCurve obj)
        {
            if (!path.Equals(obj.path))
            {
                return ComparePaths(obj.path);
            }

            bool sameTransformComponent = type == typeof(Transform) && obj.type == typeof(Transform);
            bool oneIsTransformComponent = (type == typeof(Transform) || obj.type == typeof(Transform));

// We want to sort position before rotation
            if (sameTransformComponent)
            {
                string propertyGroupA = TimelineWindowUtility.GetPropertyGroupName(propertyName);
                string propertyGroupB = TimelineWindowUtility.GetPropertyGroupName(obj.propertyName);

                if (propertyGroupA.Equals("m_LocalPosition") && (propertyGroupB.Equals("m_LocalRotation") || propertyGroupB.StartsWith("localEulerAngles")))
                    return -1;
                if ((propertyGroupA.Equals("m_LocalRotation") || propertyGroupA.StartsWith("localEulerAngles")) && propertyGroupB.Equals("m_LocalPosition"))
                    return 1;
            }
// Transform component should always come first.
            else if (oneIsTransformComponent)
            {
                if (type == typeof(Transform))
                    return -1;
                else
                    return 1;
            }

// Sort (.r, .g, .b, .a) and (.x, .y, .z, .w)
            if (obj.type == type)
            {
                int lhsIndex = TimelineWindowUtility.GetComponentIndex(obj.propertyName);
                int rhsIndex = TimelineWindowUtility.GetComponentIndex(propertyName);
                if (lhsIndex != -1 && rhsIndex != -1 && propertyName.Substring(0, propertyName.Length - 2) == obj.propertyName.Substring(0, obj.propertyName.Length - 2))
                    return rhsIndex - lhsIndex;
            }

            return string.Compare((path + type + propertyName), obj.path + obj.type + obj.propertyName, StringComparison.Ordinal);
        }
        public bool Equals(TimelineWindowCurve other)
        {
            return CompareTo(other) == 0;
        }
    }
}
