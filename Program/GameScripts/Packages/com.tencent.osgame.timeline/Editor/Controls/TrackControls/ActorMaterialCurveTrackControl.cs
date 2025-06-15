using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    [TimelineTrack(typeof(TimelineMaterialCurveTrack))]
    internal class ActorMaterialCurveTrackControl : ActorCurveTrackControl
    {
        protected override void UpdateToolbar(Rect rect3)
        {
            UpdateNavigate(ref rect3);
            rect3.x += rect3.width;
            if (GUI.Button(rect3, new GUIContent("Add")))
            {
                if (AddMaterialPopup.ShowAtPosition(rect3, AddProperty))
                {
                    GUIUtility.ExitGUI();
                }
            }
        }

        private PropertyTypeInfo ShaderTypeToCurveType(ShaderUtil.ShaderPropertyType shaderPropertyType)
        {
            return shaderPropertyType switch
            {
                ShaderUtil.ShaderPropertyType.Color => PropertyTypeInfo.Color,
                ShaderUtil.ShaderPropertyType.Float => PropertyTypeInfo.Float,
                ShaderUtil.ShaderPropertyType.Int => PropertyTypeInfo.Int,
                ShaderUtil.ShaderPropertyType.Range => PropertyTypeInfo.Float,
                ShaderUtil.ShaderPropertyType.Vector => PropertyTypeInfo.Vector4,
                _ => PropertyTypeInfo.None,
            };
        }

        private void AddProperty(string propertyName)
        {
            var materialTrack = Wrapper.Track as TimelineMaterialCurveTrack;
            if (materialTrack == null)
                return;
            var timelineActions = materialTrack.timelineActions;
            if (timelineActions == null || timelineActions.Length == 0)
                return;
            var materialCurveClip = timelineActions[0] as TimelineMaterialCurveClip;
            if (materialCurveClip == null)
                return;
            var actor = materialTrack.Actor;
            if (actor == null)
            {
                return;
            }

            var renderer = actor.GetComponentInChildren<Renderer>();
            if (renderer == null)
                return;
            var shader = renderer.sharedMaterial.shader;
            var propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (var i = 0; i < propertyCount; i++)
            {
                var name = ShaderUtil.GetPropertyName(shader, i);
                if (name != propertyName)
                {
                    continue;
                }
                var propertyType = ShaderUtil.GetPropertyType(shader, i);
                var memberCurveClipData = new MemberCurveClipData
                {
                    PropertyName = propertyName,
                    PropertyType = ShaderTypeToCurveType(propertyType)
                };
                materialCurveClip.AddMaterialCurveData(memberCurveClipData);
                EditorUtility.SetDirty(materialCurveClip);
                Undo.RecordObject(materialCurveClip, "Add Material Curve");
                break;
            }
        }

        protected override void AddNewCurveItem(TimelineTrack t, float duration)
        {
            Undo.RegisterCreatedObjectUndo(TimelineFactory.CreateActorClipCurve<TimelineMaterialCurveClip>(t, duration), "Create Material Curve");
        }
    }
}
