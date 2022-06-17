using DirectorEditor;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class DirectorControl : TimeArea
{
    private CutsceneWrapper cutscene;
    private bool hasLayoutChanged = true;
    private Dictionary<TrackGroupWrapper, TrackGroupControl> trackGroupBinding = new Dictionary<TrackGroupWrapper, TrackGroupControl>();
    private List<SidebarControl> sidebarControls = new List<SidebarControl>();
    private List<TrackItemControl> timelineControls = new List<TrackItemControl>();
    private DirectorControlState directorState = new DirectorControlState();
    private int frameRate;
    private float verticalScrollValue;
    private Texture playButton;
    private Texture pauseButton;
    private Texture stopButton;
    private Texture frameForwardButton;
    private Texture frameBackwardButton;
    private Texture scrubHead;
    private Texture scrubDurationHead;
    private const float HEADER_HEIGHT = 17f;
    private const float SCROLLBAR_WIDTH = 15f;
    private const float TRACK_HEADER_WIDTH_MIN = 256f;
    private const float TRACK_HEADER_WIDTH_MAX = 512f;
    private const float TRACK_HEADER_ICON_WIDTH = 16f;
    private const float TRACK_HEADER_ICON_HEIGHT = 16f;
    private const float MARGIN = 20f;
    private const float SIDEBAR_WIDTH = 4f;
    private const int FRAME_RATE = 60;
    private float track_header_area_width = 256f;
    private GUISkin customSkin;
    private Rect headerArea;
    private Rect timeRuleArea;
    private Rect bodyArea;
    private Rect trackBodyBackground;
    private Rect trackBodyBackgroundNoVerticalScrollbar;
    private Rect trackBodyBackgroundNoScrollbars;
    private Rect verticalScrollbarArea;
    private Rect previousControlArea;
    private Rect sidebarControlArea;
    private Vector2 mouseDownPosition = Vector2.zero;
    private bool isBoxSelecting;
    private Rect selectionBox;

    [field: CompilerGenerated]
    public event DirectorDragHandler DragPerformed;

    [field: CompilerGenerated]
    public event CutsceneEventHandler EnterPreviewMode;

    [field: CompilerGenerated]
    public event CutsceneEventHandler ExitPreviewMode;

    [field: CompilerGenerated]
    public event CutsceneEventHandler PauseCutscene;

    [field: CompilerGenerated]
    public event CutsceneEventHandler PlayCutscene;

    [field: CompilerGenerated]
    public event CutsceneEventHandler RepaintRequest;

    [field: CompilerGenerated]
    public event CutsceneEventHandler ScrubCutscene;

    [field: CompilerGenerated]
    public event CutsceneEventHandler SetCutsceneTime;

    [field: CompilerGenerated]
    public event CutsceneEventHandler StopCutscene;

    public DirectorControl()
    {
        base.rect = new Rect();
        this.frameRate = 60;
        base.margin = 20f;
        DirectorControlSettings settings = new DirectorControlSettings {
            HorizontalRangeMin = 0f
        };
        base.settings = settings;
    }

    private void bindControls(CutsceneWrapper cutscene)
    {
        List<SidebarControl> newSidebarControls = new List<SidebarControl>();
        List<SidebarControl> removedSidebarControls = new List<SidebarControl>();
        List<TrackItemControl> newTimelineControls = new List<TrackItemControl>();
        List<TrackItemControl> removedTimelineControls = new List<TrackItemControl>();
        this.bindTrackGroupControls(cutscene, newSidebarControls, removedSidebarControls, newTimelineControls, removedTimelineControls);
        foreach (SidebarControl local1 in newSidebarControls)
        {
            local1.DeleteRequest += new DirectorBehaviourControlHandler(this.control_DeleteRequest);
            local1.DuplicateRequest += new SidebarControlHandler(this.sidebarControl_Duplicate);
            local1.SelectRequest += new SidebarControlHandler(this.sidebarControl_SelectRequest);
        }
        foreach (SidebarControl local2 in removedSidebarControls)
        {
            local2.DeleteRequest -= new DirectorBehaviourControlHandler(this.control_DeleteRequest);
            local2.DuplicateRequest -= new SidebarControlHandler(this.sidebarControl_Duplicate);
            local2.SelectRequest -= new SidebarControlHandler(this.sidebarControl_SelectRequest);
        }
        foreach (TrackItemControl control in newTimelineControls)
        {
            this.timelineControls.Add(control);
            control.DeleteRequest += new DirectorBehaviourControlHandler(this.control_DeleteRequest);
            control.RequestTrackItemTranslate += new TranslateTrackItemEventHandler(this.itemControl_RequestTrackItemTranslate);
            control.TrackItemTranslate += new TranslateTrackItemEventHandler(this.itemControl_TrackItemTranslate);
            control.TrackItemUpdate += new TrackItemEventHandler(this.itemControl_TrackItemUpdate);
        }
        foreach (TrackItemControl control2 in removedTimelineControls)
        {
            this.timelineControls.Remove(control2);
            control2.DeleteRequest -= new DirectorBehaviourControlHandler(this.control_DeleteRequest);
            control2.RequestTrackItemTranslate -= new TranslateTrackItemEventHandler(this.itemControl_RequestTrackItemTranslate);
            control2.TrackItemTranslate -= new TranslateTrackItemEventHandler(this.itemControl_TrackItemTranslate);
            control2.TrackItemUpdate -= new TrackItemEventHandler(this.itemControl_TrackItemUpdate);
        }
    }

    private void bindTrackGroupControls(CutsceneWrapper cutscene, List<SidebarControl> newSidebarControls, List<SidebarControl> removedSidebarControls, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
    {
        if (cutscene.HasChanged)
        {
            foreach (TrackGroupWrapper wrapper in cutscene.TrackGroups)
            {
                TrackGroupControl control = null;
                if (!this.trackGroupBinding.TryGetValue(wrapper, out control))
                {
                    System.Type type = typeof(TrackGroupControl);
                    int num2 = 0x7fffffff;
                    foreach (System.Type type2 in DirectorControlHelper.GetAllSubTypes(typeof(TrackGroupControl)))
                    {
                        System.Type c = null;
                        foreach (CutsceneTrackGroupAttribute attribute in type2.GetCustomAttributes(typeof(CutsceneTrackGroupAttribute), true))
                        {
                            if (attribute != null)
                            {
                                c = attribute.TrackGroupType;
                            }
                        }
                        if (c == wrapper.Behaviour.GetType())
                        {
                            type = type2;
                            num2 = 0;
                            break;
                        }
                        if (wrapper.Behaviour.GetType().IsSubclassOf(c))
                        {
                            System.Type baseType = wrapper.Behaviour.GetType();
                            int num5 = 0;
                            while ((baseType != null) && (baseType != c))
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
                    control = (TrackGroupControl) Activator.CreateInstance(type);
                    control.TrackGroup = wrapper;
                    control.DirectorControl = this;
                    control.Initialize();
                    control.SetExpandedFromEditorPrefs();
                    newSidebarControls.Add(control);
                    this.trackGroupBinding.Add(wrapper, control);
                }
            }
            List<TrackGroupWrapper> list = new List<TrackGroupWrapper>();
            foreach (TrackGroupWrapper wrapper2 in this.trackGroupBinding.Keys)
            {
                bool flag = false;
                foreach (TrackGroupWrapper wrapper3 in cutscene.TrackGroups)
                {
                    if (wrapper2.Equals(wrapper3))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    removedSidebarControls.Add(this.trackGroupBinding[wrapper2]);
                    list.Add(wrapper2);
                }
            }
            foreach (TrackGroupWrapper wrapper4 in list)
            {
                this.trackGroupBinding.Remove(wrapper4);
            }
            SortedDictionary<int, TrackGroupWrapper> dictionary = new SortedDictionary<int, TrackGroupWrapper>();
            List<TrackGroupWrapper> list2 = new List<TrackGroupWrapper>();
            foreach (TrackGroupWrapper wrapper5 in this.trackGroupBinding.Keys)
            {
                if ((wrapper5.Ordinal >= 0) && !dictionary.ContainsKey(wrapper5.Ordinal))
                {
                    dictionary.Add(wrapper5.Ordinal, wrapper5);
                }
                else
                {
                    list2.Add(wrapper5);
                }
            }
            int num = 0;
            using (SortedDictionary<int, TrackGroupWrapper>.ValueCollection.Enumerator enumerator4 = dictionary.Values.GetEnumerator())
            {
                while (enumerator4.MoveNext())
                {
                    enumerator4.Current.Ordinal = num;
                    num++;
                }
            }
            using (List<TrackGroupWrapper>.Enumerator enumerator3 = list2.GetEnumerator())
            {
                while (enumerator3.MoveNext())
                {
                    enumerator3.Current.Ordinal = num;
                    num++;
                }
            }
            cutscene.HasChanged = false;
        }
        foreach (TrackGroupWrapper wrapper6 in this.trackGroupBinding.Keys)
        {
            this.trackGroupBinding[wrapper6].BindTrackControls(wrapper6, newSidebarControls, removedSidebarControls, newTimelineControls, removedTimelineControls);
        }
    }

    private void control_DeleteRequest(object sender, DirectorBehaviourControlEventArgs e)
    {
        foreach (TrackGroupWrapper wrapper in this.trackGroupBinding.Keys)
        {
            TrackGroupControl control = this.trackGroupBinding[wrapper];
            control.DeleteSelectedChildren();
            if (control.IsSelected)
            {
                control.Delete();
            }
        }
    }

    private void drawBackground()
    {
        string style = (GUI.skin.FindStyle("AnimationCurveEditorBackground") != null) ? "AnimationCurveEditorBackground" : "CurveEditorBackground";
        GUI.Box(this.trackBodyBackground, GUIContent.none, style);
        base.rect = this.trackBodyBackgroundNoVerticalScrollbar;
        base.BeginViewGUI(false);
        base.SetTickMarkerRanges();
        base.DrawMajorTicks(this.trackBodyBackground, (float) this.frameRate);
        base.EndViewGUI();
    }

    private float getTrackGroupsHeight(CutsceneWrapper cutscene)
    {
        float num = 0f;
        foreach (TrackGroupWrapper wrapper in cutscene.TrackGroups)
        {
            if (this.trackGroupBinding.ContainsKey(wrapper))
            {
                TrackGroupControl control = this.trackGroupBinding[wrapper];
                num += control.GetHeight();
            }
        }
        return num;
    }

    private float itemControl_RequestTrackItemTranslate(object sender, TrackItemEventArgs e)
    {
        float firetime = e.firetime;
        float b = e.firetime;
        bool flag = false;
        while (!flag && (b != 0f))
        {
            foreach (TrackItemControl control in this.timelineControls)
            {
                if (control.IsSelected)
                {
                    if (e.firetime > 0f)
                    {
                        b = Mathf.Min(control.RequestTranslate(firetime), b);
                    }
                    else
                    {
                        b = Mathf.Max(control.RequestTranslate(firetime), b);
                    }
                }
            }
            if (b != firetime)
            {
                firetime = b;
            }
            else
            {
                flag = true;
            }
        }
        return firetime;
    }

    private float itemControl_TrackItemTranslate(object sender, TrackItemEventArgs e)
    {
        foreach (TrackItemControl control in this.timelineControls)
        {
            if (control.IsSelected)
            {
                control.Translate(e.firetime);
            }
        }
        return 0f;
    }

    private void itemControl_TrackItemUpdate(object sender, TrackItemEventArgs e)
    {
        foreach (TrackItemControl control in this.timelineControls)
        {
            if (control.IsSelected)
            {
                control.ConfirmTranslate();
            }
        }
    }

    public void OnDisable()
    {
        EditorPrefs.SetFloat("DirectorControl.areaX", base.shownAreaInsideMargins.x);
        EditorPrefs.SetFloat("DirectorControl.areaWidth", base.shownAreaInsideMargins.width);
        EditorPrefs.SetBool("DirectorControl.isSnappingEnabled", this.directorState.IsSnapEnabled);
        EditorPrefs.SetFloat("DirectorControl.SidebarWidth", this.track_header_area_width);
    }

    public void OnGUI(Rect controlArea, CutsceneWrapper cs)
    {
        this.cutscene = cs;
        this.updateControlLayout(controlArea);
        this.drawBackground();
        this.updateTimelineHeader(this.headerArea, this.timeRuleArea);
        if (this.cutscene != null)
        {
            this.bindControls(this.cutscene);
            this.updateControlState();
            float bottomValue = this.getTrackGroupsHeight(this.cutscene);
            if (Event.current.type == EventType.ScrollWheel)
            {
                this.verticalScrollValue += (17f * Event.current.delta.y) / 3f;
            }
            this.verticalScrollValue = GUI.VerticalScrollbar(this.verticalScrollbarArea, this.verticalScrollValue, Mathf.Min(this.bodyArea.height, bottomValue), 0f, bottomValue);
            Vector2 vector = new Vector2(base.Translation.x, this.verticalScrollValue);
            base.Translation = vector;
            Rect area = new Rect(this.bodyArea.x, -base.Translation.y, this.bodyArea.width, bottomValue);
            this.directorState.Translation = base.Translation;
            this.directorState.Scale = base.Scale;
            GUILayout.BeginArea(this.bodyArea, string.Empty);
            this.updateTrackGroups(area);
            this.updateDurationBar();
            GUILayout.EndArea();
            this.updateScrubber();
            base.BeginViewGUI(true);
            this.updateUserInput();
            this.updateDragAndDrop();
        }
    }

    public void OnLoad(GUISkin skin)
    {
        this.customSkin = skin;
        string str = "Cinema Suite/Cinema Director/";
        string str2 = ".png";
        float min = 0f;
        float @float = 60f;
        if (EditorPrefs.HasKey("DirectorControl.areaX"))
        {
            min = EditorPrefs.GetFloat("DirectorControl.areaX");
        }
        if (EditorPrefs.HasKey("DirectorControl.areaWidth"))
        {
            @float = EditorPrefs.GetFloat("DirectorControl.areaWidth");
        }
        if (EditorPrefs.HasKey("DirectorControl.isSnappingEnabled"))
        {
            this.directorState.IsSnapEnabled = EditorPrefs.GetBool("DirectorControl.isSnappingEnabled");
        }
        base.SetShownHRangeInsideMargins(min, min + @float);
        if (EditorPrefs.HasKey("DirectorControl.SidebarWidth"))
        {
            this.track_header_area_width = EditorPrefs.GetFloat("DirectorControl.SidebarWidth");
        }
        if (this.playButton == null)
        {
            this.playButton = EditorGUIUtility.Load(str + "Director_PlayIcon" + str2) as Texture;
        }
        if (this.playButton == null)
        {
            Debug.Log("Play button icon missing from Resources folder.");
        }
        if (this.pauseButton == null)
        {
            this.pauseButton = EditorGUIUtility.Load(str + "Director_PauseIcon" + str2) as Texture;
        }
        if (this.pauseButton == null)
        {
            Debug.Log("Pause button missing from Resources folder.");
        }
        if (this.stopButton == null)
        {
            this.stopButton = EditorGUIUtility.Load(str + "Director_StopIcon" + str2) as Texture;
        }
        if (this.stopButton == null)
        {
            Debug.Log("Stop button icon missing from Resources folder.");
        }
        if (this.frameForwardButton == null)
        {
            this.frameForwardButton = EditorGUIUtility.Load(str + "Director_FrameForwardIcon" + str2) as Texture;
        }
        if (this.frameForwardButton == null)
        {
            Debug.Log("Director_FrameForwardIcon.png missing from Resources folder.");
        }
        if (this.frameBackwardButton == null)
        {
            this.frameBackwardButton = EditorGUIUtility.Load(str + "Director_FrameBackwardIcon" + str2) as Texture;
        }
        if (this.frameBackwardButton == null)
        {
            Debug.Log("Director_FrameBackwardIcon.png missing from Resources folder.");
        }
        if (this.scrubHead == null)
        {
            this.scrubHead = EditorGUIUtility.Load(str + "Director_Playhead" + str2) as Texture;
        }
        if (this.scrubHead == null)
        {
            Debug.Log("Director_Playhead missing from Resources folder.");
        }
        if (this.scrubDurationHead == null)
        {
            this.scrubDurationHead = EditorGUIUtility.Load(str + "Director_Duration_Playhead" + str2) as Texture;
        }
        if (this.scrubDurationHead == null)
        {
            Debug.Log("Director_Duration_Playhead missing from Resources folder.");
        }
        if (this.customSkin != null)
        {
            DirectorControlStyles.BoxSelect = this.customSkin.FindStyle("BoxSelect");
            DirectorControlStyles.UpArrowIcon = this.customSkin.FindStyle("UpArrowIcon");
            DirectorControlStyles.DownArrowIcon = this.customSkin.FindStyle("DownArrowIcon");
        }
        else
        {
            DirectorControlStyles.BoxSelect = "box";
            DirectorControlStyles.UpArrowIcon = "box";
            DirectorControlStyles.DownArrowIcon = "box";
        }
        TrackGroupControl.InitStyles(this.customSkin);
        TimelineTrackControl.InitStyles(this.customSkin);
    }

    public void Repaint()
    {
        if (this.RepaintRequest != null)
        {
            this.RepaintRequest(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
        }
    }

    public void Rescale()
    {
        if (this.cutscene != null)
        {
            base.SetShownHRangeInsideMargins(0f, this.cutscene.Duration);
        }
        else
        {
            base.SetShownHRangeInsideMargins(0f, 60f);
        }
    }

    private void sidebarControl_Duplicate(object sender, SidebarControlEventArgs e)
    {
        foreach (TrackGroupWrapper wrapper in this.trackGroupBinding.Keys)
        {
            TrackGroupControl control = this.trackGroupBinding[wrapper];
            control.DuplicateSelectedChildren();
            if (control.IsSelected)
            {
                control.Duplicate();
            }
        }
    }

    private void sidebarControl_SelectRequest(object sender, SidebarControlEventArgs e)
    {
        Behaviour behaviour = e.Behaviour;
        if (behaviour != null)
        {
            if (Event.current.control)
            {
                if (Selection.Contains(behaviour.gameObject))
                {
                    GameObject[] gameObjects = Selection.gameObjects;
                    ArrayUtility.Remove<GameObject>(ref gameObjects, behaviour.gameObject);
                    Selection.objects = gameObjects;
                }
                else
                {
                    GameObject[] gameObjects = Selection.gameObjects;
                    ArrayUtility.Add<GameObject>(ref gameObjects, behaviour.gameObject);
                    Selection.objects = gameObjects;
                }
            }
            else if (Event.current.shift)
            {
                List<SidebarControl> list = new List<SidebarControl>();
                foreach (TrackGroupWrapper wrapper in this.trackGroupBinding.Keys)
                {
                    TrackGroupControl item = this.trackGroupBinding[wrapper];
                    list.Add(item);
                    list.AddRange(item.GetSidebarControlChildren(true));
                }
                SidebarControl sidebarControl = e.SidebarControl;
                SidebarControl control2 = e.SidebarControl;
                foreach (SidebarControl control4 in list)
                {
                    if (control4.IsSelected)
                    {
                        if (sidebarControl.CompareTo(control4) > 0)
                        {
                            sidebarControl = control4;
                        }
                        if (control2.CompareTo(control4) < 0)
                        {
                            control2 = control4;
                        }
                    }
                }
                foreach (SidebarControl control5 in list)
                {
                    if ((!control5.IsSelected && (sidebarControl.CompareTo(control5) <= 0)) && (control2.CompareTo(control5) >= 0))
                    {
                        control5.Select();
                    }
                }
            }
            else
            {
                Selection.activeObject = behaviour;
            }
            Event.current.Use();
        }
    }

    private void updateControlLayout(Rect controlArea)
    {
        this.hasLayoutChanged = controlArea != this.previousControlArea;
        this.headerArea = new Rect(controlArea.x, controlArea.y, controlArea.width, 17f);
        this.sidebarControlArea = new Rect(this.track_header_area_width, this.headerArea.y + 17f, 4f, (controlArea.height - 17f) - 15f);
        EditorGUIUtility.AddCursorRect(this.sidebarControlArea, MouseCursor.ResizeHorizontal);
        int controlID = GUIUtility.GetControlID("SidebarResize".GetHashCode(), FocusType.Passive, this.sidebarControlArea);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (this.sidebarControlArea.Contains(Event.current.mousePosition) && (Event.current.button == 0))
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    this.track_header_area_width = Mathf.Clamp(Event.current.mousePosition.x, 256f, 512f);
                    this.hasLayoutChanged = true;
                }
                break;
        }
        if (this.hasLayoutChanged)
        {
            this.timeRuleArea = new Rect(this.track_header_area_width + this.sidebarControlArea.width, controlArea.y, ((controlArea.width - this.track_header_area_width) - 15f) - this.sidebarControlArea.width, 17f);
            this.bodyArea = new Rect(controlArea.x, this.headerArea.y + 17f, controlArea.width - 15f, (controlArea.height - 17f) - 15f);
            this.trackBodyBackground = new Rect((controlArea.x + this.track_header_area_width) + this.sidebarControlArea.width, this.bodyArea.y, ((controlArea.width - 15f) - this.track_header_area_width) - this.sidebarControlArea.width, (controlArea.height - 17f) - 15f);
            this.trackBodyBackgroundNoVerticalScrollbar = new Rect((controlArea.x + this.track_header_area_width) + this.sidebarControlArea.width, this.bodyArea.y, ((controlArea.width - 15f) - this.track_header_area_width) - this.sidebarControlArea.width, controlArea.height - 17f);
            this.trackBodyBackgroundNoScrollbars = new Rect((controlArea.x + this.track_header_area_width) + this.sidebarControlArea.width, this.bodyArea.y, ((controlArea.width - 15f) - this.track_header_area_width) - this.sidebarControlArea.width, (controlArea.height - 17f) - 15f);
            this.verticalScrollbarArea = new Rect(this.bodyArea.x + this.bodyArea.width, this.bodyArea.y, 15f, (controlArea.height - 17f) - 15f);
        }
        this.previousControlArea = controlArea;
    }

    private void updateControlState()
    {
        base.HScrollMax = this.cutscene.Duration;
        this.directorState.TickDistance = base.GetMajorTickDistance((float) this.frameRate);
        this.directorState.ScrubberPosition = this.cutscene.RunningTime;
    }

    private void updateDragAndDrop()
    {
        Event current = Event.current;
        if (current.type == EventType.DragExited)
        {
            DragAndDrop.PrepareStartDrag();
        }
        if (this.bodyArea.Contains(current.mousePosition))
        {
            switch (current.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    if (this.DragPerformed != null)
                    {
                        this.DragPerformed(this, new CinemaDirectorDragArgs(this.cutscene.Behaviour, DragAndDrop.objectReferences));
                    }
                    current.Use();
                    break;
            }
        }
    }

    private void updateDurationBar()
    {
        float x = this.directorState.TimeToPosition(this.cutscene.Duration) + this.trackBodyBackground.x;
        Color color = GUI.color;
        GUI.color = new Color(0.25f, 0.5f, 0.5f);
        Rect position = new Rect(x - 8f, this.bodyArea.height - 13f, 16f, 16f);
        int controlID = GUIUtility.GetControlID("DurationBar".GetHashCode(), FocusType.Passive, position);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    Vector2 mousePosition = Event.current.mousePosition;
                    mousePosition.x -= this.trackBodyBackground.x;
                    Undo.RecordObject(this.cutscene.Behaviour, "Changed Cutscene Duration");
                    float time = base.ViewToDrawingTransformPoint(mousePosition).x;
                    this.cutscene.Duration = this.directorState.SnappedTime(time);
                    Event.current.Use();
                }
                break;
        }
        if ((x > this.trackBodyBackground.x) && (x < this.bodyArea.width))
        {
            GUI.DrawTexture(position, this.scrubDurationHead);
        }
        GUI.color = color;
        Handles.color = new Color(0.25f, 0.5f, 0.5f);
        if (x > this.trackBodyBackground.x)
        {
            Handles.DrawLine(new Vector3(x, 0f, 0f), (Vector3) new Vector2(x, (this.timeRuleArea.y + this.trackBodyBackgroundNoVerticalScrollbar.height) - 13f));
            Handles.DrawLine(new Vector3(x + 1f, 0f, 0f), (Vector3) new Vector2(x + 1f, (this.timeRuleArea.y + this.trackBodyBackgroundNoVerticalScrollbar.height) - 13f));
        }
    }

    private void updateScrubber()
    {
        if ((Event.current.type == EventType.Repaint) && (this.InPreviewMode || this.cutscene.IsPlaying))
        {
            float x = this.directorState.TimeToPosition(this.cutscene.RunningTime) + this.trackBodyBackground.x;
            GUI.color = new Color(1f, 0f, 0f, 1f);
            Handles.color = new Color(1f, 0f, 0f, 1f);
            if ((x > this.trackBodyBackground.x) && (x < this.bodyArea.width))
            {
                GUI.DrawTexture(new Rect(x - 8f, 20f, 16f, 16f), this.scrubHead);
                Handles.DrawLine((Vector3) new Vector2(x, 34f), (Vector3) new Vector2(x, (this.timeRuleArea.y + this.trackBodyBackgroundNoVerticalScrollbar.height) + 3f));
                Handles.DrawLine((Vector3) new Vector2(x + 1f, 34f), (Vector3) new Vector2(x + 1f, (this.timeRuleArea.y + this.trackBodyBackgroundNoVerticalScrollbar.height) + 3f));
            }
            GUI.color = GUI.color;
        }
    }

    private void updateTimelineHeader(Rect headerArea, Rect timeRulerArea)
    {
        GUILayout.BeginArea(headerArea, string.Empty, EditorStyles.toolbarButton);
        this.updateToolbar();
        GUILayout.BeginArea(timeRulerArea, string.Empty, EditorStyles.toolbarButton);
        GUILayout.EndArea();
        GUILayout.EndArea();
        base.TimeRuler(timeRulerArea, (float) this.frameRate);
        if (this.cutscene != null)
        {
            int controlID = GUIUtility.GetControlID("TimeRuler".GetHashCode(), FocusType.Passive, timeRulerArea);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (timeRulerArea.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        Vector2 mousePosition = Event.current.mousePosition;
                        mousePosition.x -= timeRulerArea.x;
                        this.InPreviewMode = true;
                        float time = Mathf.Max(base.ViewToDrawingTransformPoint(mousePosition).x, 0f);
                        if (this.cutscene == null)
                        {
                            return;
                        }
                        this.directorState.ScrubberPosition = time;
                        this.SetCutsceneTime(this, new CinemaDirectorArgs(this.cutscene.Behaviour, time));
                    }
                    return;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        if (this.cutscene == null)
                        {
                            return;
                        }
                        this.PauseCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
                    }
                    return;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        Vector2 mousePosition = Event.current.mousePosition;
                        mousePosition.x -= timeRulerArea.x;
                        float time = Mathf.Max(base.ViewToDrawingTransformPoint(mousePosition).x, 0f);
                        if (this.cutscene != null)
                        {
                            this.ScrubCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour, time));
                            this.directorState.ScrubberPosition = time;
                        }
                        Event.current.Use();
                    }
                    return;
            }
            if (GUIUtility.hotControl == controlID)
            {
                Vector2 mousePosition = Event.current.mousePosition;
                mousePosition.x -= timeRulerArea.x;
                float time = Mathf.Max(base.ViewToDrawingTransformPoint(mousePosition).x, 0f);
                if (this.cutscene != null)
                {
                    this.ScrubCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour, time));
                    this.directorState.ScrubberPosition = time;
                }
            }
        }
    }

    private void updateToolbar()
    {
        GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.MaxWidth(150f) };
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, options);
        GUILayout.FlexibleSpace();
        if ((this.cutscene != null) && this.cutscene.IsPlaying)
        {
            if (GUILayout.Button(this.pauseButton, EditorStyles.toolbarButton, new GUILayoutOption[0]))
            {
                this.PauseCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
            }
        }
        else if (GUILayout.Button(this.playButton, EditorStyles.toolbarButton, new GUILayoutOption[0]) && (this.cutscene != null))
        {
            this.InPreviewMode = true;
            this.PlayCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
        }
        if (GUILayout.Button(this.stopButton, EditorStyles.toolbarButton, new GUILayoutOption[0]) && (this.cutscene != null))
        {
            this.InPreviewMode = false;
            this.StopCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
        }
        GUILayout.FlexibleSpace();
        if (((Event.current.type == EventType.KeyDown) && !EditorGUIUtility.editingTextField) && (Event.current.keyCode == KeyCode.Space))
        {
            if (!this.cutscene.IsPlaying)
            {
                this.InPreviewMode = true;
                this.PlayCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
            }
            else
            {
                this.PauseCutscene(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
            }
            Event.current.Use();
        }
        float runningTime = 0f;
        if (this.cutscene != null)
        {
            runningTime = this.cutscene.RunningTime;
        }
        GUILayout.Space(10f);
        GUILayoutOption[] optionArray2 = new GUILayoutOption[] { GUILayout.Width(50f) };
        runningTime = EditorGUILayout.FloatField(runningTime, optionArray2);
        if ((this.cutscene != null) && (runningTime != this.cutscene.RunningTime))
        {
            this.InPreviewMode = true;
            runningTime = Mathf.Max(runningTime, 0f);
            this.directorState.ScrubberPosition = runningTime;
            this.SetCutsceneTime(this, new CinemaDirectorArgs(this.cutscene.Behaviour, runningTime));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void updateTrackGroups(Rect area)
    {
        float y = area.y;
        SortedDictionary<int, TrackGroupWrapper> dictionary = new SortedDictionary<int, TrackGroupWrapper>();
        foreach (TrackGroupWrapper wrapper in this.trackGroupBinding.Keys)
        {
            this.trackGroupBinding[wrapper].TrackGroup = wrapper;
            dictionary.Add(wrapper.Ordinal, wrapper);
        }
        foreach (int num2 in dictionary.Keys)
        {
            TrackGroupWrapper trackGroup = dictionary[num2];
            TrackGroupControl control = this.trackGroupBinding[trackGroup];
            control.Ordinal = new int[] { num2 };
            float height = control.GetHeight();
            Rect position = new Rect(area.x, y, area.width, height);
            Rect fullHeader = new Rect(area.x, y, this.track_header_area_width + this.sidebarControlArea.width, height);
            Rect safeHeader = new Rect(area.x, y, this.track_header_area_width - 32f, height);
            Rect rect4 = new Rect(safeHeader.x + safeHeader.width, y, 16f, 16f);
            Rect content = new Rect(fullHeader.x + fullHeader.width, y, area.width - fullHeader.width, height);
            control.Update(trackGroup, this.directorState, position, fullHeader, safeHeader, content);
            GUI.enabled = num2 > 0;
            if (GUI.Button(rect4, string.Empty, DirectorControlStyles.UpArrowIcon))
            {
                trackGroup.Ordinal--;
                TrackGroupWrapper wrapper1 = this.trackGroupBinding[dictionary[num2 - 1]].TrackGroup;
                wrapper1.Ordinal++;
            }
            GUI.enabled = num2 < (dictionary.Count - 1);
            if (GUI.Button(new Rect(rect4.x + 16f, y, 16f, 16f), string.Empty, DirectorControlStyles.DownArrowIcon))
            {
                trackGroup.Ordinal++;
                TrackGroupWrapper wrapper3 = this.trackGroupBinding[dictionary[num2 + 1]].TrackGroup;
                wrapper3.Ordinal--;
            }
            GUI.enabled = true;
            y += height;
        }
    }

    private void updateUserInput()
    {
        int controlID = GUIUtility.GetControlID("DirectorBody".GetHashCode(), FocusType.Passive, this.trackBodyBackgroundNoVerticalScrollbar);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (this.trackBodyBackgroundNoVerticalScrollbar.Contains(Event.current.mousePosition) && (Event.current.button == 0))
                {
                    this.isBoxSelecting = true;
                    this.mouseDownPosition = Event.current.mousePosition;
                    Selection.activeObject = null;
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    this.isBoxSelecting = false;
                    this.selectionBox = new Rect();
                    GUIUtility.hotControl = 0;
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    float b = Mathf.Clamp(Event.current.mousePosition.x, this.trackBodyBackgroundNoScrollbars.x, this.trackBodyBackgroundNoScrollbars.xMax);
                    float num3 = Mathf.Clamp(Event.current.mousePosition.y, this.trackBodyBackgroundNoScrollbars.y, this.trackBodyBackgroundNoScrollbars.yMax);
                    float x = Mathf.Min(this.mouseDownPosition.x, b);
                    float width = Mathf.Abs((float) (b - this.mouseDownPosition.x));
                    float y = Mathf.Min(this.mouseDownPosition.y, num3);
                    float height = Mathf.Abs((float) (this.mouseDownPosition.y - num3));
                    this.selectionBox = new Rect(x, y, width, height);
                    Rect selectionBox = new Rect(this.selectionBox);
                    selectionBox.y -= 34f;
                    foreach (TrackGroupWrapper wrapper in this.trackGroupBinding.Keys)
                    {
                        this.trackGroupBinding[wrapper].BoxSelect(selectionBox);
                    }
                }
                break;
        }
        if (this.isBoxSelecting)
        {
            GUI.Box(this.selectionBox, GUIContent.none, DirectorControlStyles.BoxSelect);
        }
    }

    public void ZoomIn()
    {
        base.Scale *= 1.5f;
    }

    public void ZoomOut()
    {
        base.Scale *= 0.75f;
    }

    public DirectorEditor.ResizeOption ResizeOption
    {
        get => 
            this.directorState.ResizeOption;
        set => 
            (this.directorState.ResizeOption = value);
    }

    public bool IsSnappingEnabled
    {
        get => 
            this.directorState.IsSnapEnabled;
        set => 
            (this.directorState.IsSnapEnabled = value);
    }

    public bool InPreviewMode
    {
        get => 
            this.directorState.IsInPreviewMode;
        set
        {
            if (this.cutscene != null)
            {
                if (!this.directorState.IsInPreviewMode & value)
                {
                    this.EnterPreviewMode(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
                }
                else if (this.directorState.IsInPreviewMode && !value)
                {
                    this.ExitPreviewMode(this, new CinemaDirectorArgs(this.cutscene.Behaviour));
                }
            }
            this.directorState.IsInPreviewMode = value;
        }
    }

    public static class DirectorControlStyles
    {
        public static GUIStyle UpArrowIcon;
        public static GUIStyle DownArrowIcon;
        public static GUIStyle BoxSelect;
    }
}

