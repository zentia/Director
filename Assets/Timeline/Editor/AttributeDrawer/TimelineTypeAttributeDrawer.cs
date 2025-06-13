using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineTypeAttributeDrawer: OdinAttributeDrawer<TimelineTypeAttribute, string>
    {
        private ActorTrackGroup m_ActorTrackGroup;
        private GameObject m_GameObject;

        private OdinSelector<string> CreateSelector(Rect rect)
        {
            return TimelineTextSelector<string>.Create(texts =>
            {
                ValueEntry.SmartValue = texts.FirstOrDefault();
            },()=>
            {
                var behaviours = m_GameObject.GetComponents<Component>();
                var result = new List<string>(behaviours.Length);
                var idx = 1;
                foreach (var behaviour in behaviours)
                {
                    result.Add(behaviour.GetType().Name);
                }
                return result;
            },item=>item);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (m_ActorTrackGroup == null)
            {
                m_ActorTrackGroup = Selection.activeGameObject.GetComponentInParent<ActorTrackGroup>(true);
            }

            var actors = m_ActorTrackGroup.Actors;
            if (actors != null)
            {
                foreach (var actor in actors)
                {
                    m_GameObject = actor.gameObject;
                    SirenixEditorGUI.GetFeatureRichControlRect(label, out _, out _, out var valueRect);
                    GenericSelector<string>.DrawSelectorDropdown(valueRect, ValueEntry.SmartValue, CreateSelector);
                    return;
                }

            }

            ValueEntry.SmartValue = EditorGUILayout.TextField(label, ValueEntry.SmartValue);
        }
    }
}
