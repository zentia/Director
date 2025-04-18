using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class ActionFixedItemControl : ActionItemControl
{
    private static float mouseDownOffset = -1f;

    [field: CompilerGenerated]
    public event ActionFixedItemEventHandler AlterFixedAction;

    internal override void ConfirmTranslate()
    {
        CinemaActionFixedWrapper wrapper = base.Wrapper as CinemaActionFixedWrapper;
        if ((wrapper != null) && (this.AlterFixedAction != null))
        {
            this.AlterFixedAction(this, new ActionFixedItemEventArgs(wrapper.Behaviour, wrapper.Firetime, wrapper.Duration, wrapper.InTime, wrapper.OutTime));
        }
    }

    public override void HandleInput(DirectorControlState state, Rect trackPosition)
    {
        CinemaActionFixedWrapper wrapper = base.Wrapper as CinemaActionFixedWrapper;
        if (wrapper == null)
        {
            return;
        }
        if (base.isRenaming)
        {
            return;
        }
        float x = state.TimeToPosition(wrapper.Firetime);
        float num2 = state.TimeToPosition(wrapper.Firetime + wrapper.Duration);
        base.controlPosition = new Rect(x, 0f, num2 - x, trackPosition.height);
        bool flag1 = this.controlPosition.width < 15f;
        float width = flag1 ? 0f : 5f;
        Rect position = new Rect(x, 0f, width, trackPosition.height);
        Rect rect2 = new Rect(x + width, 0f, (num2 - x) - (2f * width), trackPosition.height);
        Rect rect3 = new Rect(num2 - width, 0f, width, trackPosition.height);
        EditorGUIUtility.AddCursorRect(rect2, MouseCursor.SlideArrow);
        if (!flag1)
        {
            EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rect3, MouseCursor.ResizeHorizontal);
        }
        base.controlID = GUIUtility.GetControlID(wrapper.Behaviour.GetInstanceID(), FocusType.Passive, base.controlPosition);
        int controlID = GUIUtility.GetControlID(wrapper.Behaviour.GetInstanceID(), FocusType.Passive, position);
        int num5 = GUIUtility.GetControlID(wrapper.Behaviour.GetInstanceID(), FocusType.Passive, rect2);
        int num6 = GUIUtility.GetControlID(wrapper.Behaviour.GetInstanceID(), FocusType.Passive, rect3);
        if (((Event.current.GetTypeForControl(base.controlID) == EventType.MouseDown) && rect2.Contains(Event.current.mousePosition)) && (Event.current.button == 1))
        {
            if (!base.IsSelected)
            {
                GameObject[] gameObjects = Selection.gameObjects;
                ArrayUtility.Add<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
                Selection.objects = gameObjects;
                base.hasSelectionChanged = true;
            }
            this.showContextMenu(wrapper.Behaviour);
            if (!base.TrackControl.TargetTrack.IsLocked)
            {
                Event.current.Use();
            }
        }
        switch (Event.current.GetTypeForControl(num5))
        {
            case EventType.MouseDown:
            {
                if (!rect2.Contains(Event.current.mousePosition) || (Event.current.button != 0))
                {
                    goto Label_0447;
                }
                GUIUtility.hotControl = num5;
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
                if (GUIUtility.hotControl == num5)
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
                goto Label_0447;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == num5)
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
                goto Label_0447;

            default:
                goto Label_0447;
        }
        base.mouseDragActivity = false;
        mouseDownOffset = ((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x) - wrapper.Firetime;
        if (!base.TrackControl.TargetTrack.IsLocked)
        {
            Event.current.Use();
        }
    Label_0447:
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
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    time = state.SnappedTime(time);
                    if (time >= 0f)
                    {
                        float num9 = wrapper.InTime - (wrapper.Firetime - time);
                        num9 = Mathf.Clamp(num9, 0f, wrapper.ItemLength);
                        float num10 = num9 - wrapper.InTime;
                        wrapper.InTime = num9;
                        wrapper.Firetime += num10;
                        if (this.AlterFixedAction != null)
                        {
                            this.AlterFixedAction(this, new ActionFixedItemEventArgs(wrapper.Behaviour, wrapper.Firetime, wrapper.Duration, wrapper.InTime, wrapper.OutTime));
                        }
                    }
                }
                break;
        }
        switch (Event.current.GetTypeForControl(num6))
        {
            case EventType.MouseDown:
                if (rect3.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = num6;
                    mouseDownOffset = ((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x) - wrapper.Firetime;
                    if (!base.TrackControl.TargetTrack.IsLocked)
                    {
                        Event.current.Use();
                    }
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == num6)
                {
                    mouseDownOffset = -1f;
                    GUIUtility.hotControl = 0;
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == num6)
                {
                    float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    float num12 = state.SnappedTime(time) - (wrapper.Firetime - wrapper.InTime);
                    Undo.RecordObject(wrapper.Behaviour, $"Changed {wrapper.Behaviour.name}");
                    wrapper.OutTime = Mathf.Clamp(num12, 0f, wrapper.ItemLength);
                    if (this.AlterFixedAction != null)
                    {
                        this.AlterFixedAction(this, new ActionFixedItemEventArgs(wrapper.Behaviour, wrapper.Firetime, wrapper.Duration, wrapper.InTime, wrapper.OutTime));
                    }
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
}

