using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    internal class AssetAttributeDrawer : OdinAttributeDrawer<AssetAttribute, string>
    {
        private UnityEngine.Object m_Asset;
        private const string c_customDir = "Assets/CustomResources/";
        private const string c_extension = ".prefab";
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (m_Asset == null && !string.IsNullOrEmpty(ValueEntry.SmartValue))
            {
                m_Asset = AssetDatabase.LoadAssetAtPath<Object>(c_customDir + ValueEntry.SmartValue + c_extension);
            }
            var asset = EditorGUILayout.ObjectField(label, m_Asset, typeof(GameObject), false);
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
                    var idx = c_customDir.Length;
                    ValueEntry.SmartValue = path.Substring(idx, path.Length - idx - c_extension.Length);
                }
                
            }
        }
    }
}
