using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    internal class AssetAttributeDrawer : OdinAttributeDrawer<AssetAttribute, string>
    {
        private Object m_Asset;
        private const string CustomDir = "Assets/CustomResources/";
        private const string Extension = ".prefab";
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (m_Asset == null && !string.IsNullOrEmpty(ValueEntry.SmartValue))
            {
                m_Asset = AssetDatabase.LoadAssetAtPath<Object>(CustomDir + ValueEntry.SmartValue + Extension);
            }
            var asset = EditorGUILayout.ObjectField(label.text, m_Asset, typeof(GameObject), false);
            if (asset != m_Asset)
            {
                m_Asset = asset;
                if (asset == null)
                {
                    ValueEntry.SmartValue = "";
                }
                else
                {
                    var path = AssetDatabase.GetAssetPath(asset);
                    if (!path.StartsWith(CustomDir))
                    {
                        m_Asset = null;
                        ValueEntry.SmartValue = "";
                        return;
                    }
                    var idx = CustomDir.Length;
                    ValueEntry.SmartValue = path.Substring(idx, path.Length - idx - Extension.Length);
                }
            }
            else if (asset == null)
            {
                ValueEntry.SmartValue = "";
            }
        }
    }
}
