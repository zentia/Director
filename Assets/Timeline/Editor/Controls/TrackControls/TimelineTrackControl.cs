using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineEditor
{
    [TimelineTrack(typeof(TimelineTrack))]
    public class TimelineTrackControl : SidebarControl
    {
        private int controlID; // The control ID for this track control.
        private ContextData savedData; // Saved data from the object picker.
        public static TrackStyles styles;

        protected readonly List<TrackItemControl> Children = new ();

        protected bool isRenaming;
        private int renameControlID;
        protected bool renameRequested;
        public TimelineControlState state;
        protected Rect trackArea = new (0f, 0f, 0f, 17f);

        public Rect Rect
        {
            get
            {
                CalculateHeight();
                return trackArea;
            }
        }

        public TimelineTrackWrapper Wrapper;
        public override Behaviour behaviour => Wrapper.Track;

        public TimelineTrackGroupControl TrackGroupControl;

        public bool IsEditing=>Children.Any(child => child.IsEditing);

        internal void BindTimelineItemControls(TimelineTrackWrapper track, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
        {
            if (Wrapper.HasChanged)
            {
                foreach (var wrapper in track.Children)
                {
                    if (Children.Find((itemControl => itemControl.Wrapper == wrapper)) == null)
                    {
                        var type = typeof(TrackItemControl);
                        var num = 0x7fffffff;
                        var drawPriority = 0;
                        foreach (var type2 in TimelineControlHelper.GetAllSubTypes(typeof(TrackItemControl)))
                        {
                            foreach (var attribute in type2.GetCustomAttributes(typeof(TimelineItemControlAttribute), true).Cast<TimelineItemControlAttribute>())
                            {
                                if (attribute != null)
                                {
                                    var subTypeDepth = TimelineControlHelper.GetSubTypeDepth(wrapper.timelineItem.GetType(), attribute.ItemType);
                                    if (subTypeDepth < num)
                                    {
                                        type = type2;
                                        num = subTypeDepth;
                                        drawPriority = attribute.DrawPriority;
                                    }
                                }
                            }
                        }

                        var control = (TrackItemControl)Activator.CreateInstance(type);
                        control.DrawPriority = drawPriority;
                        control.Initialize(wrapper);
                        control.Parent = this;
                        InitializeTrackItemControl(control);
                        newTimelineControls.Add(control);
                        Children.Add(control);
                    }
                }

                var list = new List<TrackItemControl>();
                foreach (var trackItemControl in Children)
                {
                    var flag = false;
                    foreach (var timelineItemWrapper in track.Children)
                    {
                        if (trackItemControl.Wrapper == timelineItemWrapper)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        PrepareTrackItemControlForRemoval(trackItemControl);
                        removedTimelineControls.Add(trackItemControl);
                        list.Add(trackItemControl);
                    }
                }

                foreach (var wrapper4 in list)
                    Children.Remove(wrapper4);
            }

            track.HasChanged = false;
        }

        internal void BoxSelect(Rect selectionBox)
        {
            foreach (var wrapper in Children)
            {
                wrapper.BoxSelect(selectionBox);
            }
        }

        public void ZoomSelectKey(Rect selectionBox)
        {
            foreach (var control in Children)
            {
                control.ZoomSelectKey(selectionBox);
            }
        }

        public void ClearSelectedCurves()
        {
            foreach (var wrapper in Children)
            {
                var data = wrapper.Wrapper as TimelineClipCurveWrapper;
                if (data != null)
                {
                    var data1 = wrapper as TimelineCurveClipItemControl;
                    if (data1 != null)
                        data1.ClearSelectedCurves();
                }
            }
        }

        protected virtual void CalculateHeight()
        {
            trackArea.height = 17f;
            if (isExpanded)
                trackArea.height = 17f * expandedSize;
        }

        internal void Delete()
        {
            Wrapper.GroupWrapper.RemoveTrack(this);
            var trackGroup = Wrapper.GroupWrapper.Data;
            Undo.DestroyObjectImmediate(Wrapper.Track.gameObject);
            trackGroup.OnValidate();
        }

        private void Solo()
        {
            TrackGroupControl.Children.ForEach(element =>
            {
                element.Wrapper.Track.gameObject.SetActive(element == this);
            });
        }

        private void Mute()
        {
            Wrapper.Track.gameObject.SetActive(false);
        }
        private void Active()
        {
            Wrapper.Track.gameObject.SetActive(true);
        }

        internal void DeleteSelectedChildren()
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var control = Children[i];
                if (!control.IsSelected)
                {
                    continue;
                }
                control.Delete();
                Children.Remove(control);
            }
            Wrapper.Track.OnValidate();
        }

        internal void Duplicate()
        {
            var objectToUndo = Object.Instantiate(Wrapper.Track.gameObject);
            var name = Wrapper.Track.gameObject.name;
            var s = Regex.Match(name, @"(\d+)$").Value;
            int result;
            if (int.TryParse(s, out result))
            {
                result++;
                objectToUndo.name = name.Substring(0, name.Length - s.Length) + result;
            }
            else
            {
                objectToUndo.name = name.Substring(0, name.Length - s.Length) + " " + 1;
            }

            objectToUndo.transform.SetParent(Wrapper.Track.transform.parent, true);
            Undo.RegisterCreatedObjectUndo(objectToUndo, "Duplicate " + objectToUndo.name);
        }

        public virtual void Initialize()
        {
        }

        protected void InitializeTrackItemControl(TrackItemControl control)
        {
        }

        internal static void InitStyles(GUISkin skin)
        {
            if (styles == null)
                styles = new TrackStyles(skin);
        }

        private void PrepareTrackItemControlForRemoval(TrackItemControl control)
        {
        }

        private void Rename()
        {
            renameRequested = true;
            isRenaming = true;
        }

        public void UpdateHeaderBackground(Rect position, int ordinal)
        {
            if (styles != null)
            {
                if (Selection.Contains(Wrapper.Track.gameObject) && !isRenaming)
                    GUI.Box(position, string.Empty, styles.backgroundSelected);
                else if (ordinal % 2 == 0)
                    GUI.Box(position, string.Empty, styles.TrackSidebarBG2);
                else
                    GUI.Box(position, string.Empty, styles.TrackSidebarBG1);
            }
        }

        public virtual void UpdateHeaderContents(Rect position, Rect headerBackground)
        {
            var rect = new Rect(position.x + 14f, position.y, 14f, position.height);
            var rect2 = new Rect(rect.x + rect.width, position.y, position.width - 14f - 96f - 14f, position.height);
            var name = Wrapper.Track.name;
            isExpanded = EditorGUI.Foldout(rect, isExpanded, GUIContent.none, false);
            UpdateHeaderControl(new Rect(position.width - 32f, position.y, 16f, 16f));
            var controlId = GUIUtility.GetControlID(Wrapper.Track.GetInstanceID(), FocusType.Passive, position);
            if (isRenaming)
            {
                GUI.SetNextControlName("TrackRename");
                name = EditorGUI.TextField(rect2, GUIContent.none, name);
                if (renameRequested)
                {
                    EditorGUI.FocusTextInControl("TrackRename");
                    renameRequested = false;
                    renameControlID = GUIUtility.keyboardControl;
                }

                if (!EditorGUIUtility.editingTextField || renameControlID != GUIUtility.keyboardControl || Event.current.keyCode == KeyCode.Return || (Event.current.type == EventType.MouseDown && !rect2.Contains(Event.current.mousePosition)))
                {
                    isRenaming = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    EditorGUIUtility.editingTextField = false;
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
                if (Event.current.GetTypeForControl(controlId) == EventType.MouseDown)
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

        private void UpdateHeaderControl5(Rect position)
        {
        }

        public void UpdateTrackBodyBackground(Rect position)
        {

        }

        public void UpdateChildren(Rect position)
        {
            trackArea = position;
            foreach (var control in Children)
            {
                control.ControlState = state;
                control.PreUpdate(state, position);
                control.HandleInput(state, position);
                control.Draw(state);
                control.PostUpdate(state);
            }
            var rect = new Rect(0f, 0f, position.width, position.height);
            var controlID = GUIUtility.GetControlID(Wrapper.Track.GetInstanceID(), FocusType.Passive, rect);
            if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 1 && !Event.current.alt && !Event.current.shift && !Event.current.control)
            {
                ShowBodyContextMenu(Event.current, state);
                Event.current.Use();
            }
        }
        protected virtual void UpdateHeaderControl(Rect position)
        {
            var track = Wrapper.Track;
            if (track == null) return;

            var temp = GUI.color;
            GUI.color = track.timelineItems.Count > 0 ? Color.green : Color.red;
            controlID = GUIUtility.GetControlID(track.GetInstanceID(), FocusType.Passive);

            if (GUI.Button(position, string.Empty, TimelineTrackGroupControl.styles.AddIcon))
            {
                var trackTypes = track.GetAllowedTimelineItems();

                if (trackTypes.Count == 1)
                {
                    var data = GetContextData(trackTypes[0]);
                    if (data.PairedType == null)
                        AddTimelineItem(data);
                    else
                        ShowObjectPicker(data);
                }
                else if (trackTypes.Count > 1)
                {
                    var createMenu = new GenericMenu();
                    for (var i = 0; i < trackTypes.Count; i++)
                    {
                        var data = GetContextData(trackTypes[i]);

                        createMenu.AddItem(new GUIContent(string.Format("{0}/{1}", data.Category, data.Label)), false, AddTimelineItem, data);
                    }

                    createMenu.ShowAsContext();
                }
            }

            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorClosed")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == controlID)
                {
                    var pickedObject = EditorGUIUtility.GetObjectPickerObject();

                    if (pickedObject != null)
                        AddTimelineItem(savedData, pickedObject);

                    Event.current.Use();
                }
            }
            GUI.color = temp;
        }

        protected virtual void ShowBodyContextMenu(Event evt, TimelineControlState state = null)
        {
            var itemTrack = Wrapper.Track;
            if (itemTrack == null) return;

            var trackTypes = itemTrack.GetAllowedTimelineItems();
            var createMenu = new GenericMenu();
            for (var i = 0; i < trackTypes.Count; i++)
            {
                var data = GetContextData(trackTypes[i]);
                data.Frame = TimelineUtility.TimeToFrame((evt.mousePosition.x - state.Translation.x) / state.Scale.x);
                createMenu.AddItem(new GUIContent(string.Format("Add New/{0}/{1}", data.Category, data.Label)), false, AddTimelineItem, data);
            }

            var b = TimelineCopyPaste.Peek();
            var pasteContext = new PasteContext(evt.mousePosition, itemTrack);
            if (b != null && TimelineHelper.IsTrackItemValidForTrack(b, itemTrack))
                createMenu.AddItem(new GUIContent("Paste"), false, PasteItem, pasteContext);
            else
                createMenu.AddDisabledItem(new GUIContent("Paste"));
            createMenu.ShowAsContext();
        }

        private void ShowObjectPicker(ContextData data)
        {
            var method = typeof(EditorGUIUtility).GetMethod("ShowObjectPicker");
            var generic = method.MakeGenericMethod(data.PairedType);
            generic.Invoke(this, new object[] { null, false, string.Empty, data.ControlID });

            savedData = data;
        }

        private void AddTimelineItem(object userData)
        {
            var data = userData as ContextData;
            if (data != null)
            {
                if (data.PairedType == null)
                {
                    var item = TimelineFactory.CreateTimelineItem(data.Track, data.Type, data.Label, data.Frame).gameObject;
                    Undo.RegisterCreatedObjectUndo(item, string.Format("Create {0}", item.name));
                }
                else
                {
                    ShowObjectPicker(data);
                }
            }
        }

        private static void AddTimelineItem(ContextData data, Object pickedObject)
        {
            var item = TimelineFactory.CreateTimelineItem(data.Track, data.Type, pickedObject, data.Frame).gameObject;
            Undo.RegisterCreatedObjectUndo(item, $"Create {item.name}");
        }

        private ContextData GetContextData(Type type)
        {
            MemberInfo info = type;
            var label = string.Empty;
            var category = string.Empty;
            Type requiredObject = null;
            foreach (TimelineItemAttribute attribute in info.GetCustomAttributes(typeof(TimelineItemAttribute), true))
            {
                label = attribute.Label;
                category = attribute.Category;
                requiredObject = attribute.RequiredObjectType;
                break;
            }

            return new ContextData(controlID, type, requiredObject, Wrapper.Track, category, label, state.scrubberPositionFrame);
        }

        private void PasteItem(object userData)
        {
            var data = userData as PasteContext;
            if (data != null)
            {
                var firetime = (data.mousePosition.x - state.Translation.x) / state.Scale.x;
                var clone = TimelineCopyPaste.Paste(data.track.transform);
                clone.GetComponent<TimelineItem>().Firetime = firetime;
                Undo.RegisterCreatedObjectUndo(clone, "Pasted " + clone.name);
            }
        }
        protected virtual void ShowHeaderContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename"), false, Rename);
            menu.AddItem(new GUIContent("Duplicate"), false, RequestDuplicate);
            menu.AddItem(new GUIContent("Delete"), false, Delete);
            menu.AddItem(new GUIContent("Solo"), false, Solo);
            menu.AddItem(new GUIContent("Mute"), false, Mute);
            menu.AddItem(new GUIContent("Active"), false, Active);
            menu.ShowAsContext();
        }

        private class ContextData
        {
            public readonly string Category;
            public readonly int ControlID;
            public int Frame;
            public readonly string Label;
            public readonly Type PairedType;
            public readonly TimelineTrack Track;
            public readonly Type Type;

            public ContextData(int controlId, Type type, Type pairedType, TimelineTrack track, string category, string label, int frame)
            {
                ControlID = controlId;
                Type = type;
                PairedType = pairedType;
                Track = track;
                Category = category;
                Label = label;
                Frame = frame;
            }
        }

        private class PasteContext
        {
            public readonly Vector2 mousePosition;
            public readonly TimelineTrack track;

            public PasteContext(Vector2 mousePosition, TimelineTrack track)
            {
                this.mousePosition = mousePosition;
                this.track = track;
            }
        }
    }
}
