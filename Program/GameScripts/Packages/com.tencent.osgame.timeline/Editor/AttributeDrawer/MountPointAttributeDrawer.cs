using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class MountPointAttributeDrawer : OdinAttributeDrawer<MountPointAttribute, string>
    {
        private static string TransformToPath(Object parent, Transform child)
        {
            if (parent == child)
            {
                return "";
            }
            var path = "";
            while (child && child != parent)
            {
                if (path == "")
                {
                    path = child.name;
                }
                else
                {
                    path = child.name + "/" + path;
                }
                child = child.parent;
            }

            return path;
        }


        protected override void DrawPropertyLayout(GUIContent label)
        {
            var activeGameObject = Selection.activeGameObject;
            if (activeGameObject != null)
            {
                var timelineItem = activeGameObject.GetComponent<TimelineItem>();
                if (timelineItem != null)
                {
                    var trackGroup = timelineItem.timelineTrack.trackGroup;
                    var actor = trackGroup.timeline.GetActor(trackGroup.name);
                    if (actor != null)
                    {
                        var mountPoint = actor.Find(ValueEntry.SmartValue);
                        var newMountPoint = EditorGUILayout.ObjectField(label,  mountPoint, typeof(Transform), true) as Transform;
                        if (newMountPoint != mountPoint)
                        {
                            ValueEntry.SmartValue = newMountPoint == null ? "" : TransformToPath(actor, newMountPoint);
                        }
                        return;
                    }
                }
            }
            ValueEntry.SmartValue = EditorGUILayout.TextField(label, ValueEntry.SmartValue);
        }
    }
}
