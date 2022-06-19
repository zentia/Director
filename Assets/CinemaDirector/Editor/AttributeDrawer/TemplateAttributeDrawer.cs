using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    internal class TemplateAttributeDrawer : OdinAttributeDrawer<TemplateAttribute, int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
            var templateName = new List<string>();
            DirectorWindow.Instance.cutscene.m_templateObjectList.ForEach(o=>templateName.Add(o.templateObject.name));
            ValueEntry.SmartValue = EditorGUILayout.Popup(ValueEntry.SmartValue,templateName.ToArray());
        }
    }
}
