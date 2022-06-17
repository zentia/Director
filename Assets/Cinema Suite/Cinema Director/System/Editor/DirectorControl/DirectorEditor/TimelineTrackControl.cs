using DirectorEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TimelineTrackControl : SidebarControl
{
    public static TrackStyles styles;
    public const float ROW_HEIGHT = 17f;
    protected const int INDENT_AMOUNT = 14;
    protected const int TRACK_ICON_WIDTH = 0x10;
    protected UnityEngine.Rect trackArea = new UnityEngine.Rect(0f, 0f, 0f, 17f);
    protected DirectorControlState state;
    private Dictionary<TimelineItemWrapper, TrackItemControl> itemMap = new Dictionary<TimelineItemWrapper, TrackItemControl>();
    private TimelineTrackWrapper targetTrack;
    private TrackGroupControl trackGroupControl;
    protected bool renameRequested;
    protected bool isRenaming;
    private int renameControlID;

    internal void BindTimelineItemControls(TimelineTrackWrapper track, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
    {
        if (this.TargetTrack.HasChanged)
        {
            foreach (TimelineItemWrapper wrapper in track.Items)
            {
                TrackItemControl control = null;
                if (!this.itemMap.TryGetValue(wrapper, out control))
                {
                    System.Type type = typeof(TrackItemControl);
                    int num = 0x7fffffff;
                    int drawPriority = 0;
                    foreach (System.Type type2 in DirectorControlHelper.GetAllSubTypes(typeof(TrackItemControl)))
                    {
                        foreach (CutsceneItemControlAttribute attribute in type2.GetCustomAttributes(typeof(CutsceneItemControlAttribute), true))
                        {
                            if (attribute != null)
                            {
                                int subTypeDepth = DirectorControlHelper.GetSubTypeDepth(wrapper.Behaviour.GetType(), attribute.ItemType);
                                if (subTypeDepth < num)
                                {
                                    type = type2;
                                    num = subTypeDepth;
                                    drawPriority = attribute.DrawPriority;
                                }
                            }
                        }
                    }
                    control = (TrackItemControl) Activator.CreateInstance(type);
                    control.DrawPriority = drawPriority;
                    control.Initialize(wrapper, this.TargetTrack);
                    control.TrackControl = this;
                    this.initializeTrackItemControl(control);
                    newTimelineControls.Add(control);
                    this.itemMap.Add(wrapper, control);
                }
            }
            List<TimelineItemWrapper> list = new List<TimelineItemWrapper>();
            foreach (TimelineItemWrapper wrapper2 in this.itemMap.Keys)
            {
                bool flag = false;
                foreach (TimelineItemWrapper wrapper3 in track.Items)
                {
                    if (wrapper2.Equals(wrapper3))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    this.prepareTrackItemControlForRemoval(this.itemMap[wrapper2]);
                    removedTimelineControls.Add(this.itemMap[wrapper2]);
                    list.Add(wrapper2);
                }
            }
            foreach (TimelineItemWrapper wrapper4 in list)
            {
                this.itemMap.Remove(wrapper4);
            }
        }
        track.HasChanged = false;
    }

    internal void BoxSelect(UnityEngine.Rect selectionBox)
    {
        foreach (TimelineItemWrapper wrapper in this.itemMap.Keys)
        {
            this.itemMap[wrapper].BoxSelect(selectionBox);
        }
    }

    public virtual void calculateHeight()
    {
        this.trackArea.height = 17f;
        if (base.isExpanded)
        {
            this.trackArea.height = 17f * base.expandedSize;
        }
    }

    private static int Comparison(KeyValuePair<int, TimelineItemWrapper> x, KeyValuePair<int, TimelineItemWrapper> y)
    {
        int num = 0;
        if (x.Key < y.Key)
        {
            return 1;
        }
        if (x.Key > y.Key)
        {
            num = -1;
        }
        return num;
    }

    private void control_DeleteRequest(object sender, DirectorBehaviourControlEventArgs e)
    {
        this.delete();
    }

    private void delete()
    {
        base.RequestDelete();
    }

    internal void Delete()
    {
        Undo.DestroyObjectImmediate(this.TargetTrack.Behaviour.gameObject);
    }

    internal void DeleteSelectedChildren()
    {
        foreach (TimelineItemWrapper wrapper in this.itemMap.Keys)
        {
            TrackItemControl control = this.itemMap[wrapper];
            if (control.IsSelected)
            {
                control.Delete();
            }
        }
    }

    private void duplicate()
    {
        base.RequestDuplicate();
    }

    internal void Duplicate()
    {
        GameObject objectToUndo = UnityEngine.Object.Instantiate<GameObject>(this.TargetTrack.Behaviour.gameObject);
        string name = this.TargetTrack.Behaviour.gameObject.name;
        string s = Regex.Match(name, @"(\d+)$").Value;
        int result = 1;
        if (int.TryParse(s, out result))
        {
            result++;
            objectToUndo.name = name.Substring(0, name.Length - s.Length) + result.ToString();
        }
        else
        {
            objectToUndo.name = name.Substring(0, name.Length - s.Length) + " " + 1.ToString();
        }
        objectToUndo.transform.parent = this.TargetTrack.Behaviour.transform.parent;
        Undo.RegisterCreatedObjectUndo(objectToUndo, "Duplicate " + objectToUndo.name);
    }

    public virtual void Initialize()
    {
    }

    protected virtual void initializeTrackItemControl(TrackItemControl control)
    {
    }

    internal static void InitStyles(GUISkin skin)
    {
        if (styles == null)
        {
            styles = new TrackStyles(skin);
        }
    }

    protected virtual void prepareTrackItemControlForRemoval(TrackItemControl control)
    {
    }

    private void rename()
    {
        this.renameRequested = true;
        this.isRenaming = true;
    }

    protected virtual void showBodyContextMenu(Event current)
    {
    }

    protected void showHeaderContextMenu()
    {
        GenericMenu menu1 = new GenericMenu();
        menu1.AddItem(new GUIContent("Rename"), false, new GenericMenu.MenuFunction(this.rename));
        menu1.AddItem(new GUIContent("Duplicate"), false, new GenericMenu.MenuFunction(this.duplicate));
        menu1.AddItem(new GUIContent("Delete"), false, new GenericMenu.MenuFunction(this.delete));
        menu1.ShowAsContext();
    }

    public virtual void UpdateHeaderBackground(UnityEngine.Rect position, int ordinal)
    {
        if (Selection.Contains(this.TargetTrack.Behaviour.gameObject) && !this.isRenaming)
        {
            GUI.Box(position, string.Empty, styles.backgroundSelected);
        }
        else if ((ordinal % 2) == 0)
        {
            GUI.Box(position, string.Empty, styles.TrackSidebarBG2);
        }
        else
        {
            GUI.Box(position, string.Empty, styles.TrackSidebarBG1);
        }
    }

    public virtual void UpdateHeaderContents(DirectorControlState state, UnityEngine.Rect position, UnityEngine.Rect headerBackground)
    {
        UnityEngine.Rect rect = new UnityEngine.Rect(position.x + 14f, position.y, 14f, position.height);
        UnityEngine.Rect rect2 = new UnityEngine.Rect(rect.x + rect.width, position.y, ((position.width - 14f) - 96f) - 14f, position.height);
        string name = this.TargetTrack.Behaviour.name;
        bool flag = EditorGUI.Foldout(rect, base.isExpanded, GUIContent.none, false);
        if (flag != base.isExpanded)
        {
            base.isExpanded = flag;
            EditorPrefs.SetBool(base.IsExpandedKey, base.isExpanded);
        }
        this.updateHeaderControl1(new UnityEngine.Rect(position.width - 80f, position.y, 16f, 16f));
        this.updateHeaderControl2(new UnityEngine.Rect(position.width - 64f, position.y, 16f, 16f));
        this.updateHeaderControl3(new UnityEngine.Rect(position.width - 48f, position.y, 16f, 16f));
        this.updateHeaderControl4(new UnityEngine.Rect(position.width - 32f, position.y, 16f, 16f));
        this.updateHeaderControl5(new UnityEngine.Rect(position.width - 16f, position.y, 16f, 16f));
        int controlID = GUIUtility.GetControlID(this.TargetTrack.Behaviour.GetInstanceID(), FocusType.Passive, position);
        if (this.isRenaming)
        {
            GUI.SetNextControlName("TrackRename");
            name = EditorGUI.TextField(rect2, GUIContent.none, name);
            if (this.renameRequested)
            {
                EditorGUI.FocusTextInControl("TrackRename");
                this.renameRequested = false;
                this.renameControlID = GUIUtility.keyboardControl;
            }
            if ((!EditorGUIUtility.editingTextField || (this.renameControlID != GUIUtility.keyboardControl)) || ((Event.current.keyCode == KeyCode.Return) || ((Event.current.type == EventType.MouseDown) && !rect2.Contains(Event.current.mousePosition))))
            {
                this.isRenaming = false;
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                EditorGUIUtility.editingTextField = false;
            }
        }
        if (this.TargetTrack.Behaviour.name != name)
        {
            Undo.RecordObject(this.TargetTrack.Behaviour.gameObject, $"Renamed {this.TargetTrack.Behaviour.name}");
            this.TargetTrack.Behaviour.name = name;
        }
        if (!this.isRenaming)
        {
            string text = name;
            for (Vector2 vector = GUI.skin.label.CalcSize(new GUIContent(text)); (vector.x > rect2.width) && (text.Length > 5); vector = GUI.skin.label.CalcSize(new GUIContent(text)))
            {
                text = text.Substring(0, text.Length - 4) + "...";
            }
            if (Selection.Contains(this.TargetTrack.Behaviour.gameObject))
            {
                GUI.Label(rect2, text, EditorStyles.whiteLabel);
            }
            else
            {
                GUI.Label(rect2, text);
            }
            if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
            {
                if (position.Contains(Event.current.mousePosition) && (Event.current.button == 1))
                {
                    if (!this.IsSelected)
                    {
                        base.RequestSelect();
                    }
                    this.showHeaderContextMenu();
                    Event.current.Use();
                }
                else if (position.Contains(Event.current.mousePosition) && (Event.current.button == 0))
                {
                    base.RequestSelect();
                    Event.current.Use();
                }
            }
        }
    }

    protected virtual void updateHeaderControl1(UnityEngine.Rect position)
    {
    }

    protected virtual void updateHeaderControl2(UnityEngine.Rect position)
    {
    }

    protected virtual void updateHeaderControl3(UnityEngine.Rect position)
    {
    }

    protected virtual void updateHeaderControl4(UnityEngine.Rect position)
    {
    }

    protected virtual void updateHeaderControl5(UnityEngine.Rect position)
    {
    }

    public virtual void UpdateTrackBodyBackground(UnityEngine.Rect position)
    {
        GUI.Box(position, string.Empty, styles.TrackAreaStyle);
    }

    public virtual void UpdateTrackContents(DirectorControlState state, UnityEngine.Rect position)
    {
        this.state = state;
        this.trackArea = position;
        List<KeyValuePair<int, TimelineItemWrapper>> list = new List<KeyValuePair<int, TimelineItemWrapper>>();
        foreach (TimelineItemWrapper wrapper in this.itemMap.Keys)
        {
            TrackItemControl control = this.itemMap[wrapper];
            control.Wrapper = wrapper;
            control.Track = this.TargetTrack;
            control.PreUpdate(state, position);
            KeyValuePair<int, TimelineItemWrapper> item = new KeyValuePair<int, TimelineItemWrapper>(control.DrawPriority, wrapper);
            list.Add(item);
        }
        list.Sort(new Comparison<KeyValuePair<int, TimelineItemWrapper>>(TimelineTrackControl.Comparison));
        foreach (KeyValuePair<int, TimelineItemWrapper> pair2 in list)
        {
            this.itemMap[pair2.Value].HandleInput(state, position);
        }
        list.Reverse();
        foreach (KeyValuePair<int, TimelineItemWrapper> pair3 in list)
        {
            TrackItemControl local1 = this.itemMap[pair3.Value];
            local1.Draw(state);
            local1.PostUpdate(state);
        }
        UnityEngine.Rect rect = new UnityEngine.Rect(0f, 0f, position.width, position.height);
        int controlID = GUIUtility.GetControlID(this.TargetTrack.Behaviour.GetInstanceID(), FocusType.Passive, rect);
        if ((((Event.current.GetTypeForControl(controlID) == EventType.MouseDown) && rect.Contains(Event.current.mousePosition)) && ((Event.current.button == 1) && !Event.current.alt)) && (!Event.current.shift && !Event.current.control))
        {
            this.showBodyContextMenu(Event.current);
            Event.current.Use();
        }
    }

    public UnityEngine.Rect Rect
    {
        get
        {
            this.calculateHeight();
            return this.trackArea;
        }
    }

    public TimelineTrackWrapper TargetTrack
    {
        get => 
            this.targetTrack;
        set
        {
            this.targetTrack = value;
            base.Behaviour = this.targetTrack.Behaviour;
        }
    }

    public DirectorControlState State
    {
        set => 
            (this.state = value);
    }

    public IEnumerable<TrackItemControl> Controls =>
        this.itemMap.Values;

    public TrackGroupControl TrackGroupControl
    {
        get => 
            this.trackGroupControl;
        set => 
            (this.trackGroupControl = value);
    }

    public bool IsSelected =>
        Selection.Contains(this.TargetTrack.Behaviour.gameObject);

    public class TrackStyles
    {
        public GUIStyle TrackAreaStyle;
        public GUIStyle backgroundSelected;
        public GUIStyle backgroundContentSelected;
        public GUIStyle TrackSidebarBG1;
        public GUIStyle TrackSidebarBG2;
        private GUIStyle trackItemStyle;
        public GUIStyle AudioTrackItemStyle;
        public GUIStyle ShotTrackItemStyle;
        public GUIStyle GlobalTrackItemStyle;
        public GUIStyle ActorTrackItemStyle;
        private GUIStyle curveTrackItemStyle;
        private GUIStyle trackItemSelectedStyle;
        public GUIStyle AudioTrackItemSelectedStyle;
        public GUIStyle ShotTrackItemSelectedStyle;
        public GUIStyle GlobalTrackItemSelectedStyle;
        public GUIStyle ActorTrackItemSelectedStyle;
        private GUIStyle curveTrackItemSelectedStyle;
        public GUIStyle keyframeStyle;
        public GUIStyle keyframeContextStyle;
        public GUIStyle curveStyle;
        public GUIStyle tangentStyle;
        public GUIStyle curveCanvasStyle;
        public GUIStyle compressStyle;
        public GUIStyle expandStyle;
        public GUIStyle editCurveItemStyle;
        public GUIStyle EventItemStyle;
        public GUIStyle EventItemBottomStyle;

        public TrackStyles(GUISkin skin)
        {
            if (skin != null)
            {
                this.TrackAreaStyle = skin.FindStyle("Track Area");
                this.TrackItemStyle = skin.FindStyle("Track Item");
                this.TrackItemSelectedStyle = skin.FindStyle("TrackItemSelected");
                this.ShotTrackItemStyle = skin.FindStyle("ShotTrackItem");
                this.ShotTrackItemSelectedStyle = skin.FindStyle("ShotTrackItemSelected");
                this.AudioTrackItemStyle = skin.FindStyle("AudioTrackItem");
                this.AudioTrackItemSelectedStyle = skin.FindStyle("AudioTrackItemSelected");
                this.GlobalTrackItemStyle = skin.FindStyle("GlobalTrackItem");
                this.GlobalTrackItemSelectedStyle = skin.FindStyle("GlobalTrackItemSelected");
                this.ActorTrackItemStyle = skin.FindStyle("ActorTrackItem");
                this.ActorTrackItemSelectedStyle = skin.FindStyle("ActorTrackItemSelected");
                this.CurveTrackItemStyle = skin.FindStyle("CurveTrackItem");
                this.CurveTrackItemSelectedStyle = skin.FindStyle("CurveTrackItemSelected");
                this.keyframeStyle = skin.FindStyle("Keyframe");
                this.curveStyle = skin.FindStyle("Curve");
                this.tangentStyle = skin.FindStyle("TangentHandle");
                this.curveCanvasStyle = skin.FindStyle("CurveCanvas");
                this.compressStyle = skin.FindStyle("CompressVertical");
                this.expandStyle = skin.FindStyle("ExpandVertical");
                this.editCurveItemStyle = skin.FindStyle("EditCurveItem");
                this.EventItemStyle = skin.FindStyle("EventItem");
                this.EventItemBottomStyle = skin.FindStyle("EventItemBottom");
                this.keyframeContextStyle = skin.FindStyle("KeyframeContext");
                this.TrackSidebarBG1 = skin.FindStyle("TrackSidebarBG");
                this.TrackSidebarBG2 = skin.FindStyle("TrackSidebarBGAlt");
                this.backgroundSelected = skin.FindStyle("TrackGroupFocused");
                this.backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
            }
            else
            {
                GUI.skin = new GUISkin();
                this.TrackAreaStyle = "box";
                this.TrackItemStyle = "flow node 0";
                this.TrackItemSelectedStyle = "flow node 0 on";
                this.ShotTrackItemStyle = "flow node 1";
                this.ShotTrackItemSelectedStyle = "flow node 1 on";
                this.AudioTrackItemStyle = "flow node 2";
                this.AudioTrackItemSelectedStyle = "flow node 2 on";
                this.GlobalTrackItemStyle = "flow node 3";
                this.GlobalTrackItemSelectedStyle = "flow node 3 on";
                this.ActorTrackItemStyle = "flow node 4";
                this.ActorTrackItemSelectedStyle = "flow node 4 on";
                this.CurveTrackItemStyle = "flow node 0";
                this.CurveTrackItemSelectedStyle = "flow node 0 on";
                this.keyframeStyle = "Dopesheetkeyframe";
                this.curveStyle = "box";
                this.tangentStyle = "box";
                this.curveCanvasStyle = "box";
                this.compressStyle = "box";
                this.expandStyle = "box";
                this.editCurveItemStyle = "box";
                this.EventItemStyle = "box";
                this.EventItemBottomStyle = "box";
                this.keyframeContextStyle = "box";
                this.TrackSidebarBG1 = "box";
                this.TrackSidebarBG2 = "box";
                this.backgroundSelected = "box";
                this.backgroundContentSelected = "box";
            }
        }

        public GUIStyle TrackItemStyle
        {
            get
            {
                if (this.trackItemStyle == null)
                {
                    this.trackItemStyle = "box";
                }
                return this.trackItemStyle;
            }
            set => 
                (this.trackItemStyle = value);
        }

        public GUIStyle CurveTrackItemStyle
        {
            get
            {
                if (this.curveTrackItemStyle == null)
                {
                    this.curveTrackItemStyle = "box";
                }
                return this.curveTrackItemStyle;
            }
            set => 
                (this.curveTrackItemStyle = value);
        }

        public GUIStyle TrackItemSelectedStyle
        {
            get
            {
                if (this.trackItemSelectedStyle == null)
                {
                    this.trackItemSelectedStyle = "box";
                }
                return this.trackItemSelectedStyle;
            }
            set => 
                (this.trackItemSelectedStyle = value);
        }

        public GUIStyle CurveTrackItemSelectedStyle
        {
            get
            {
                if (this.curveTrackItemSelectedStyle == null)
                {
                    this.curveTrackItemSelectedStyle = "box";
                }
                return this.curveTrackItemSelectedStyle;
            }
            set => 
                (this.curveTrackItemSelectedStyle = value);
        }
    }
}

