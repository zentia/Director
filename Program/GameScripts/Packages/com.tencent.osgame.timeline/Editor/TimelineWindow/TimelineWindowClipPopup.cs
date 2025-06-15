using System;
using System.Collections.Generic;
using TimelineEditorInternal;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace TimelineEditor
{
    [System.Serializable]
    class TimelineWindowClipPopup
    {
        [SerializeField] public TimelineWindowState state;

        static int s_ClipPopupHash = "s_ClipPopupHash".GetHashCode();

        private const float kMenuOffsetMac = 19;

        internal sealed class ClipPopupCallbackInfo
        {
            // The global shared popup state
            public static ClipPopupCallbackInfo instance = null;

            // Name of the command event sent from the popup menu to OnGUI when user has changed selection
            private const string kPopupMenuChangedMessage = "ClipPopupMenuChanged";

            // The control ID of the popup menu that is currently displayed.
            // Used to pass selection changes back again...
            private readonly int m_ControlID = 0;

            // Which item was selected
            private Timeline m_SelectedClip = null;

            // Which view should we send it to.
            private readonly GUIView m_SourceView;

            public ClipPopupCallbackInfo(int controlID)
            {
                m_ControlID = controlID;
                m_SourceView = GUIView.current;
            }

            public static Timeline GetSelectedClipForControl(int controlID, Timeline clip)
            {
                Event evt = Event.current;
                if (evt.type == EventType.ExecuteCommand && evt.commandName == kPopupMenuChangedMessage)
                {
                    if (instance == null)
                    {
                        Debug.LogError("Popup menu has no instance");
                        return clip;
                    }
                    if (instance.m_ControlID == controlID)
                    {
                        clip = instance.m_SelectedClip;
                        instance = null;
                        GUI.changed = true;
                        evt.Use();
                    }
                }
                return clip;
            }

            public static void SetSelectedClip(Timeline clip)
            {
                if (instance == null)
                {
                    Debug.LogError("Popup menu has no instance");
                    return;
                }

                instance.m_SelectedClip = clip;
            }

            public static void SendEvent()
            {
                if (instance == null)
                {
                    Debug.LogError("Popup menu has no instance");
                    return;
                }

                instance.m_SourceView.SendEvent(EditorGUIUtility.CommandEvent(kPopupMenuChangedMessage));
            }
        }


        private void DisplayClipMenu(Rect position, int controlID, Timeline clip)
        {
            Timeline clips = GetOrderedClipList();
            GUIContent[] menuContent = GetClipMenuContent(clips);
            int selected = 0;

            // Center popup menu around button widget
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                position.y = position.y - selected * EditorGUI.kSingleLineHeight - kMenuOffsetMac;
            }

            ClipPopupCallbackInfo.instance = new ClipPopupCallbackInfo(controlID);

            EditorUtility.DisplayCustomMenu(position, menuContent, null, selected, (userData, options, index) =>
            {
                if (index < 1)
                {
                    ClipPopupCallbackInfo.SetSelectedClip(clips);
                }
                else
                {
                    Timeline newClip = TimelineWindowUtility.CreateNewClip(state.selection.rootGameObject.name);
                    if (newClip)
                    {
                        //TimelineWindowUtility.AddClipToAnimationPlayerComponent(state.activeAnimationPlayer, newClip);
                        ClipPopupCallbackInfo.SetSelectedClip(newClip);
                    }
                }

                ClipPopupCallbackInfo.SendEvent();
            }, null);
        }

        // (case 1029160) Modified version of EditorGUI.DoPopup to fit large data list query.
        private Timeline DoClipPopup(Timeline clip, GUIStyle style)
        {
            Rect position = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, style);
            int controlID = GUIUtility.GetControlID(s_ClipPopupHash, FocusType.Keyboard, position);

            clip = ClipPopupCallbackInfo.GetSelectedClipForControl(controlID, clip);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    Font originalFont = style.font;
                    if (originalFont && EditorGUIUtility.GetBoldDefaultFont() && originalFont == EditorStyles.miniFont)
                    {
                        style.font = EditorStyles.miniBoldFont;
                    }

                    GUIContent buttonContent = EditorGUIUtility.TempContent(CurveUtility.GetClipName(clip));
                    buttonContent.tooltip = AssetDatabase.GetAssetPath(clip);

                    style.Draw(position, buttonContent, controlID, false);

                    style.font = originalFont;
                    break;
                case EventType.MouseDown:
                    if (evt.button == 0 && position.Contains(evt.mousePosition))
                    {
                        DisplayClipMenu(position, controlID, clip);
                        GUIUtility.keyboardControl = controlID;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(controlID))
                    {
                        DisplayClipMenu(position, controlID, clip);
                        evt.Use();
                    }
                    break;
            }

            return clip;
        }

        public void OnGUI()
        {
            if (state.selection.canChangeAnimationClip)
            {
                EditorGUI.BeginChangeCheck();
                var newClip = DoClipPopup(state.activeTimeline, AnimationWindowStyles.animClipToolbarPopup);
                if (EditorGUI.EndChangeCheck())
                {
                    state.activeTimeline = newClip;

                    //  Layout has changed, bail out now.
                    EditorGUIUtility.ExitGUI();
                }
            }
            else if (state.activeAnimationClip != null)
            {
                Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, TimelineWindowStyles.toolbarLabel);
                EditorGUI.LabelField(r, CurveUtility.GetClipName(state.activeTimeline), TimelineWindowStyles.toolbarLabel);
            }
        }

        private GUIContent[] GetClipMenuContent(Timeline clips)
        {
            int size = 1;
            if (state.selection.canCreateClips)
                size += 2;

            GUIContent[] content = new GUIContent[size];
            content[0] = new GUIContent(CurveUtility.GetClipName(clips));

            if (state.selection.canCreateClips)
            {
                content[content.Length - 2] = GUIContent.none;
                content[content.Length - 1] = TimelineWindowStyles.createNewClip;
            }

            return content;
        }

        private Timeline GetOrderedClipList()
        {
            Timeline clips = null;
            if (state.activeRootGameObject != null)
                clips = state.activeRootGameObject.GetComponent<Timeline>();

            return clips;
        }

        private int ClipToIndex(Timeline clips, AnimationClip clip)
        {
            
            return 0;
        }
    }
}
