using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineEditor
{
    public class ObjectSpaceAttributeDrawer : OdinAttributeDrawer<ObjectSpaceAttribute, ObjectSpace>
    {
        private Object m_Asset;

        private static bool IsParent(Transform child, Transform parent, out string path)
        {
            path = "";
            if (parent == null)
            {
                return false;
            }
            while (child)
            {
                if (child == parent)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(path))
                {
                    path = child.name;
                }
                else
                {
                    path = child.name + "/" + path;    
                }
                child = child.parent;
            }

            return false;
        }

        private OdinSelector<ActorTrackGroup> CreatorSelector(Rect rect)
        {
            return TimelineGroupSelector.Create(groups =>
            {
                ValueEntry.SmartValue.group = groups.FirstOrDefault();
            });
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            int tempControlId;
            bool hasKeyboardFocus;
            Rect valueRect;
            SirenixEditorGUI.GetFeatureRichControlRect(label, out tempControlId, out hasKeyboardFocus, out valueRect);
            GenericSelector<ActorTrackGroup>.DrawSelectorDropdown(valueRect, ValueEntry.SmartValue.group!=null?ValueEntry.SmartValue.group.name:"null", CreatorSelector);
            GUI.enabled = true;
            if (ValueEntry.SmartValue.group == null)
            {
                ValueEntry.SmartValue.path = EditorGUILayout.TextField("SpaceObjectName", ValueEntry.SmartValue.path);
            }
            else
            {
                var tf = EditorGUILayout.ObjectField("Path", m_Asset, typeof(Transform), true) as Transform;
                if (tf == null)
                {
                    m_Asset = null;
                    ValueEntry.SmartValue.path = null;
                }
                else
                {
                    foreach (var actor in ValueEntry.SmartValue.group.Actors)
                    {
                        if (IsParent(tf, actor, out ValueEntry.SmartValue.path))
                        {
                            m_Asset = tf;
                        }    
                    }
                    
                }
            }
        }
    }
}
