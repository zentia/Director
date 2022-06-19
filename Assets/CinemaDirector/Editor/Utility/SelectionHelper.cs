using System;
using UnityEngine;
using System.Collections.Generic;
using EditorExtension;
using Object = UnityEngine.Object;

namespace CinemaDirector
{
    [Serializable]
    public class SelectionHelper
    {
        public List<object> objects = new List<object>();
        [SerializeField]
        private DirectorInspector m_OdinEditorWindow;
        [SerializeField]
        private Object _activeObject;
        public Object activeObject
        {
            get
            {
                return _activeObject;
            }
            set
            {
                if (_activeObject != null)
                {
                    Remove(_activeObject);
                }

                _activeObject = value;    
                if (_activeObject != null)
                {
                    objects.Clear();
                    Add(_activeObject);
                    if (m_OdinEditorWindow == null)
                    {
                        m_OdinEditorWindow = DirectorInspector.CreateInspectorWindow();
                        m_OdinEditorWindow.minSize = new Vector2(200, 600);
                        m_OdinEditorWindow.DockTo(DirectorWindow.Instance, DockPosition.Right);
                    }
                }
            }
        }

        public SelectionHelper()
        {
            DirectorEvent.DestroyObject.AddListener(DestroyObject);
        }

        ~SelectionHelper()
        {
            DirectorEvent.DestroyObject.RemoveListener(DestroyObject);
        }

        private void DestroyObject(DirectorObject directorObject)
        {
            if (directorObject == _activeObject)
            {
                activeObject = null;
            }
        }

        public bool Contains(Object obj)
        {
            return objects.Contains(obj);
        }

        public void Add(Object obj)
        {
            objects.Add(obj);
        }

        public void Remove(Object obj)
        {
            objects.Remove(obj);
        }

        public void Destroy()
        {
            if (m_OdinEditorWindow)
            {
                m_OdinEditorWindow.Close();
            }
        }

        public void Repaint()
        {
            if (m_OdinEditorWindow)
            {
                m_OdinEditorWindow.Repaint();
            }
        }
    }
}