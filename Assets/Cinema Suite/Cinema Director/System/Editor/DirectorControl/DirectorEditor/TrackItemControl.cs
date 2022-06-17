using DirectorEditor;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class TrackItemControl : DirectorBehaviourControl
{
    private TimelineItemWrapper wrapper;
    private TimelineTrackWrapper track;
    private TimelineTrackControl trackControl;
    protected Rect controlPosition;
    protected int controlID;
    private int drawPriority;
    protected bool renameRequested;
    protected bool isRenaming;
    private int renameControlID;
    protected bool mouseDragActivity;
    protected bool hasSelectionChanged;

    [field: CompilerGenerated]
    public event TrackItemEventHandler AlterTrackItem;

    [field: CompilerGenerated]
    internal event TranslateTrackItemEventHandler RequestTrackItemTranslate;

    [field: CompilerGenerated]
    internal event TranslateTrackItemEventHandler TrackItemTranslate;

    [field: CompilerGenerated]
    internal event TrackItemEventHandler TrackItemUpdate;

    internal void BoxSelect(Rect selectionBox)
    {
        Rect rect = new Rect(this.controlPosition);
        rect.x += this.trackControl.Rect.x;
        rect.y += this.trackControl.Rect.y;
        if (rect.Overlaps(selectionBox, true))
        {
            GameObject[] gameObjects = Selection.gameObjects;
            ArrayUtility.Add<GameObject>(ref gameObjects, this.wrapper.Behaviour.gameObject);
            Selection.objects = gameObjects;
        }
        else if (Selection.Contains(this.wrapper.Behaviour.gameObject))
        {
            GameObject[] gameObjects = Selection.gameObjects;
            ArrayUtility.Remove<GameObject>(ref gameObjects, this.wrapper.Behaviour.gameObject);
            Selection.objects = gameObjects;
        }
    }

    internal virtual void ConfirmTranslate()
    {
        if (this.AlterTrackItem != null)
        {
            this.AlterTrackItem(this, new TrackItemEventArgs(this.wrapper.Behaviour, this.wrapper.Firetime));
        }
    }

    protected void copyItem(object userData)
    {
        Behaviour behaviour = userData as Behaviour;
        if (behaviour != null)
        {
            DirectorCopyPaste.Copy(behaviour);
        }
    }

    internal void Delete()
    {
        Undo.DestroyObjectImmediate(this.Wrapper.Behaviour.gameObject);
    }

    protected void deleteItem(object userData)
    {
        Behaviour behaviour = userData as Behaviour;
        if (behaviour != null)
        {
            base.Behaviour = behaviour;
            base.RequestDelete();
        }
    }

    public virtual void Draw(DirectorControlState state)
    {
        Behaviour behaviour = this.wrapper.Behaviour;
        if (behaviour != null)
        {
            if (base.IsSelected)
            {
                GUI.color = new Color(0.5f, 0.6f, 0.905f, 1f);
            }
            Rect controlPosition = this.controlPosition;
            controlPosition.height = 17f;
            GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.EventItemStyle);
            if (this.trackControl.isExpanded)
            {
                GUI.Box(new Rect(this.controlPosition.x, controlPosition.yMax, this.controlPosition.width, this.controlPosition.height - controlPosition.height), GUIContent.none, TimelineTrackControl.styles.EventItemBottomStyle);
            }
            GUI.color = GUI.color;
            Rect labelPosition = new Rect(this.controlPosition.x + 16f, this.controlPosition.y, 128f, this.controlPosition.height);
            string name = behaviour.name;
            this.DrawRenameLabel(name, labelPosition, null);
        }
    }

    protected virtual void DrawRenameLabel(string name, Rect labelPosition, GUIStyle labelStyle = null)
    {
        if (this.isRenaming)
        {
            GUI.SetNextControlName("TrackItemControlRename");
            name = EditorGUI.TextField(labelPosition, GUIContent.none, name);
            if (this.renameRequested)
            {
                EditorGUI.FocusTextInControl("TrackItemControlRename");
                this.renameRequested = false;
                this.renameControlID = GUIUtility.keyboardControl;
            }
            if ((!EditorGUIUtility.editingTextField || (this.renameControlID != GUIUtility.keyboardControl)) || ((Event.current.keyCode == KeyCode.Return) || ((Event.current.type == EventType.MouseDown) && !labelPosition.Contains(Event.current.mousePosition))))
            {
                this.isRenaming = false;
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                EditorGUIUtility.editingTextField = false;
                int drawPriority = this.DrawPriority;
                this.DrawPriority = drawPriority - 1;
            }
        }
        if (base.Behaviour.name != name)
        {
            Undo.RecordObject(base.Behaviour.gameObject, $"Renamed {base.Behaviour.name}");
            base.Behaviour.name = name;
        }
        if (!this.isRenaming)
        {
            if (base.IsSelected)
            {
                GUI.Label(labelPosition, base.Behaviour.name, EditorStyles.whiteLabel);
            }
            else
            {
                GUI.Label(labelPosition, base.Behaviour.name);
            }
        }
    }

    public virtual void HandleInput(DirectorControlState state, Rect trackPosition)
    {
        Behaviour objectToUndo = this.wrapper.Behaviour;
        if (objectToUndo == null)
        {
            return;
        }
        float num = (this.wrapper.Firetime * state.Scale.x) + state.Translation.x;
        this.controlPosition = new Rect(num - 8f, 0f, 16f, trackPosition.height);
        this.controlID = GUIUtility.GetControlID(this.wrapper.Behaviour.GetInstanceID(), FocusType.Passive, this.controlPosition);
        switch (Event.current.GetTypeForControl(this.controlID))
        {
            case EventType.MouseDown:
            {
                if (!this.controlPosition.Contains(Event.current.mousePosition) || (Event.current.button != 0))
                {
                    goto Label_0183;
                }
                GUIUtility.hotControl = this.controlID;
                if (!Event.current.control)
                {
                    if (!base.IsSelected)
                    {
                        Selection.activeInstanceID = objectToUndo.GetInstanceID();
                    }
                    break;
                }
                if (!base.IsSelected)
                {
                    GameObject[] array = Selection.gameObjects;
                    ArrayUtility.Add<GameObject>(ref array, this.Wrapper.Behaviour.gameObject);
                    Selection.objects = array;
                    this.hasSelectionChanged = true;
                    break;
                }
                GameObject[] gameObjects = Selection.gameObjects;
                ArrayUtility.Remove<GameObject>(ref gameObjects, this.Wrapper.Behaviour.gameObject);
                Selection.objects = gameObjects;
                this.hasSelectionChanged = true;
                break;
            }
            case EventType.MouseUp:
                if (GUIUtility.hotControl == this.controlID)
                {
                    GUIUtility.hotControl = 0;
                    if (this.mouseDragActivity)
                    {
                        if (this.TrackItemUpdate != null)
                        {
                            this.TrackItemUpdate(this, new TrackItemEventArgs(this.wrapper.Behaviour, this.wrapper.Firetime));
                        }
                    }
                    else if (!Event.current.control)
                    {
                        Selection.activeInstanceID = objectToUndo.GetInstanceID();
                    }
                    else if (!this.hasSelectionChanged)
                    {
                        if (!base.IsSelected)
                        {
                            GameObject[] gameObjects = Selection.gameObjects;
                            ArrayUtility.Add<GameObject>(ref gameObjects, this.Wrapper.Behaviour.gameObject);
                            Selection.objects = gameObjects;
                        }
                        else
                        {
                            GameObject[] gameObjects = Selection.gameObjects;
                            ArrayUtility.Remove<GameObject>(ref gameObjects, this.Wrapper.Behaviour.gameObject);
                            Selection.objects = gameObjects;
                        }
                    }
                    this.hasSelectionChanged = false;
                }
                goto Label_03AD;

            case EventType.MouseDrag:
                if ((GUIUtility.hotControl == this.controlID) && !this.hasSelectionChanged)
                {
                    Undo.RecordObject(objectToUndo, $"Changed {objectToUndo.name}");
                    float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    time = state.SnappedTime(time);
                    if (!this.mouseDragActivity)
                    {
                        this.mouseDragActivity = !(this.Wrapper.Firetime == time);
                    }
                    if (this.RequestTrackItemTranslate != null)
                    {
                        float firetime = time - this.wrapper.Firetime;
                        float num4 = this.RequestTrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, firetime));
                        if (this.TrackItemTranslate != null)
                        {
                            this.TrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, num4));
                        }
                    }
                }
                goto Label_03AD;

            default:
                goto Label_03AD;
        }
        this.mouseDragActivity = false;
        if (!this.TrackControl.TargetTrack.IsLocked)
        {
            Event.current.Use();
        }
    Label_0183:
        if (this.controlPosition.Contains(Event.current.mousePosition) && (Event.current.button == 1))
        {
            if (!base.IsSelected)
            {
                GameObject[] gameObjects = Selection.gameObjects;
                ArrayUtility.Add<GameObject>(ref gameObjects, this.Wrapper.Behaviour.gameObject);
                Selection.objects = gameObjects;
                this.hasSelectionChanged = true;
            }
            this.showContextMenu(objectToUndo);
            Event.current.Use();
        }
    Label_03AD:
        if (Selection.activeGameObject == objectToUndo.gameObject)
        {
            if ((Event.current.type == EventType.ValidateCommand) && (Event.current.commandName == "Copy"))
            {
                Event.current.Use();
            }
            if ((Event.current.type == EventType.ExecuteCommand) && (Event.current.commandName == "Copy"))
            {
                DirectorCopyPaste.Copy(objectToUndo);
                Event.current.Use();
            }
        }
        if (((Event.current.type == EventType.KeyDown) && (Event.current.keyCode == KeyCode.Delete)) && (Selection.activeGameObject == objectToUndo.gameObject))
        {
            this.deleteItem(objectToUndo);
            Event.current.Use();
        }
    }

    public virtual void Initialize(TimelineItemWrapper wrapper, TimelineTrackWrapper track)
    {
        this.wrapper = wrapper;
        this.track = track;
    }

    public virtual void PostUpdate(DirectorControlState state)
    {
    }

    public virtual void PreUpdate(DirectorControlState state, Rect trackPosition)
    {
    }

    protected void renameItem(object userData)
    {
        if (userData is Behaviour)
        {
            this.renameRequested = true;
            this.isRenaming = true;
            int drawPriority = this.DrawPriority;
            this.DrawPriority = drawPriority + 1;
        }
    }

    internal virtual float RequestTranslate(float amount)
    {
        float b = this.Wrapper.Firetime + amount;
        float num2 = Mathf.Max(0f, b);
        return (amount + (num2 - b));
    }

    protected virtual void showContextMenu(Behaviour behaviour)
    {
        GenericMenu menu1 = new GenericMenu();
        menu1.AddItem(new GUIContent("Rename"), false, new GenericMenu.MenuFunction2(this.renameItem), behaviour);
        menu1.AddItem(new GUIContent("Copy"), false, new GenericMenu.MenuFunction2(this.copyItem), behaviour);
        menu1.AddItem(new GUIContent("Delete"), false, new GenericMenu.MenuFunction2(this.deleteItem), behaviour);
        menu1.ShowAsContext();
    }

    internal virtual void Translate(float amount)
    {
        TimelineItemWrapper wrapper = this.Wrapper;
        wrapper.Firetime += amount;
    }

    protected void TriggerRequestTrackItemTranslate(float firetime)
    {
        if (this.RequestTrackItemTranslate != null)
        {
            float num = firetime - this.wrapper.Firetime;
            float num2 = this.RequestTrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, num));
            if (this.TrackItemTranslate != null)
            {
                this.TrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, num2));
            }
        }
    }

    protected void TriggerTrackItemUpdateEvent()
    {
        if (this.TrackItemUpdate != null)
        {
            this.TrackItemUpdate(this, new TrackItemEventArgs(this.wrapper.Behaviour, this.wrapper.Firetime));
        }
    }

    public TimelineItemWrapper Wrapper
    {
        get => 
            this.wrapper;
        set
        {
            this.wrapper = value;
            base.Behaviour = value.Behaviour;
        }
    }

    public TimelineTrackWrapper Track
    {
        get => 
            this.track;
        set => 
            (this.track = value);
    }

    public TimelineTrackControl TrackControl
    {
        get => 
            this.trackControl;
        set => 
            (this.trackControl = value);
    }

    public int DrawPriority
    {
        get => 
            this.drawPriority;
        set => 
            (this.drawPriority = value);
    }
}

