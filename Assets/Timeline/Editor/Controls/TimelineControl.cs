using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Plugins.Common;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineControl : TimeArea
    {
        public static Texture frameForwardButton;
        public static Texture frameBackwardButton;
        private readonly TimelineControlState m_TimelineState = new ();
        private readonly int frameRate = 30;
        private readonly List<TrackItemControl> m_TrackItemControls = new ();

        public readonly List<TimelineTrackGroupControl> Children = new ();
        public bool IsEditing => Children.Any(child => child.IsEditing);
        private Rect _bodyArea;
        private GUISkin customSkin;
        private bool hasLayoutChanged = true;
        private Rect headerArea;
        private bool isBoxSelecting;
        private Vector2 mouseDownPosition = Vector2.zero;
        private Texture pauseButton;
        private Texture playButton;
        private Rect previousControlArea;
        private Texture scrubDurationHead;
        private Texture scrubHead;
        private Rect selectionBox;
        private Rect sidebarControlArea;
        private Texture stopButton;

        public TimelineWrapper Wrapper;
        private Rect timeRuleArea;
        private Rect trackBodyBackground;
        private Rect trackBodyBackgroundNoScrollbars;
        private Rect trackBodyBackgroundNoVerticalScrollbar;
        private float trackHeaderAreaWidth = 256f;
        private Rect verticalScrollbarArea;
        private float verticalScrollValue;
        private int m_BodyControlID;

        public TimelineControl()
        {
            rect = new Rect();
            frameRate = 30;
            margin = 20f;
            settings = new TimelineControlSettings
            {
                HorizontalRangeMin = 0f
            };
        }

        public ResizeOption ResizeOption
        {
            get { return m_TimelineState.ResizeOption; }
            set { m_TimelineState.ResizeOption = value; }
        }

        public bool InPreviewMode
        {
            get { return m_TimelineState.IsInPreviewMode; }
            set
            {
                if (Wrapper != null)
                {
                    if (!m_TimelineState.IsInPreviewMode & value)
                        EnterPreviewMode();
                    else if (m_TimelineState.IsInPreviewMode && !value)
                        ExitPreviewMode();
                }

                m_TimelineState.IsInPreviewMode = value;
            }
        }

        public event TimelineDragHandler DragPerformed;
        public Action EnterPreviewMode;
        public Action ExitPreviewMode;
        public event TimelineEventHandler PauseTimeline;
        public event TimelineEventHandler PlayTimeline;
        public Action<short> ScrubTimeline;
        public Action<short> SetTimelineFrame;
        public Action Repaint;

        private void BindControls()
        {
            var newSidebarControls = new List<SidebarControl>();
            var removedSidebarControls = new List<SidebarControl>();
            var newTimelineControls = new List<TrackItemControl>();
            var removedTimelineControls = new List<TrackItemControl>();
            BindTrackGroupControls(Wrapper, newSidebarControls, removedSidebarControls, newTimelineControls, removedTimelineControls);
            foreach (var control in newSidebarControls)
            {
                control.timelineControl = this;
                control.DuplicateRequest += SidebarControlDuplicate;
                control.SelectRequest = SidebarControlSelectRequest;
            }

            foreach (var local2 in removedSidebarControls)
            {
                local2.DuplicateRequest -= SidebarControlDuplicate;
                local2.SelectRequest = null;
            }

            foreach (var control in newTimelineControls)
            {
                m_TrackItemControls.Add(control);
                control.timelineControl = this;
                control.RequestTrackItemTranslate += ItemControlRequestTrackItemTranslate;
                control.TrackItemTranslate += ItemControlTrackItemTranslate;
                control.TrackItemUpdate += ItemControlTrackItemUpdate;
            }

            foreach (var control2 in removedTimelineControls)
            {
                m_TrackItemControls.Remove(control2);
                control2.RequestTrackItemTranslate -= ItemControlRequestTrackItemTranslate;
                control2.TrackItemTranslate -= ItemControlTrackItemTranslate;
                control2.TrackItemUpdate -= ItemControlTrackItemUpdate;
            }
        }

        private void BindTrackGroupControls(TimelineWrapper timelineWrapper, List<SidebarControl> newSidebarControls, List<SidebarControl> removedSidebarControls, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
        {
            if (timelineWrapper.HasChanged)
            {
                var timelineTrackGroupWrappers = timelineWrapper.TimelineTrackGroupWrappers;
                for (var i = timelineTrackGroupWrappers.Count - 1; i >= 0; i--)
                {
                    var timelineTrackGroupWrapper = timelineWrapper.TimelineTrackGroupWrappers[i];
                    if (timelineTrackGroupWrapper.Data == null)
                    {
                        timelineTrackGroupWrappers.RemoveAt(i);
                        continue;
                    }
                    if (Children.Find(item=>timelineTrackGroupWrapper==item.Wrapper) == null)
                    {
                        var type = typeof(TimelineTrackGroupControl);
                        var num2 = 0x7fffffff;
                        foreach (var type2 in TimelineControlHelper.GetAllSubTypes(typeof(TimelineTrackGroupControl)))
                        {
                            Type c = null;
                            foreach (TimelineTrackGroupControlAttribute attribute in type2.GetCustomAttributes(
                                         typeof(TimelineTrackGroupControlAttribute), true))
                                if (attribute != null)
                                    c = attribute.TrackGroupType;
                            if (c == timelineTrackGroupWrapper.Data.GetType())
                            {
                                type = type2;
                                break;
                            }

                            if (timelineTrackGroupWrapper.Data.GetType().IsSubclassOf(c))
                            {
                                var baseType = timelineTrackGroupWrapper.Data.GetType();
                                var num5 = 0;
                                while (baseType != null && baseType != c)
                                {
                                    baseType = baseType.BaseType;
                                    num5++;
                                }

                                if (num5 <= num2)
                                {
                                    num2 = num5;
                                    type = type2;
                                }
                            }
                        }

                        TimelineTrackGroupControl timelineTrackGroupControl = type == typeof(ActorTrackGroupControl) ? new ActorTrackGroupControl(timelineTrackGroupWrapper) : new DirectorGroupControl(timelineTrackGroupWrapper);
                        timelineTrackGroupControl.Initialize();
                        newSidebarControls.Add(timelineTrackGroupControl);
                        Children.Insert(0, timelineTrackGroupControl);
                    }
                }
                Children.RemoveAll(item=>!timelineWrapper.TimelineTrackGroupWrappers.Contains(item.Wrapper));
                timelineWrapper.HasChanged = false;
            }

            foreach (var wrapper6 in Children)
                wrapper6.BindTrackControls(wrapper6.Wrapper, newSidebarControls, removedSidebarControls,
                    newTimelineControls, removedTimelineControls);
        }

        public void ControlDeleteRequest(object sender, TimelineBehaviourControlEventArgs e)
        {
            foreach (var control in Children)
            {
                control.DeleteSelectedChildren();
                if (control.IsSelected) control.Delete();
            }
        }

        private void DrawBackground()
        {
            var style = GUI.skin.FindStyle("AnimationCurveEditorBackground") != null ? "AnimationCurveEditorBackground" : "CurveEditorBackground";
            GUI.Box(trackBodyBackground, GUIContent.none, style);
            rect = trackBodyBackgroundNoVerticalScrollbar;
            BeginViewGUI(false);
            SetTickMarkerRanges();
            DrawMajorTicks(trackBodyBackground, frameRate);
            EndViewGUI();
        }

        private float GetTrackGroupsHeight()
        {
            var num = 0f;
            foreach (var control in Children)
                num += control.GetHeight();
            return num;
        }

        private float ItemControlRequestTrackItemTranslate(object sender, TrackItemEventArgs e)
        {
            var firetime = e.FireTime;
            var b = e.FireTime;
            var flag = false;
            while (!flag && b != 0f)
            {
                foreach (var control in m_TrackItemControls)
                    if (control.IsSelected)
                    {
                        if (e.FireTime > 0f)
                            b = Mathf.Min(control.RequestTranslate(firetime), b);
                        else
                            b = Mathf.Max(control.RequestTranslate(firetime), b);
                    }
                if (b != firetime)
                    firetime = b;
                else
                    flag = true;
            }

            return firetime;
        }

        private float ItemControlTrackItemTranslate(object sender, TrackItemEventArgs e)
        {
            foreach (var control in m_TrackItemControls)
                if (control.IsSelected)
                    control.Translate(e.FireTime);
            Repaint();
            return 0f;
        }

        private void ItemControlTrackItemUpdate(object sender, TrackItemEventArgs e)
        {
            foreach (var control in m_TrackItemControls)
                if (control.IsSelected)
                    control.ConfirmTranslate();
        }

        public void OnDisable()
        {
            EditorPrefs.SetFloat("TimelineControl.areaX", shownAreaInsideMargins.x);
            EditorPrefs.SetFloat("TimelineControl.areaWidth", shownAreaInsideMargins.width);
            EditorPrefs.SetFloat("TimelineControl.SidebarWidth", trackHeaderAreaWidth);
        }

        public void OnGUI(Rect controlArea)
        {
            UpdateControlLayout(controlArea);
            DrawBackground();
            UpdateTimelineHeader(headerArea, timeRuleArea);
            if (Wrapper == null)
            {
                return;
            }
            BindControls();
            UpdateControlState();
            var bottomValue = GetTrackGroupsHeight();
            if (Event.current.type == EventType.ScrollWheel)
            {
                verticalScrollValue += 17f * Event.current.delta.y / 3f;
                Repaint();
            }
            verticalScrollValue = GUI.VerticalScrollbar(verticalScrollbarArea, verticalScrollValue, Mathf.Min(_bodyArea.height, bottomValue), 0f, bottomValue);
            var vector = new Vector2(Translation.x, verticalScrollValue);
            Translation = vector;
            var area = new Rect(_bodyArea.x, -Translation.y, _bodyArea.width, bottomValue);
            m_TimelineState.Translation = Translation;
            m_TimelineState.Scale = Scale;
            GUILayout.BeginArea(_bodyArea, string.Empty);
            UpdateChildren(area);
            UpdateDurationBar();
            GUILayout.EndArea();
            UpdateScrubber();
            BeginViewGUI(true);
            UpdateUserInput(_bodyArea);
            UpdateDragAndDrop();
        }

        public void OnLoad(GUISkin skin)
        {
            customSkin = skin;
            var min = 0f;
            var areaWidth = 60f;
            if (EditorPrefs.HasKey("TimelineControl.areaX"))
                min = EditorPrefs.GetFloat("TimelineControl.areaX");
            if (EditorPrefs.HasKey("TimelineControl.areaWidth"))
                areaWidth = EditorPrefs.GetFloat("TimelineControl.areaWidth");
            SetShownHRangeInsideMargins(min, min + areaWidth);
            if (EditorPrefs.HasKey("TimelineControl.SidebarWidth"))
                trackHeaderAreaWidth = EditorPrefs.GetFloat("TimelineControl.SidebarWidth");
            if (playButton == null)
                playButton = Resources.Load("Director_PlayIcon") as Texture;
            if (playButton == null)
                Log.LogE(LogTag.Timeline, "Play button icon missing from Resources folder.");
            if (pauseButton == null)
                pauseButton = Resources.Load("Director_PauseIcon") as Texture;
            if (pauseButton == null)
                Log.LogE(LogTag.Timeline, "Pause button missing from Resources folder.");
            if (stopButton == null)
                stopButton = Resources.Load("Director_StopIcon") as Texture;
            if (stopButton == null)
                Log.LogE(LogTag.Timeline, "Stop button icon missing from Resources folder.");
            if (frameForwardButton == null)
                frameForwardButton = Resources.Load("Director_FrameForwardIcon") as Texture;
            if (frameBackwardButton == null)
                frameBackwardButton = Resources.Load("Director_FrameBackwardIcon") as Texture;
            if (scrubHead == null)
                scrubHead = Resources.Load("Director_Playhead") as Texture;
            if (scrubHead == null)
                Log.LogE(LogTag.Timeline, "Director_Playhead missing from Resources folder.");
            if (scrubDurationHead == null)
                scrubDurationHead = Resources.Load("Director_Duration_Playhead") as Texture;
            if (scrubDurationHead == null)
                Log.LogE(LogTag.Timeline, "Director_Duration_Playhead missing from Resources folder.");
            if (customSkin != null)
            {
                TimelineControlStyles.BoxSelect = customSkin.FindStyle("BoxSelect");
                TimelineControlStyles.UpArrowIcon = customSkin.FindStyle("UpArrowIcon");
                TimelineControlStyles.DownArrowIcon = customSkin.FindStyle("DownArrowIcon");
            }
            else
            {
                TimelineControlStyles.BoxSelect = "box";
                TimelineControlStyles.UpArrowIcon = "box";
                TimelineControlStyles.DownArrowIcon = "box";
            }

            TimelineTrackGroupControl.InitStyles(customSkin);
            TimelineTrackControl.InitStyles(customSkin);
        }

        public void Rescale()
        {
            SetShownHRangeInsideMargins(0f, Wrapper?.Duration ?? 60f);
        }

        private void SidebarControlDuplicate(object sender, SidebarControlEventArgs e)
        {
            foreach (var control in Children)
            {
                control.DuplicateSelectedChildren();
                if (control.IsSelected)
                    control.Duplicate();
            }
        }

        private void SidebarControlSelectRequest(object sender, SidebarControlEventArgs e)
        {
            var behaviour = e.Behaviour;
            if (behaviour != null)
            {
                if (Event.current.control)
                {
                    if (Selection.Contains(behaviour.gameObject))
                    {
                        var gameObjects = Selection.gameObjects;
                        ArrayUtility.Remove(ref gameObjects, behaviour.gameObject);
                        Selection.objects = gameObjects;
                    }
                    else
                    {
                        var gameObjects = Selection.gameObjects;
                        ArrayUtility.Add(ref gameObjects, behaviour.gameObject);
                        Selection.objects = gameObjects;
                    }
                }
                else if (Event.current.shift)
                {
                    var list = new List<SidebarControl>();
                    foreach (var item in Children)
                    {
                        list.Add(item);
                        list.AddRange(item.GetSidebarControlChildren(true));
                    }

                    foreach (var control5 in list)
                    {
                        if (!control5.IsSelected)
                            control5.Select();
                    }
                }
                else
                {
                    Selection.activeObject = behaviour.gameObject;
                }
                Event.current.Use();
            }
        }

        private void UpdateControlLayout(Rect controlArea)
        {
            hasLayoutChanged = controlArea != previousControlArea;
            headerArea = new Rect(controlArea.x, controlArea.y, controlArea.width, 17f);
            sidebarControlArea = new Rect(trackHeaderAreaWidth, headerArea.y + 17f, 4f, controlArea.height - 17f - 15f);
            EditorGUIUtility.AddCursorRect(sidebarControlArea, MouseCursor.ResizeHorizontal);
            var controlId = GUIUtility.GetControlID("SidebarResize".GetHashCode(), FocusType.Passive, sidebarControlArea);
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (sidebarControlArea.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                        GUIUtility.hotControl = 0;
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        trackHeaderAreaWidth = Mathf.Clamp(Event.current.mousePosition.x, 256f, 512f);
                        hasLayoutChanged = true;
                    }
                    break;
            }
            if (hasLayoutChanged)
            {
                timeRuleArea = new Rect(trackHeaderAreaWidth + sidebarControlArea.width, controlArea.y, controlArea.width - trackHeaderAreaWidth - 15f - sidebarControlArea.width, 17f);
                _bodyArea = new Rect(controlArea.x, headerArea.y + 17f, controlArea.width - 15f, controlArea.height - 17f - 15f);
                trackBodyBackground = new Rect(controlArea.x + trackHeaderAreaWidth + sidebarControlArea.width, _bodyArea.y, controlArea.width - 15f - trackHeaderAreaWidth - sidebarControlArea.width, controlArea.height - 17f - 15f);
                trackBodyBackgroundNoVerticalScrollbar = new Rect(controlArea.x + trackHeaderAreaWidth + sidebarControlArea.width, _bodyArea.y, controlArea.width - 15f - trackHeaderAreaWidth - sidebarControlArea.width, controlArea.height - 17f);
                m_BodyControlID = GUIUtility.GetControlID("DirectorBody".GetHashCode(), FocusType.Passive, trackBodyBackgroundNoVerticalScrollbar);
                trackBodyBackgroundNoScrollbars = new Rect(controlArea.x + trackHeaderAreaWidth + sidebarControlArea.width, _bodyArea.y, controlArea.width - 15f - trackHeaderAreaWidth - sidebarControlArea.width, controlArea.height - 17f - 15f);
                verticalScrollbarArea = new Rect(_bodyArea.x + _bodyArea.width, _bodyArea.y, 15f, controlArea.height - 17f - 15f);
                Repaint();
            }
            previousControlArea = controlArea;
        }

        private void UpdateControlState()
        {
            HScrollMax = Wrapper.Duration;
            m_TimelineState.position = Wrapper.RunningTime;
        }

        private void UpdateDragAndDrop()
        {
            var current = Event.current;
            if (_bodyArea.Contains(current.mousePosition))
                switch (current.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        break;
                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        DragPerformed?.Invoke(this, new TimelineDragArgs(Wrapper.timeline, DragAndDrop.objectReferences));
                        current.Use();
                        break;
                }
        }

        private void UpdateDurationBar()
        {
            var x = m_TimelineState.TimeToPosition(Wrapper.Duration) + trackBodyBackground.x;
            var color = GUI.color;
            GUI.color = new Color(0.25f, 0.5f, 0.5f);
            var position = new Rect(x - 8f, _bodyArea.height - 13f, 16f, 16f);
            var controlId = GUIUtility.GetControlID("DurationBar".GetHashCode(), FocusType.Passive, position);
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                        GUIUtility.hotControl = 0;
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        var mousePosition = Event.current.mousePosition;
                        mousePosition.x -= trackBodyBackground.x;
                        Undo.RecordObject(Wrapper.timeline, "Changed timeline Duration");
                        Wrapper.Duration = ViewToDrawingTransformPoint(mousePosition).x;
                        Event.current.Use();
                    }
                    break;
            }
            if (x > trackBodyBackground.x && x < _bodyArea.width)
                GUI.DrawTexture(position, scrubDurationHead);
            GUI.color = color;
            Handles.color = new Color(0.25f, 0.5f, 0.5f);
            if (x > trackBodyBackground.x)
            {
                Handles.DrawLine(new Vector3(x, 0f, 0f), new Vector2(x, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height - 13f));
                Handles.DrawLine(new Vector3(x + 1f, 0f, 0f), new Vector2(x + 1f, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height - 13f));
            }
        }

        private void UpdateScrubber()
        {
            if (Event.current.type == EventType.Repaint && (InPreviewMode || Wrapper.IsPlaying))
            {
                var x = m_TimelineState.TimeToPosition(Wrapper.RunningTime) + trackBodyBackground.x;
                var tempColor = GUI.color;
                GUI.color = new Color(1f, 0f, 0f, 1f);
                Handles.color = new Color(1f, 0f, 0f, 1f);
                if (x > trackBodyBackground.x && x < _bodyArea.width)
                {
                    GUI.DrawTexture(new Rect(x - 8f, 20f, 16f, 16f), scrubHead);
                    Handles.DrawLine(new Vector2(x, 34f), new Vector2(x, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height + 3f));
                    Handles.DrawLine(new Vector2(x + 0.5f, 34f), new Vector2(x + 0.5f, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height + 3f));
                }
                GUI.color = tempColor;
            }
        }

        private void UpdateTimelineHeader(Rect headerArea, Rect timeRulerArea)
        {
            GUILayout.BeginArea(headerArea, string.Empty, EditorStyles.toolbarButton);
            UpdateToolbar();
            GUILayout.BeginArea(timeRulerArea, string.Empty, EditorStyles.toolbarButton);
            GUILayout.EndArea();
            GUILayout.EndArea();
            TimeRuler(timeRulerArea, frameRate);
            if (Wrapper != null)
            {
                var controlId = GUIUtility.GetControlID("TimeRuler".GetHashCode(), FocusType.Passive, timeRulerArea);
                switch (Event.current.GetTypeForControl(controlId))
                {
                    case EventType.MouseDown:
                        if (timeRulerArea.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.hotControl = controlId;
                            var mousePosition = Event.current.mousePosition;
                            mousePosition.x -= timeRulerArea.x;
                            InPreviewMode = true;
                            m_TimelineState.position = Mathf.Max(ViewToDrawingTransformPoint(mousePosition).x, 0f);
                            if (Wrapper == null)
                                return;
                            SetTimelineFrame?.Invoke(m_TimelineState.scrubberPositionFrame);
                        }
                        return;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlId)
                        {
                            GUIUtility.hotControl = 0;
                            if (Wrapper == null)
                                return;
                            PauseTimeline(this, new TimelineArgs(Wrapper.timeline));
                        }
                        return;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlId)
                        {
                            var mousePosition = Event.current.mousePosition;
                            mousePosition.x -= timeRulerArea.x;
                            m_TimelineState.position = Mathf.Max(ViewToDrawingTransformPoint(mousePosition).x, 0f);
                            if (Wrapper != null)
                            {
                                ScrubTimeline?.Invoke(m_TimelineState.scrubberPositionFrame);
                            }
                            Event.current.Use();
                        }
                        return;
                }
                if (GUIUtility.hotControl == controlId)
                {
                    var mousePosition = Event.current.mousePosition;
                    mousePosition.x -= timeRulerArea.x;
                    m_TimelineState.position = Mathf.Max(ViewToDrawingTransformPoint(mousePosition).x, 0f);
                    if (Wrapper != null)
                    {
                        ScrubTimeline?.Invoke(m_TimelineState.scrubberPositionFrame);
                    }
                }
            }
        }

        private void UpdateToolbar()
        {
            var options = new[] { GUILayout.MaxWidth(150f) };
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, options);
            GUILayout.FlexibleSpace();
            if (Wrapper != null && Wrapper.IsPlaying)
            {
                if (GUILayout.Button(pauseButton, EditorStyles.toolbarButton))
                    PauseTimeline(this, new TimelineArgs(Wrapper.timeline));
            }
            else if (GUILayout.Button(playButton, EditorStyles.toolbarButton) && Wrapper != null)
            {
                InPreviewMode = true;
                PlayTimeline(this, new TimelineArgs(Wrapper.timeline));
            }

            if (GUILayout.Button(stopButton, EditorStyles.toolbarButton) && Wrapper != null)
            {
                InPreviewMode = false;
            }

            GUILayout.FlexibleSpace();
            if (Event.current.type == EventType.KeyDown && !EditorGUIUtility.editingTextField && Event.current.keyCode == KeyCode.Space)
            {
                if (!Wrapper.IsPlaying)
                {
                    InPreviewMode = true;
                    PlayTimeline(this, new TimelineArgs(Wrapper.timeline));
                }
                else
                {
                    PauseTimeline(this, new TimelineArgs(Wrapper.timeline));
                }

                Event.current.Use();
            }
            short frameCount = 0;
            if (Wrapper != null)
                frameCount = Wrapper.timeline.GetFrameCount();
            GUILayout.Space(10f);
            frameCount = (short)EditorGUILayout.IntField(frameCount, GUILayout.Width(50f));
            if (Wrapper != null && frameCount != Wrapper.timeline.GetFrameCount())
            {
                InPreviewMode = true;
                m_TimelineState.position = frameCount/30.0f;
                SetTimelineFrame(m_TimelineState.scrubberPositionFrame);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateChildren(Rect area)
        {
            var y = area.y;
            for (var i = 0; i < Children.Count; i++)
            {
                var control = Children[i];
                if (control.Wrapper.Data == null)
                {
                    Wrapper.HasChanged = true;
                    continue;
                }
                var height = control.GetHeight();
                var position = new Rect(area.x, y, area.width, height);
                var fullHeader = new Rect(area.x, y, trackHeaderAreaWidth + sidebarControlArea.width, height);
                var safeHeader = new Rect(area.x, y, trackHeaderAreaWidth - 32f, height);
                var rect4 = new Rect(safeHeader.x + safeHeader.width, y, 16f, 16f);
                var content = new Rect(fullHeader.x + fullHeader.width, y, area.width - fullHeader.width, height);
                control.Update(m_TimelineState, position, fullHeader, safeHeader, content);
                if (i != 0 && GUI.Button(rect4, string.Empty, TimelineControlStyles.UpArrowIcon))
                {
                    control.Wrapper.Data.transform.SetSiblingIndex(i - 1);
                }
                if (i != Children.Count && GUI.Button(new Rect(rect4.x + 16f, y, 16f, 16f), string.Empty, TimelineControlStyles.DownArrowIcon))
                {
                    control.Wrapper.Data.transform.SetSiblingIndex(i + 1);
                }
                y += height;
            }
        }

        private void UpdateUserInput(Rect area)
        {
            switch (Event.current.GetTypeForControl(m_BodyControlID))
            {
                case EventType.MouseDown:
                    if (trackBodyBackgroundNoVerticalScrollbar.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        isBoxSelecting = true;
                        mouseDownPosition = Event.current.mousePosition;
                        Selection.activeObject = null;
                        GUIUtility.hotControl = m_BodyControlID;
                        foreach (var control in Children)
                        {
                            control.ClearSelectCurves();
                        }
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == m_BodyControlID)
                    {
                        isBoxSelecting = false;
                        selectionBox = new Rect();
                        GUIUtility.hotControl = 0;
                        Repaint();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == m_BodyControlID)
                    {
                        var b = Mathf.Clamp(Event.current.mousePosition.x, trackBodyBackgroundNoScrollbars.x, trackBodyBackgroundNoScrollbars.xMax);
                        var num3 = Mathf.Clamp(Event.current.mousePosition.y, trackBodyBackgroundNoScrollbars.y, trackBodyBackgroundNoScrollbars.yMax);
                        var x = Mathf.Min(mouseDownPosition.x, b);
                        var width = Mathf.Abs(b - mouseDownPosition.x);
                        var y = Mathf.Min(mouseDownPosition.y, num3);
                        var height = Mathf.Abs(mouseDownPosition.y - num3);
                        this.selectionBox = new Rect(x, y, width, height);
                        var selectionBox = new Rect(this.selectionBox);
                        selectionBox.y -= 34f;
                        foreach (var wrapper in Children)
                            wrapper.BoxSelect(selectionBox);
                        var r = new Rect(mouseDownPosition.x, mouseDownPosition.y, Event.current.mousePosition.x - mouseDownPosition.x, Event.current.mousePosition.y - mouseDownPosition.y);
                        if (r.width < 0)
                        {
                            r.x += r.width;
                            r.width = -r.width;
                        }
                        if (r.height < 0)
                        {
                            r.y += r.height;
                            r.height = -r.height;
                        }
                        foreach (var control in Children)
                        {
                            control.ZoomSelectKey(r, area);
                        }
                        Event.current.Use();
                    }
                    break;
            }
            if (isBoxSelecting)
                GUI.Box(selectionBox, GUIContent.none, TimelineControlStyles.BoxSelect);
        }

        public void ZoomIn()
        {
            Scale *= 1.5f;
        }

        public void ZoomOut()
        {
            Scale *= 0.75f;
        }
    }
}
