using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TagAttributeDrawer : OdinAttributeDrawer<TagAttribute, string>
    {
        private int index;
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            index = EditorGUILayout.Popup("Tag", index, tags);
            ValueEntry.SmartValue = tags[index];
        }
    }
}