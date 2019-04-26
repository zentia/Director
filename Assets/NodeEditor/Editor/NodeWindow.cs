using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace NodeDirector
{
    public class NodeWindow : EditorWindow
    {
        public static NodeWindow m_Instance;
        private GUIStyle m_NodeStyle;
        private void OnEnable()
        {
            m_Instance = this;
            m_NodeStyle = new GUIStyle();
            m_NodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        }
        private bool ProcessNode(NodeEntity entity)
        {
            if (!entity.m_Rect.Contains(Event.current.mousePosition))
            {
                return false;
            }
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        entity.OnMouseLeftDown(Event.current);
                    }
                    else
                    {
                        entity.OnMouseRightDown(Event.current);
                    }
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0)
                    {
                        entity.OnMouseLeftUp(Event.current);
                    }
                    else
                    {
                        entity.OnMouseRightUp(Event.current);
                    }
                    break;
                case EventType.MouseDrag:
                    if (Event.current.button == 0)
                    {
                        entity.OnMouseLeftDrag(Event.current);
                    }
                    else
                    {
                        entity.OnMouseRightDrag(Event.current);
                    }
                    break;
            }
            return true;
        }
        private void DrawNode(NodeEntity entity)
        {
            GUI.Box(entity.m_Rect, entity.m_Name, entity.m_Style);
        }
        private bool ProcessNodes()
        {
            bool mark = false;
            for (int i = 0; i < (int)NodeKind.Count; i++)
            {
                var list = NodeControl.m_NodeInstanceGroups[i];
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        var entity = list[j];
                        if (entity != null)
                        {
                            mark |= ProcessNode(entity);
                            DrawNode(entity);
                        }
                    }
                }
            }
            return mark;
        }
        private void OnGUI()
        {
            if (!ProcessNodes())
            {
                ProcessDirector();
            }
            if (GUI.changed)
            {
                Repaint();
            }
        }
        private void ProcessDirector()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 1)
                    {
                        GenericMenu genericMenu = new GenericMenu();
                        genericMenu.AddItem(new GUIContent("添加节点"), false, () => { });
                        genericMenu.ShowAsContext();
                    }
                    break;
            }
        }
        private void OnDisable()
        {
            m_Instance = null;
        }
    }
}

