using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class ActionItemControl : TrackItemControl
    {
        private static float mouseDownOffset = -1f;
        protected Texture actionIcon;
        public event ActionItemEventHandler AlterAction;

        internal override void ConfirmTranslate()
        {
            var wrapper = Wrapper as TimelineActionWrapper;
            if (wrapper != null && AlterAction != null)
                AlterAction(this, new ActionItemEventArgs(wrapper.timelineItem, wrapper.fireTime, wrapper.Duration));
        }

        public override void Draw(TimelineControlState state)
        {
            if (Wrapper.timelineItem != null)
            {
                var name = Wrapper.timelineItem.name;
                if (isRenaming)
                {
                    GUI.Box(ControlPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
                    GUI.SetNextControlName("TrackItemControlRename");
                    name = EditorGUI.TextField(ControlPosition, GUIContent.none, name);
                    if (renameRequested)
                    {
                        EditorGUI.FocusTextInControl("TrackItemControlRename");
                        renameRequested = false;
                    }

                    if (!IsSelected || Event.current.keyCode == KeyCode.Return || ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.Ignore) && !ControlPosition.Contains(Event.current.mousePosition)))
                    {
                        isRenaming = false;
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        var drawPriority = DrawPriority;
                        DrawPriority = drawPriority - 1;
                    }
                }

                if (Wrapper.timelineItem.name != name)
                {
                    Undo.RecordObject(Wrapper.timelineItem.gameObject, $"Renamed {Wrapper.timelineItem.name}");
                    Wrapper.timelineItem.name = name;
                }

                if (!isRenaming)
                {
                    GUI.Box(ControlPosition, new GUIContent(name), IsSelected ? TimelineTrackControl.styles.TrackItemSelectedStyle : TimelineTrackControl.styles.TrackItemStyle);
                }
            }
        }

        public override void HandleInput(TimelineControlState state, Rect trackPosition)
        {
            var wrapper = Wrapper as TimelineActionWrapper;
            if (wrapper == null)
                return;
            if (isRenaming)
                return;
            var x = wrapper.fireTime * state.Scale.x + state.Translation.x;
            var num2 = (wrapper.fireTime + wrapper.Duration) * state.Scale.x + state.Translation.x;
            ControlPosition = new Rect(x, 0f, num2 - x, trackPosition.height);
            var position = new Rect(x, 0f, 5f, ControlPosition.height);
            var rect2 = new Rect(x + 5f, 0f, num2 - x - 10f, ControlPosition.height);
            var rect3 = new Rect(num2 - 5f, 0f, 5f, ControlPosition.height);
            EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rect2, MouseCursor.SlideArrow);
            EditorGUIUtility.AddCursorRect(rect3, MouseCursor.ResizeHorizontal);
            this.controlID = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, ControlPosition);
            var controlID = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, position);
            var num4 = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, rect2);
            var num5 = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, rect3);
            if (Event.current.GetTypeForControl(this.controlID) == EventType.MouseDown && rect2.Contains(Event.current.mousePosition) && Event.current.button == 1)
            {
                if (!IsSelected)
                {
                    var gameObjects = Selection.gameObjects;
                    ArrayUtility.Add(ref gameObjects, Wrapper.timelineItem.gameObject);
                    Selection.objects = gameObjects;
                    hasSelectionChanged = true;
                }

                ShowContextMenu(Wrapper.timelineItem);
                Event.current.Use();
            }

            switch (Event.current.GetTypeForControl(num4))
            {
                case EventType.MouseDown:
                {
                    if (!rect2.Contains(Event.current.mousePosition) || Event.current.button != 0) goto Label_0471;
                    GUIUtility.hotControl = num4;
                    if (!Event.current.control)
                    {
                        if (!IsSelected)
                            Selection.activeInstanceID = Wrapper.timelineItem.GetInstanceID();
                        break;
                    }

                    if (!IsSelected)
                    {
                        var array = Selection.gameObjects;
                        ArrayUtility.Add(ref array, Wrapper.timelineItem.gameObject);
                        Selection.objects = array;
                        hasSelectionChanged = true;
                        break;
                    }

                    var gameObjects = Selection.gameObjects;
                    ArrayUtility.Remove(ref gameObjects, Wrapper.timelineItem.gameObject);
                    Selection.objects = gameObjects;
                    hasSelectionChanged = true;
                    break;
                }
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == num4)
                    {
                        mouseDownOffset = -1f;
                        GUIUtility.hotControl = 0;
                        if (mouseDragActivity)
                        {
                            TriggerTrackItemUpdateEvent();
                        }
                        else if (!Event.current.control)
                        {
                            Selection.activeInstanceID = Wrapper.timelineItem.GetInstanceID();
                        }
                        else if (!hasSelectionChanged)
                        {
                            if (!IsSelected)
                            {
                                var gameObjects = Selection.gameObjects;
                                ArrayUtility.Add(ref gameObjects, Wrapper.timelineItem.gameObject);
                                Selection.objects = gameObjects;
                            }
                            else
                            {
                                var gameObjects = Selection.gameObjects;
                                ArrayUtility.Remove(ref gameObjects, Wrapper.timelineItem.gameObject);
                                Selection.objects = gameObjects;
                            }
                        }

                        hasSelectionChanged = false;
                    }

                    goto Label_0471;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == num4 && !hasSelectionChanged)
                    {
                        Undo.RecordObject(Wrapper.timelineItem, $"Changed {Wrapper.timelineItem.name}");
                        var fireTime = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                        fireTime -= mouseDownOffset;
                        if (!mouseDragActivity) mouseDragActivity = !(Wrapper.fireTime == fireTime);
                        TriggerRequestTrackItemTranslate(fireTime);
                    }

                    goto Label_0471;

                default:
                    goto Label_0471;
            }

            mouseDragActivity = false;
            mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - wrapper.fireTime;
            Event.current.Use();
        Label_0471:
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - wrapper.fireTime;
                        Event.current.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        mouseDownOffset = -1f;
                        GUIUtility.hotControl = 0;
                        AlterAction?.Invoke(this, new ActionItemEventArgs(wrapper.timelineItem, wrapper.fireTime, wrapper.Duration));
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        var time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                        var a = 0f;
                        var num9 = wrapper.fireTime + wrapper.Duration;
                        foreach (TimelineActionWrapper wrapper2 in Parent.Wrapper.Children)
                            if (wrapper2 != null && wrapper2.timelineItem != Wrapper.timelineItem)
                            {
                                var b = wrapper2.fireTime + wrapper2.Duration;
                                if (b <= Wrapper.fireTime) a = Mathf.Max(a, b);
                            }

                        time = Mathf.Max(a, time);
                        time = Mathf.Min(num9, time);
                        wrapper.Duration += Wrapper.fireTime - time;
                        wrapper.fireTime = time;
                    }

                    break;
            }

            switch (Event.current.GetTypeForControl(num5))
            {
                case EventType.MouseDown:
                    if (rect3.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = num5;
                        mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - Wrapper.fireTime;
                        Event.current.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == num5)
                    {
                        mouseDownOffset = -1f;
                        GUIUtility.hotControl = 0;
                        AlterAction?.Invoke(this, new ActionItemEventArgs(wrapper.timelineItem, wrapper.fireTime, wrapper.Duration));
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == num5)
                    {
                        var time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                        var positiveInfinity = float.PositiveInfinity;
                        foreach (TimelineActionWrapper wrapper3 in Parent.Wrapper.Children)
                            if (wrapper3 != null && wrapper3.timelineItem != Wrapper.timelineItem)
                            {
                                var num13 = wrapper.fireTime + wrapper.Duration;
                                if (wrapper3.fireTime >= num13) positiveInfinity = Mathf.Min(positiveInfinity, wrapper3.fireTime);
                            }

                        time = Mathf.Clamp(time, Wrapper.fireTime, positiveInfinity);
                        wrapper.Duration = time - Wrapper.fireTime;
                    }

                    break;
            }

            if (Selection.activeGameObject == Wrapper.timelineItem.gameObject)
            {
                if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy")
                    Event.current.Use();
                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Copy")
                {
                    TimelineCopyPaste.Copy(Wrapper.timelineItem);
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && Selection.activeGameObject == Wrapper.timelineItem.gameObject)
            {
                DeleteItem(Wrapper.timelineItem);
                Event.current.Use();
            }
        }

        internal override float RequestTranslate(float amount)
        {
            return amount;
        }
    }
}
