using TimelineRuntime;
using UnityEditor;

namespace TimelineEditor
{
    [CustomEditor(typeof(TimelineItem))]
    public class TimelineItemInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            if (DrawDefaultInspector())
            {
                TimelineWindow.TimelineWindowRepaint();
            }
        }
    }
}
