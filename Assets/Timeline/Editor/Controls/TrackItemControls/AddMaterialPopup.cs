using System;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    internal class AddMaterialPopup : EditorWindow
    {
        private static AddMaterialPopup s_AddMaterialPopup;

        private static Vector2 windowSize = new Vector2(240, 250);

        private string m_Name = "";

        private Action<string> m_Action;

        private void Init(Rect buttonRect)
        {
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);
            ShowAsDropDown(buttonRect, windowSize);
        }

        public static bool ShowAtPosition(Rect rect, Action<string> callback)
        {
            Event.current.Use();
            if (s_AddMaterialPopup == null)
                s_AddMaterialPopup = CreateInstance<AddMaterialPopup>();
            s_AddMaterialPopup.Init(rect);
            s_AddMaterialPopup.m_Action = callback;
            return true;
        }

        internal void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                return;
            }
            
            var rect = new Rect(1, 1, 40, 20);
            GUI.Label(rect, "Name");
            rect.x += rect.width;
            rect.width *= 2;
            m_Name = TextField(rect, m_Name);
            rect.x += rect.width;
            if (GUI.Button(rect, "OK"))
            {
                m_Action.Invoke(m_Name);
                Close();
            }
        }

        private static string HandleCopyPaste(int controlId)
        {
            if (controlId == GUIUtility.keyboardControl)
            {
                if (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command)
                {
                    if (Event.current.keyCode == KeyCode.C)
                    {
                        Event.current.Use();
                        var editor =
                            (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.Copy();
                    }
                    else if (Event.current.keyCode == KeyCode.V)
                    {
                        Event.current.Use();
                        var editor =
                            (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.Paste();
                        return editor.text;
                    }
                }
            }

            return null;
        }

        private static string TextField(Rect rect, string value)
        {
            var textFieldId = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
            if (textFieldId == 0)
            {
                return value;
            }

            value = HandleCopyPaste(textFieldId) ?? value;
            return GUI.TextField(rect, value);
        }
    }
}
