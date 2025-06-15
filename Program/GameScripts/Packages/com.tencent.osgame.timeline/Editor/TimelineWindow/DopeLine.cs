using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TimelineEditorInternal
{
    internal class DopeLine
    {
        private int m_HierarchyNodeID;
        private TimelineWindowCurve[] m_Curves;
        private List<TimelineWindowKeyframe> m_Keys;

        public static GUIStyle dopekeyStyle = "Dopesheetkeyframe";

        public Rect position;
        public System.Type objectType;
        public bool tallMode;
        public bool hasChildren;
        public bool isMasterDopeline;

        public System.Type valueType
        {
            get
            {
                if (m_Curves.Length > 0)
                {
                    System.Type type = m_Curves[0].valueType;
                    for (int i = 1; i < m_Curves.Length; i++)
                    {
                        if (m_Curves[i].valueType != type)
                            return null;
                    }
                    return type;
                }

                return null;
            }
        }

        public bool isPptrDopeline
        {
            get
            {
                if (m_Curves.Length > 0)
                {
                    for (int i = 0; i < m_Curves.Length; i++)
                    {
                        if (!m_Curves[i].isPPtrCurve)
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }

        public bool isEditable
        {
            get
            {
                if (m_Curves.Length > 0)
                {
                    bool isReadOnly = Array.Exists(m_Curves, curve => !curve.animationIsEditable);
                    return !isReadOnly;
                }

                return false;
            }
        }

        public int hierarchyNodeID
        {
            get
            {
                return m_HierarchyNodeID;
            }
        }

        public TimelineWindowCurve[] curves
        {
            get
            {
                return m_Curves;
            }
        }

        public List<TimelineWindowKeyframe> keys
        {
            get
            {
                if (m_Keys == null)
                {
                    m_Keys = new List<TimelineWindowKeyframe>();
                    foreach (TimelineWindowCurve curve in m_Curves)
                        foreach (TimelineWindowKeyframe key in curve.m_Keyframes)
                            m_Keys.Add(key);

                    m_Keys.Sort((a, b) => a.time.CompareTo(b.time));
                }

                return m_Keys;
            }
        }

        public void InvalidateKeyframes()
        {
            m_Keys = null;
        }

        public DopeLine(int hierarchyNodeID, TimelineWindowCurve[] curves)
        {
            m_HierarchyNodeID = hierarchyNodeID;
            m_Curves = curves;
        }
    }
}
