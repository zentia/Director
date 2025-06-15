using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TrackItemControl : TimelineBehaviourControl
    {
        protected int controlID;

        protected Rect ControlPosition;
        protected bool hasSelectionChanged;
        protected bool isRenaming;
        protected bool mouseDragActivity;
        private int renameControlID;
        protected bool renameRequested;
        public TimelineItemWrapper Wrapper;

        public TimelineControlState ControlState;

        public TimelineTrackControl Parent;
        public bool IsEditing;
        public int DrawPriority { get; set; }

        public event TrackItemEventHandler AlterTrackItem;

        internal event TranslateTrackItemEventHandler RequestTrackItemTranslate;
        internal event TranslateTrackItemEventHandler TrackItemTranslate;
        internal event TrackItemEventHandler TrackItemUpdate;

        internal void BoxSelect(Rect selectionBox)
        {
            var rect = new Rect(ControlPosition);
            rect.x += Parent.Rect.x;
            rect.y += Parent.Rect.y;
            if (rect.Overlaps(selectionBox, true))
            {
                var gameObjects = Selection.gameObjects;
                ArrayUtility.Add(ref gameObjects, Wrapper.timelineItem.gameObject);
                Selection.objects = gameObjects;
            }
            else if (Selection.Contains(Wrapper.timelineItem.gameObject))
            {
                var gameObjects = Selection.gameObjects;
                ArrayUtility.Remove(ref gameObjects, Wrapper.timelineItem.gameObject);
                Selection.objects = gameObjects;
            }
        }

        internal virtual void ConfirmTranslate()
        {
            AlterTrackItem?.Invoke(this, new TrackItemEventArgs(Wrapper.timelineItem, Wrapper.fireTime));
        }

        protected void CopyItem(object userData)
        {
            var data = userData as Behaviour;
            if (data != null)
                TimelineCopyPaste.Copy(data);
        }

        internal void Delete()
        {
            var item = Wrapper.timelineItem;
            var actorItem = item as TimelineActorEvent;
            if (actorItem != null)
            {
                var actor = actorItem.GetActor();
                actorItem.Stop(actor == null ? null : actor.gameObject);
            }
            else
            {
                var actorAction = item as TimelineActorAction;
                if (actorAction != null)
                {
                    var actor = actorAction.Actor;
                    actorAction.Stop(actor == null ? null : actor.gameObject);
                }
                else
                {
                    item.Stop();
                }
            }
            Undo.DestroyObjectImmediate(item.gameObject);
        }

        protected void DeleteItem(object userData)
        {
            var data = userData as Behaviour;
            if (data != null)
            {
                RequestDelete();
            }
        }

        public virtual void Draw(TimelineControlState state)
        {
            var item = Wrapper.timelineItem;
            if (item != null)
            {
                var temp = GUI.color;
                if (IsSelected)
                    GUI.color = new Color(0.5f, 0.6f, 0.905f, 1f);
                var controlPosition = ControlPosition;
                controlPosition.height = 17f;
                GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.EventItemStyle);
                if (Parent.isExpanded)
                    GUI.Box(new Rect(ControlPosition.x, controlPosition.yMax, ControlPosition.width, ControlPosition.height - controlPosition.height), GUIContent.none, TimelineTrackControl.styles.EventItemBottomStyle);
                GUI.color = temp;
                var labelPosition = new Rect(ControlPosition.x + 16f, this.ControlPosition.y, 128f, this.ControlPosition.height);
                var name = item.name;
                DrawRenameLabel(name, labelPosition);
            }
        }

        protected void DrawRenameLabel(string name, Rect labelPosition)
        {
            if (isRenaming)
            {
                GUI.SetNextControlName("TrackItemControlRename");
                name = EditorGUI.TextField(labelPosition, GUIContent.none, name);
                if (renameRequested)
                {
                    EditorGUI.FocusTextInControl("TrackItemControlRename");
                    renameRequested = false;
                    renameControlID = GUIUtility.keyboardControl;
                }

                if (!EditorGUIUtility.editingTextField || renameControlID != GUIUtility.keyboardControl || Event.current.keyCode == KeyCode.Return || (Event.current.type == EventType.MouseDown && !labelPosition.Contains(Event.current.mousePosition)))
                {
                    isRenaming = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    EditorGUIUtility.editingTextField = false;
                    var drawPriority = DrawPriority;
                    DrawPriority = drawPriority - 1;
                }
            }

            if (Wrapper.timelineItem.name != name)
            {
                Undo.RecordObject(behaviour.gameObject, $"Renamed {behaviour.name}");
                Wrapper.timelineItem.name = name;
            }

            if (!isRenaming)
            {
                if (IsSelected)
                    GUI.Label(labelPosition, Wrapper.timelineItem.name, EditorStyles.whiteLabel);
                else
                    GUI.Label(labelPosition, Wrapper.timelineItem.name);
            }
        }

        public virtual void HandleInput(TimelineControlState state, Rect trackPosition)
        {
            var wrapperBehaviour = Wrapper.timelineItem;
            if (wrapperBehaviour == null)
                return;
            var num = Wrapper.fireTime * state.Scale.x + state.Translation.x;
            ControlPosition = new Rect(num - 8f, 0f, 16f, trackPosition.height);
            controlID = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, ControlPosition);
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                {
                    if (!ControlPosition.Contains(Event.current.mousePosition) || Event.current.button != 0)
                    {
                        HandleInputMouseDown(wrapperBehaviour);
                        return;
                    }
                    GUIUtility.hotControl = controlID;
                    if (!Event.current.control)
                    {
                        if (!IsSelected)
                            Selection.activeInstanceID = wrapperBehaviour.GetInstanceID();
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
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        if (mouseDragActivity)
                        {
                            TrackItemUpdate?.Invoke(this, new TrackItemEventArgs(Wrapper.timelineItem, Wrapper.fireTime));
                        }
                        else if (!Event.current.control)
                        {
                            Selection.activeInstanceID = wrapperBehaviour.GetInstanceID();
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

                    HandleInputCopy(wrapperBehaviour);
                    return;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID && !hasSelectionChanged)
                    {
                        Undo.RecordObject(wrapperBehaviour, $"Changed {wrapperBehaviour.name}");
                        var time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                        if (!mouseDragActivity) mouseDragActivity = !(Wrapper.fireTime == time);
                        if (RequestTrackItemTranslate != null)
                        {
                            var fireTime = time - Wrapper.fireTime;
                            var num4 = RequestTrackItemTranslate(this, new TrackItemEventArgs(Wrapper.timelineItem, fireTime));
                            TrackItemTranslate?.Invoke(this, new TrackItemEventArgs(Wrapper.timelineItem, num4));
                        }
                    }
                    HandleInputCopy(wrapperBehaviour);
                    return;
                default:
                    HandleInputCopy(wrapperBehaviour);
                    return;
            }

            mouseDragActivity = false;
            Event.current.Use();
            HandleInputMouseDown(wrapperBehaviour);
        }

        private void HandleInputMouseDown(Behaviour behaviour)
        {
            HandleInputSelection(behaviour);
            HandleInputCopy(behaviour);
        }

        private void HandleInputSelection(Behaviour objectToUndo)
        {
            if (ControlPosition.Contains(Event.current.mousePosition) && Event.current.button == 1)
            {
                if (!IsSelected)
                {
                    var gameObjects = Selection.gameObjects;
                    ArrayUtility.Add(ref gameObjects, Wrapper.timelineItem.gameObject);
                    Selection.objects = gameObjects;
                    hasSelectionChanged = true;
                }

                ShowContextMenu(objectToUndo);
                Event.current.Use();
            }
        }

        private void HandleInputCopy(Behaviour objectToUndo)
        {
            if (Selection.activeGameObject == objectToUndo.gameObject)
            {
                if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy") Event.current.Use();
                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Copy")
                {
                    TimelineCopyPaste.Copy(objectToUndo);
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && Selection.activeGameObject == objectToUndo.gameObject)
            {
                DeleteItem(objectToUndo);
                Event.current.Use();
            }
        }

        public virtual void ZoomSelectKey(Rect selectionBox)
        {
        }

        public void Initialize(TimelineItemWrapper wrapper)
        {
            Wrapper = wrapper;
        }

        public virtual void PostUpdate(TimelineControlState state)
        {
        }

        public virtual void PreUpdate(TimelineControlState state, Rect trackPosition)
        {
        }

        protected void RenameItem(object userData)
        {
            if (userData is Behaviour)
            {
                renameRequested = true;
                isRenaming = true;
                var drawPriority = DrawPriority;
                DrawPriority = drawPriority + 1;
            }
        }

        internal virtual float RequestTranslate(float amount)
        {
            var b = Wrapper.fireTime + amount;
            var num2 = Mathf.Max(0f, b);
            return amount + (num2 - b);
        }

        protected virtual void ShowContextMenu(Behaviour behaviour)
        {
            var menu1 = new GenericMenu();
            menu1.AddItem(new GUIContent("Rename"), false, RenameItem, behaviour);
            menu1.AddItem(new GUIContent("Copy"), false, CopyItem, behaviour);
            menu1.AddItem(new GUIContent("Delete"), false, DeleteItem, behaviour);
            menu1.ShowAsContext();
        }

        internal virtual void Translate(float amount)
        {
            Wrapper.fireTime += amount;
        }

        protected void TriggerRequestTrackItemTranslate(float fireTime)
        {
            if (RequestTrackItemTranslate != null)
            {
                var num = fireTime - Wrapper.fireTime;
                var num2 = RequestTrackItemTranslate(this, new TrackItemEventArgs(Wrapper.timelineItem, num));
                TrackItemTranslate?.Invoke(this, new TrackItemEventArgs(Wrapper.timelineItem, num2));
            }
        }

        protected void TriggerTrackItemUpdateEvent()
        {
            TrackItemUpdate?.Invoke(this, new TrackItemEventArgs(Wrapper.timelineItem, Wrapper.fireTime));
        }

        public override bool IsSelected
        {
            get
            {
                if (Wrapper.timelineItem == null)
                    return false;
                return Selection.Contains(Wrapper.timelineItem.gameObject);
            }
        }
    }
}
