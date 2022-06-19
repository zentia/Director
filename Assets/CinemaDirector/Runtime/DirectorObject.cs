using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml;
using Sirenix.OdinInspector;

namespace CinemaDirector
{
    public class DirectorObject : SerializedScriptableObject, IComparable<DirectorObject>
    {
        public virtual int CompareTo(DirectorObject directorObject)
        {
            return name.CompareTo(directorObject.name);
        }
        private DirectorObject m_Parent;
        public DirectorObject Parent 
        { 
            get 
            {
                return m_Parent;
            } 
            protected set
            {
                m_Parent = value;
            }
        }
        [HideInInspector]
        public List<DirectorObject> Children;
        private bool dirty;
        public bool Dirty
        {
            get
            {
                if (dirty)
                    return true;
                foreach (var child in Children)
                {
                    if (child.Dirty)
                        return true;
                }
                return false;
            }
            set
            {
                dirty = value;
            }
        }
        public DirectorObject()
        {
            Children = new List<DirectorObject>();
        }
        public void SetParent(DirectorObject p)
        {
            if (m_Parent != null)
            {
                m_Parent.RemoveChild(this);
            }
            m_Parent = p;
            if (p != null)
                p.AddChild(this);   
        }

        private void AddChild(DirectorObject child)
        {
            Children.Add(child);
        }

        public virtual DirectorObject CreateChild(DirectorObject directorObject = null)
        {
            return null;
        }

        private void RemoveChild(DirectorObject child)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Delete Node");
#endif
            Children.Remove(child);
            OnRemoveChild(child);
            Dirty = true;
            DirectorEvent.DestroyObject.Invoke(child);
        }

        protected virtual void  OnRemoveChild(DirectorObject child)
        {
            
        }

        public virtual void UpdateRaw()
        {
            
        }

        public T[] GetChildrenByType<T>() where T : DirectorObject
        {
            var list = new List<T>();
            foreach (var child in Children)
            {
                if (child is T)
                {
                    list.Add((T)child);
                }
            }
            return list.ToArray();
        }

        public T GetChildByType<T>() where T : DirectorObject
        {
            foreach (var child in Children)
            {
                if (child is T)
                    return child as T;
            }
            return null;
        }

        public void Destroy(bool undo = false)
        {
            OnUninitialize();
            if (Parent != null)
            {
                Parent.RemoveChild(this);
            }
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                child.Destroy(undo);
            }

            if (!undo)
            {
                DestroyImmediate(this);
            }
        }

        public virtual void OnUninitialize()
        {

        }

        public virtual void Import(XmlElement xmlElement)
        {
            dirty = false;
        }

        public virtual void Export(XmlElement xmlElement)
        {
            dirty = false;
        }

        public DirectorObject GetComponent(string type)
        {
            return Children.First(o => o.GetType().Name == type);
        }

        public static DirectorObject Create(Type type, DirectorObject parent = null, string name = null)
        {
            var instance = CreateInstance(type) as DirectorObject;
            if (!string.IsNullOrEmpty(name))
                instance.name = name;
            instance.SetParent(parent);
            return instance;
        }

        public static DirectorObject Create(DirectorObject directorObject, DirectorObject parent = null, string name = null)
        {
            var instance = Instantiate(directorObject);
            instance.Children.Clear();
            if (!string.IsNullOrEmpty(name))
                instance.name = name;
            instance.SetParent(parent);
            return instance;
        }

        public static T Create<T>(DirectorObject parent = null, string name = null) where T : DirectorObject
        {
            var instance = CreateInstance<T>();
            if (!string.IsNullOrEmpty(name))
                instance.name = name;
            instance.SetParent(parent);
            return instance;
        }
    }
}