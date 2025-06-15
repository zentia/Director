using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    [CustomEditor(typeof(Timeline))]
    public class TimelineInspector:Editor
    {
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                TimelineWindow.TimelineWindowRepaint();
            }
            if (GUILayout.Button("Open Timeline"))
            {
                TimelineWindow.ShowWindow();
            }
        }
    }
}
