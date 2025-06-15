using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public abstract class TimelineCurveTrackControl : TimelineTrackControl
    {
        private int m_ControlSelection;

        private TimelineCurveClipItemControl FocusedControl
        {
            get
            {
                if (Children.Count == 0)
                    return null;
                return Children[0] as TimelineCurveClipItemControl;
            }
        }
        private AnimationClip _sourceAnimation;
        private bool _showAniWindow;

        protected override void CalculateHeight()
        {
            if (isExpanded)
            {
                var rowsShowing = 2;
                if (FocusedControl != null)
                {
                    var memberCurves = FocusedControl.ClipCurveWrapper.MemberCurves;
                    rowsShowing += memberCurves.Count;
                    foreach (var timelineMemberCurveWrapper in memberCurves)
                    {
                        rowsShowing += timelineMemberCurveWrapper.AnimationCurves.Count;
                    }
                }
                trackArea.height = 17f * rowsShowing;
            }
            else
                trackArea.height = 17f;
        }

        private void LoadFromAni(AnimationClip clip)
        {
            foreach (TimelineCurveClipItemControl control in Children)
            {
                control.LoadFromAnimation(clip);
            }
        }

        private void NavigateFrameBackward()
        {
            var keys = FocusedControl.keyframeTimes.Keys;
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                if (keys[i] < state.scrubberPositionFrame)
                {
                    timelineControl.Wrapper.timeline.SetRunningTime(TimelineUtility.FrameToTime(keys[i]));
                    break;
                }
            }
        }

        private void NavigateFrameForward()
        {
            var keys = FocusedControl.keyframeTimes.Keys;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] > state.scrubberPositionFrame)
                {
                    timelineControl.Wrapper.timeline.SetRunningTime(TimelineUtility.FrameToTime(keys[i]));
                    break;
                }
            }
        }

        protected void UpdateNavigate(ref Rect rect3)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftArrow)
            {
                NavigateFrameBackward();
                timelineControl.Repaint();
            }
            else if (GUI.Button(rect3, TimelineControl.frameBackwardButton))
            {
                NavigateFrameBackward();
            }
            rect3.x += rect3.width;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.RightArrow)
            {
                NavigateFrameForward();
                timelineControl.Repaint();
            }
            else if (GUI.Button(rect3, TimelineControl.frameForwardButton))
            {
                NavigateFrameForward();
            }
            rect3.x += rect3.width;
        }

        protected virtual void UpdateToolbar(Rect rect3)
        {
            if (_showAniWindow)
            {
                var aniObjField = new Rect(0, rect3.y, rect3.width, rect3.height);
                _sourceAnimation = (AnimationClip)EditorGUI.ObjectField(aniObjField, _sourceAnimation, typeof(AnimationClip), false);
                rect3.x += rect3.width;
                if (GUI.Button(rect3, new GUIContent("Import")))
                {
                    _showAniWindow = false;
                    if (_sourceAnimation != null)
                    {
                        LoadFromAni(_sourceAnimation);
                    }
                }
            }
            else
            {
                UpdateNavigate(ref rect3);
                if (GUI.Button(rect3, new GUIContent("Import")))
                {
                    _showAniWindow = true;
                    _sourceAnimation = null;
                }
                rect3.x += rect3.width;
                if (GUI.Button(rect3, TimelineWindow.ms_AutoKeyFrameImage))
                {
                    if (state.IsInPreviewMode)
                    {
                        var context = new CurvesContext(FocusedControl.Wrapper as TimelineClipCurveWrapper, state.scrubberPositionFrame, state);
                        FocusedControl.AddKeyframe(context);
                    }
                }
            }
        }

        public override void UpdateHeaderContents(Rect position, Rect headerBackground)
        {
            var rect = new Rect(position.x + 14f, position.y, 14f, position.height);
            var rect2 = new Rect(rect.x + rect.width, position.y, position.width - 14f - 96f - 14f, 17f);
            var name = Wrapper.Track.name;
            isExpanded = EditorGUI.Foldout(rect, isExpanded, GUIContent.none, false);
            UpdateHeaderControl(new Rect(position.width - 16f, position.y, 16f, 16f));
            if (isExpanded)
            {
                var rect3 = new Rect(rect2.x + 28f, position.y + 17f, headerBackground.width - rect2.x - 28f, 17f);
                if (FocusedControl != null && FocusedControl.Wrapper != null && FocusedControl.Wrapper.timelineItem != null)
                {
                    var controlHeaderArea = new Rect(rect3.x, rect3.y + rect3.height, rect3.width, position.height - 34f);
                    FocusedControl.UpdateHeaderArea(state, controlHeaderArea);

                    rect3.width /= 5f;
                    UpdateToolbar(rect3);
                }
            }

            if (isRenaming)
            {
                GUI.SetNextControlName("CurveTrackRename");
                name = EditorGUI.TextField(rect2, GUIContent.none, name);
                if (renameRequested)
                {
                    EditorGUI.FocusTextInControl("CurveTrackRename");
                    renameRequested = false;
                }

                if (Event.current.keyCode == KeyCode.Return || (Event.current.type == EventType.MouseDown && !rect2.Contains(Event.current.mousePosition)))
                {
                    isRenaming = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (Wrapper.Track.name != name)
            {
                Undo.RecordObject(Wrapper.Track.gameObject, $"Renamed {Wrapper.Track.name}");
                Wrapper.Track.name = name;
            }

            if (!isRenaming)
            {
                var text = name;
                for (var vector = GUI.skin.label.CalcSize(new GUIContent(text)); vector.x > rect2.width && text.Length > 5; vector = GUI.skin.label.CalcSize(new GUIContent(text)))
                    text = text.Substring(0, text.Length - 4) + "...";

                if (Selection.Contains(Wrapper.Track.gameObject))
                    GUI.Label(rect2, text, EditorStyles.whiteLabel);
                else
                    GUI.Label(rect2, text);

                var controlID = GUIUtility.GetControlID(Wrapper.Track.GetInstanceID(), FocusType.Passive, position);
                if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
                {
                    if (position.Contains(Event.current.mousePosition) && Event.current.button == 1)
                    {
                        if (!IsSelected)
                            RequestSelect();

                        ShowHeaderContextMenu();
                        Event.current.Use();
                    }
                    else if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        RequestSelect();
                        Event.current.Use();
                    }
                }
            }
        }
    }
}
