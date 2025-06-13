using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    [CustomEditor(typeof(PlayCameraAnimationAction))]
    public class PlayCameraAnimationActionInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                TimelineWindow.TimelineWindowRepaint();
            }
            if (GUILayout.Button("FixLength"))
            {
                (target as PlayCameraAnimationAction).FixLength();
                TimelineWindow.TimelineWindowRepaint();
            }
        }
    }
}
