using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    internal class AssetAttributeDrawer : OdinAttributeDrawer<AssetAttribute, string>
    {
        private UnityEngine.Object m_Asset;
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (m_Asset == null && !string.IsNullOrEmpty(ValueEntry.SmartValue))
            {
                m_Asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ValueEntry.SmartValue);
            }
            var asset = EditorGUILayout.ObjectField(label, m_Asset, typeof(GameObject), false);
            if (asset != m_Asset)
            {
                ValueEntry.SmartValue = AssetDatabase.GetAssetPath(asset);
            }
        }
    }
}
