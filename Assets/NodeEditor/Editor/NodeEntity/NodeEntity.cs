using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeDirector
{
    public class NodeEntity
    {
        public Rect m_Rect;
        bool m_Drag;
        public string m_Name;
        public GUIStyle m_Style;
        public virtual void OnMouseLeftDown(Event @event)
        {

        }
        public virtual void OnMouseLeftUp(Event @event)
        {

        }
        public virtual void OnMouseLeftDrag(Event @event)
        {

        }
        public virtual void OnMouseRightDown(Event @event)
        {

        }
        public virtual void OnMouseRightUp(Event @event)
        {

        }
        public virtual void OnMouseRightDrag(Event @event)
        {

        }
        public void OnRename(string name)
        {
            m_Name = name;
        }
    }
}
