using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    internal class AnimationClipAttributeDrawer : OdinAttributeDrawer<AnimationClipAttribute, string>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.Popup(0, new string[] { ""});
        }
    }
}
