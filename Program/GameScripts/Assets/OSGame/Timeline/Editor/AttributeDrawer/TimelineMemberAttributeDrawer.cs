using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineMemberAttributeDrawer : OdinAttributeDrawer<TimelineMemberAttribute, string>
    {
        private ActorTrackGroup m_ActorTrackGroup;
        private ActorSampleEvent m_ActorSampleEvent;
        private GameObject m_GameObject;

        private OdinSelector<MemberInfo> CreateSelector(Rect rect)
        {
            return TimelineTextSelector<MemberInfo>.Create(members =>
            {
                var member = members.FirstOrDefault();
                ValueEntry.SmartValue = member.Name;
                m_ActorSampleEvent.isProperty = member is PropertyInfo;
                m_ActorSampleEvent.propertyTypeInfo = UnityPropertyTypeInfo.GetMappedType(m_ActorSampleEvent.isProperty ? (member as PropertyInfo).PropertyType : (member as FieldInfo).FieldType);
            }, ()=>
            {
                if (string.IsNullOrEmpty(m_ActorSampleEvent.typeName))
                {
                    return null;
                }
                var component = m_GameObject.GetComponent(m_ActorSampleEvent.typeName);
                return component == null? null : TimelineHelper.GetValidMembers(component);
            },item=>item.Name);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (m_ActorTrackGroup == null)
            {
                m_ActorSampleEvent = Selection.activeGameObject.GetComponent<ActorSampleEvent>();
                m_ActorTrackGroup = m_ActorSampleEvent.GetComponentInParent<ActorTrackGroup>(true);
            }

            var actors = m_ActorTrackGroup.Actors;
            if (actors != null)
            {
                foreach (var actor in actors)
                {
                    m_GameObject = actor.gameObject;
                    SirenixEditorGUI.GetFeatureRichControlRect(label, out _, out _, out var valueRect);
                    GenericSelector<MemberInfo>.DrawSelectorDropdown(valueRect, ValueEntry.SmartValue, CreateSelector);
                    return;
                }
            }
            base.DrawPropertyLayout(label);
        }
    }
}
