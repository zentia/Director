using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CinemaDirector
{
    public class DirectorInspector : OdinEditorWindow
    {
        protected override object GetTarget()
        {
            var activeObject = DirectorWindow.GetSelection().activeObject;
            titleContent = new GUIContent(activeObject.GetType().Name);
            return activeObject;
        }

        protected override IEnumerable<object> GetTargets()
        {
            if (DirectorWindow.directorControl.InMultiMode)
            {
                return DirectorWindow.GetSelection().objects;
            }
            return base.GetTargets();
        }

        public static DirectorInspector CreateInspectorWindow()
        {
            var window = CreateInstance<DirectorInspector>();
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            EditorUtility.SetDirty(window);
            window.Show();
            return window;
        }
    }
}