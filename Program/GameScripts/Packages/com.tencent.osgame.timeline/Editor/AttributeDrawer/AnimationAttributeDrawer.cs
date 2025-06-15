using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class AnimationAttributeDrawer: OdinAttributeDrawer<AnimationAttribute, string>
    {
        private ActorTrackGroup m_ActorTrackGroup;
        private Animation m_Animation;

        private OdinSelector<string> CreateSelector(Rect rect)
        {
            return TimelineTextSelector<string>.Create(texts =>
            {
                ValueEntry.SmartValue = texts.FirstOrDefault();
            },()=>
            {
                var result = new List<string>(m_Animation.GetClipCount() + 1){""};
                var idx = 1;
                foreach (AnimationState animationState in m_Animation)
                {
                    if (animationState == null)
                    {
                        continue;
                    }
                    result.Add(animationState.name);
                }
                return result;
            }, item =>item);
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
                    m_Animation = actor.GetComponent<Animation>();
                    if (m_Animation != null)
                    {
                        SirenixEditorGUI.GetFeatureRichControlRect(label, out var tempControlId, out var hasKeyboardFocus, out var valueRect);
                        GenericSelector<string>.DrawSelectorDropdown(valueRect, ValueEntry.SmartValue, CreateSelector);
                        return;
                    }

                    break;
                }

            }

            ValueEntry.SmartValue = EditorGUILayout.TextField(label, ValueEntry.SmartValue);
        }
    }
}
