using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class ActionItemControl : TrackItemControl
{
    protected const float ITEM_RESIZE_HANDLE_SIZE = 5f;
    private static float mouseDownOffset = -1f;
    protected Texture actionIcon;

    [field: CompilerGenerated]
    public event ActionItemEventHandler AlterAction;

    internal override void ConfirmTranslate()
    {
        CinemaActionWrapper wrapper = base.Wrapper as CinemaActionWrapper;
        if ((wrapper != null) && (this.AlterAction != null))
        {
            this.AlterAction(this, new ActionItemEventArgs(wrapper.Behaviour, wrapper.Firetime, wrapper.Duration));
        }
    }

    private bool doActionsConflict(float firetime, float endtime, float newFiretime, float newEndtime)
    {
        if (((newFiretime < firetime) || (newFiretime >= endtime)) && ((firetime < newFiretime) || (firetime >= newEndtime)))
        {
            return (newFiretime < 0f);
        }
        return true;
    }

    public override void Draw(DirectorControlState state)
    {
        if (base.Wrapper.Behaviour != null)
        {
            string name = base.Behaviour.name;
            if (base.isRenaming)
            {
                GUI.Box(base.controlPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
                GUI.SetNextControlName("TrackItemControlRename");
                name = EditorGUI.TextField(base.controlPosition, GUIContent.none, name);
                if (base.renameRequested)
                {
                    EditorGUI.FocusTextInControl("TrackItemControlRename");
                    base.renameRequested = false;
                }
                if ((!base.IsSelected || (Event.current.keyCode == KeyCode.Return)) || (((Event.current.type == EventType.MouseDown) || (Event.current.type == EventType.Ignore)) && !this.controlPosition.Contains(Event.current.mousePosition)))
                {
                    base.isRenaming = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    int drawPriority = base.DrawPriority;
                    base.DrawPriority = drawPriority - 1;
                }
            }
            if (base.Behaviour.name != name)
            {
                Undo.RecordObject(base.Behaviour.gameObject, $"Renamed {base.Behaviour.name}");
                base.Behaviour.name = name;
            }
            if (!base.isRenaming)
            {
                if (base.IsSelected)
                {
                    GUI.Box(base.controlPosition, new GUIContent(name), TimelineTrackControl.styles.TrackItemSelectedStyle);
                }
                else
                {
                    GUI.Box(base.controlPosition, new GUIContent(name), TimelineTrackControl.styles.TrackItemStyle);
                }
            }
        }
    }

    public override void HandleInput(DirectorControlState state, Rect trackPosition)
    {
        CinemaActionWrapper wrapper = base.Wrapper as CinemaActionWrapper;
        if (wrapper == null)
        {
            return;
        }
        if (base.isRenaming)
        {
            return;
        }
        float x = (wrapper.Firetime * state.Scale.x) + state.Translation.x;
        float num2 = ((wrapper.Firetime + wrapper.Duration) * state.Scale.x) + state.Translation.x;
        base.controlPosition = new Rect(x, 0f, num2 - x, trackPosition.height);
        Rect position = new Rect(x, 0f, 5f, this.controlPosition.height);
        Rect rect2 = new Rect(x + 5f, 0f, (num2 - x) - 10f, this.controlPosition.height);
        Rect rect3 = new Rect(num2 - 5f, 0f, 5f, this.controlPosition.height);
        EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);
        EditorGUIUtility.AddCursorRect(rect2, MouseCursor.SlideArrow);
        EditorGUIUtility.AddCursorRect(rect3, MouseCursor.ResizeHorizontal);
        base.controlID = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), FocusType.Passive, base.controlPosition);
        int controlID = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), FocusType.Passive, position);
        int num4 = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), FocusType.Passive, rect2);
        int num5 = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), FocusType.Passive, rect3);
        if (((Event.current.GetTypeForControl(base.controlID) == EventType.MouseDown) && rect2.Contains(Event.current.mousePosition)) && (Event.current.button == 1))
        {
            if (!base.IsSelected)
            {
                GameObject[] gameObjects = Selection.gameObjects;
                ArrayUtility.Add<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
                Selection.objects = gameObjects;
                base.hasSelectionChanged = true;
            }
            this.showContextMenu(base.Wrapper.Behaviour);
            Event.current.Use();
        }
        switch (Event.current.GetTypeForControl(num4))
        {
            case EventType.MouseDown:
            {
                if (!rect2.Contains(Event.current.mousePosition) || (Event.current.button != 0))
                {
                    goto Label_0471;
                }
                GUIUtility.hotControl = num4;
                if (!Event.current.control)
                {
                    if (!base.IsSelected)
                    {
                        Selection.activeInstanceID = base.Behaviour.GetInstanceID();
                    }
                    break;
                }
                if (!base.IsSelected)
                {
                    GameObject[] array = Selection.gameObjects;
                    ArrayUtility.Add<GameObject>(ref array, base.Wrapper.Behaviour.gameObject);
                    Selection.objects = array;
                    base.hasSelectionChanged = true;
                    break;
                }
                GameObject[] gameObjects = Selection.gameObjects;
                ArrayUtility.Remove<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
                Selection.objects = gameObjects;
                base.hasSelectionChanged = true;
                break;
            }
            case EventType.MouseUp:
                if (GUIUtility.hotControl == num4)
                {
                    mouseDownOffset = -1f;
                    GUIUtility.hotControl = 0;
                    if (base.mouseDragActivity)
                    {
                        base.TriggerTrackItemUpdateEvent();
                    }
                    else if (!Event.current.control)
                    {
                        Selection.activeInstanceID = base.Behaviour.GetInstanceID();
                    }
                    else if (!base.hasSelectionChanged)
                    {
                        if (!base.IsSelected)
                        {
                            GameObject[] gameObjects = Selection.gameObjects;
                            ArrayUtility.Add<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
                            Selection.objects = gameObjects;
                        }
                        else
                        {
                            GameObject[] gameObjects = Selection.gameObjects;
                            ArrayUtility.Remove<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
                            Selection.objects = gameObjects;
                        }
                    }
                    base.hasSelectionChanged = false;
                }
                goto Label_0471;

            case EventType.MouseDrag:
                if ((GUIUtility.hotControl == num4) && !base.hasSelectionChanged)
                {
                    Undo.RecordObject(base.Behaviour, $"Changed {base.Behaviour.name}");
                    float firetime = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    firetime = state.SnappedTime(firetime - mouseDownOffset);
                    if (!base.mouseDragActivity)
                    {
                        base.mouseDragActivity = !(base.Wrapper.Firetime == firetime);
                    }
                    base.TriggerRequestTrackItemTranslate(firetime);
                }
                goto Label_0471;

            default:
                goto Label_0471;
        }
        base.mouseDragActivity = false;
        mouseDownOffset = ((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x) - wrapper.Firetime;
        if (!base.TrackControl.TargetTrack.IsLocked)
        {
            Event.current.Use();
        }
    Label_0471:
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    mouseDownOffset = ((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x) - wrapper.Firetime;
                    if (!base.TrackControl.TargetTrack.IsLocked)
                    {
                        Event.current.Use();
                    }
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    mouseDownOffset = -1f;
                    GUIUtility.hotControl = 0;
                    if (this.AlterAction != null)
                    {
                        this.AlterAction(this, new ActionItemEventArgs(wrapper.Behaviour, wrapper.Firetime, wrapper.Duration));
                    }
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    time = state.SnappedTime(time);
                    float a = 0f;
                    float num9 = wrapper.Firetime + wrapper.Duration;
                    foreach (CinemaActionWrapper wrapper2 in base.Track.Items)
                    {
                        if ((wrapper2 != null) && (wrapper2.Behaviour != base.Wrapper.Behaviour))
                        {
                            float b = wrapper2.Firetime + wrapper2.Duration;
                            if (b <= base.Wrapper.Firetime)
                            {
                                a = Mathf.Max(a, b);
                            }
                        }
                    }
                    time = Mathf.Max(a, time);
                    time = Mathf.Min(num9, time);
                    wrapper.Duration += base.Wrapper.Firetime - time;
                    wrapper.Firetime = time;
                }
                break;
        }
        switch (Event.current.GetTypeForControl(num5))
        {
            case EventType.MouseDown:
                if (rect3.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = num5;
                    mouseDownOffset = ((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x) - base.Wrapper.Firetime;
                    if (!base.TrackControl.TargetTrack.IsLocked)
                    {
                        Event.current.Use();
                    }
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == num5)
                {
                    mouseDownOffset = -1f;
                    GUIUtility.hotControl = 0;
                    if (this.AlterAction != null)
                    {
                        this.AlterAction(this, new ActionItemEventArgs(wrapper.Behaviour, wrapper.Firetime, wrapper.Duration));
                    }
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == num5)
                {
                    float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    time = state.SnappedTime(time);
                    float positiveInfinity = float.PositiveInfinity;
                    foreach (CinemaActionWrapper wrapper3 in base.Track.Items)
                    {
                        if ((wrapper3 != null) && (wrapper3.Behaviour != base.Wrapper.Behaviour))
                        {
                            float num13 = wrapper.Firetime + wrapper.Duration;
                            if (wrapper3.Firetime >= num13)
                            {
                                positiveInfinity = Mathf.Min(positiveInfinity, wrapper3.Firetime);
                            }
                        }
                    }
                    time = Mathf.Clamp(time, base.Wrapper.Firetime, positiveInfinity);
                    wrapper.Duration = time - base.Wrapper.Firetime;
                }
                break;
        }
        if (Selection.activeGameObject == base.Wrapper.Behaviour.gameObject)
        {
            if ((Event.current.type == EventType.ValidateCommand) && (Event.current.commandName == "Copy"))
            {
                Event.current.Use();
            }
            if ((Event.current.type == EventType.ExecuteCommand) && (Event.current.commandName == "Copy"))
            {
                DirectorCopyPaste.Copy(base.Wrapper.Behaviour);
                Event.current.Use();
            }
        }
        if (((Event.current.type == EventType.KeyDown) && (Event.current.keyCode == KeyCode.Delete)) && (Selection.activeGameObject == base.Wrapper.Behaviour.gameObject))
        {
            base.deleteItem(base.Wrapper.Behaviour);
            Event.current.Use();
        }
    }

    internal override float RequestTranslate(float amount)
    {
        CinemaActionWrapper wrapper = base.Wrapper as CinemaActionWrapper;
        if (wrapper == null)
        {
            return 0f;
        }
        float num = base.Wrapper.Firetime + amount;
        float newFiretime = base.Wrapper.Firetime + amount;
        float num3 = newFiretime;
        bool flag = true;
        float a = 0f;
        float positiveInfinity = float.PositiveInfinity;
        float newEndtime = newFiretime + wrapper.Duration;
        foreach (CinemaActionWrapper wrapper2 in base.Track.Items)
        {
            if (((wrapper2 != null) && (wrapper2.Behaviour != wrapper.Behaviour)) && !Selection.Contains(wrapper2.Behaviour.gameObject))
            {
                float endtime = wrapper2.Firetime + wrapper2.Duration;
                float num8 = wrapper.Firetime + wrapper.Duration;
                if (this.doActionsConflict(wrapper2.Firetime, endtime, newFiretime, newEndtime))
                {
                    flag = false;
                }
                if (endtime <= wrapper.Firetime)
                {
                    a = Mathf.Max(a, endtime);
                }
                if (wrapper2.Firetime >= num8)
                {
                    positiveInfinity = Mathf.Min(positiveInfinity, wrapper2.Firetime);
                }
            }
        }
        if (flag)
        {
            num3 = Mathf.Max(0f, newFiretime);
        }
        else
        {
            newFiretime = Mathf.Max(a, newFiretime);
            num3 = Mathf.Min(positiveInfinity - wrapper.Duration, newFiretime);
        }
        return (amount + (num3 - num));
    }
}

