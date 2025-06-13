using TimelineRuntime;
using UnityEditor;

namespace TimelineEditor
{
    [CustomEditor(typeof(TimelineAction))]
    public class TimelineActionInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    if ("duration" == iterator.propertyPath)
                    {
                        iterator.floatValue = TimelineUtility.FrameToTime(EditorGUILayout.IntField(iterator.displayName, TimelineUtility.TimeToFrame(iterator.floatValue)));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                TimelineWindow.TimelineWindowRepaint();
            }
        }
    }
}
