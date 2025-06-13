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
    [TimelineTrackGroupControl(typeof(TrackGroup))]
    public class TimelineTrackGroupControl : SidebarControl
    {
        public static TrackGroupStyles styles;
        private readonly float sortingOptionsWidth = 32f;
        public readonly List<TimelineTrackControl> Children = new ();
        private bool isRenaming;
        protected Texture LabelPrefix;
        private Rect _position;
        private int renameControlID;
        private bool renameRequested;
        protected TimelineControlState State;
        public readonly TimelineTrackGroupWrapper Wrapper;

        public TimelineTrackGroupControl(TimelineTrackGroupWrapper wrapper)
        {
            Wrapper = wrapper;
            Wrapper.Control = this;
        }

        public override Behaviour behaviour => Wrapper.Data;
        public virtual bool IsEditing => Children.Any(child => child.IsEditing);
        protected virtual void AddTrackContext()
        {
            var trackGroup = Wrapper.Data;
            if (trackGroup != null)
            {
                List<Type> trackTypes = TimelineRuntimeHelper.GetAllowedTrackTypes(trackGroup);
                GenericMenu createMenu = new GenericMenu();

                foreach (var t in trackTypes)
                {
                    MemberInfo info = t;
                    string label = string.Empty;
                    foreach (TimelineRuntime.TimelineTrackAttribute attribute in info.GetCustomAttributes(typeof(TimelineRuntime.TimelineTrackAttribute), true))
                    {
                        label = attribute.Label;
                        break;
                    }

                    createMenu.AddItem(new GUIContent(string.Format("Add {0}", label)), false, AddTrack, new TrackContextData(label, t, trackGroup));
                }

                createMenu.ShowAsContext();
            }
        }
        protected void AddTrack(object userData)
        {
            GameObject item;
            if (userData is TrackContextData trackData)
            {
                item = TimelineFactory.CreateTimelineTrack(trackData.TrackGroup, trackData.Type, trackData.Label).gameObject;
                isExpanded = true;
                Undo.RegisterCreatedObjectUndo(item, $"Create {item.name}");
            }
        }

        protected class TrackContextData
        {
            public string Label;
            public Type Type;
            public TrackGroup TrackGroup;

            public TrackContextData(string label, Type type, TrackGroup trackGroup)
            {
                Label = label;
                Type = type;
                TrackGroup = trackGroup;
            }
        }
        internal void BindTrackControls(TimelineTrackGroupWrapper trackGroupWrapper, List<SidebarControl> newSidebarControls, List<SidebarControl> removedSidebarControls, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
        {
            if (trackGroupWrapper.HasChanged)
            {
                var flag = false;
                foreach (var wrapper in trackGroupWrapper.Children)
                {
                    TimelineTrackControl control = Children.Find(trackControl => trackControl.Wrapper == wrapper);
                    if (control == null)
                    {
                        flag = true;
                        var type = typeof(TimelineTrackControl);
                        var num = 0x7fffffff;
                        foreach (var type2 in TimelineControlHelper.GetAllSubTypes(typeof(TimelineTrackControl)))
                        {
                            Type c = null;
                            foreach (TimelineTrackAttribute attribute in type2.GetCustomAttributes(typeof(TimelineTrackAttribute), true))
                                if (attribute != null)
                                    c = attribute.TrackType;
                            if (c == wrapper.Track.GetType())
                            {
                                type = type2;
                                num = 0;
                                break;
                            }

                            if (c != null && wrapper.Track.GetType().IsSubclassOf(c))
                            {
                                var baseType = wrapper.Track.GetType();
                                var num4 = 0;
                                while (baseType != null && baseType != c)
                                {
                                    baseType = baseType.BaseType;
                                    num4++;
                                }

                                if (num4 <= num)
                                {
                                    num = num4;
                                    type = type2;
                                }
                            }
                        }

                        control = (TimelineTrackControl)Activator.CreateInstance(type);
                        control.Initialize();
                        control.TrackGroupControl = this;
                        control.Wrapper = wrapper;
                        newSidebarControls.Add(control);
                        Children.Add(control);
                    }
                }

                var list = new List<TimelineTrackControl>();
                foreach (var control in Children)
                {
                    if (control.Wrapper.Track == null)
                    {
                        Wrapper.Children.Remove(control.Wrapper);
                        Wrapper.Data.timelineTracks.Remove(control.Wrapper.Track);
                        list.Add(control);
                        continue;
                    }
                    var flag2 = false;
                    foreach (var wrapper3 in trackGroupWrapper.Children)
                        if (control.Wrapper.Equals(wrapper3))
                        {
                            flag2 = true;
                            break;
                        }

                    if (!flag2)
                    {
                        removedSidebarControls.Add(control);
                        list.Add(control);
                    }
                }

                foreach (var timelineTrackWrapper in list)
                {
                    Children.Remove(timelineTrackWrapper);
                }
            }

            foreach (var key in Children)
                key.BindTimelineItemControls(key.Wrapper, newTimelineControls, removedTimelineControls);
            trackGroupWrapper.HasChanged = false;
        }

        internal void BoxSelect(Rect selectionBox)
        {
            if (isExpanded)
                foreach (var wrapper in Children)
                    wrapper.BoxSelect(selectionBox);
        }

        public void ZoomSelectKey(Rect selectionBox, Rect bodyArea)
        {
            foreach (var control in Children)
            {
                var newSelectBox = new Rect(selectionBox.x - control.Rect.x - bodyArea.x, selectionBox.y - bodyArea.y - control.Rect.y, selectionBox.width, selectionBox.height);
                control.ZoomSelectKey(newSelectBox);
            }
        }

        public void ClearSelectCurves()
        {
            foreach (var pair in Children)
            {
                pair.ClearSelectedCurves();
            }
        }

        internal void Delete()
        {
            timelineControl.Wrapper.RemoveTrackGroup(Wrapper);
            timelineControl.Children.Remove(this);
            var timeline = Wrapper.Data.timeline;
            Undo.DestroyObjectImmediate(Wrapper.Data.gameObject);
            timeline.OnValidate();
        }

        internal void DeleteSelectedChildren()
        {
            foreach (var control in Children)
            {
                control.DeleteSelectedChildren();
                if (control.IsSelected) control.Delete();
            }
        }

        internal void Duplicate()
        {
            var objectToUndo = Object.Instantiate(Wrapper.Data.gameObject);
            var name = Wrapper.Data.gameObject.name;
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

            objectToUndo.transform.SetParent(Wrapper.Data.transform.parent, true);
            Undo.RegisterCreatedObjectUndo(objectToUndo, "Duplicate " + objectToUndo.name);
        }

        internal void DuplicateSelectedChildren()
        {
            foreach (var control in Children)
            {
                if (control.IsSelected) control.Duplicate();
            }
        }

        internal float GetHeight()
        {
            var num = 17f;
            if (isExpanded)
                foreach (var control in Children)
                    num += control.Rect.height;
            return num + 1f;
        }

        internal List<SidebarControl> GetSidebarControlChildren(bool onlyVisible)
        {
            var list = new List<SidebarControl>();
            if (isExpanded)
                foreach (var item in Children)
                {
                    list.Add(item);
                }

            return list;
        }

        public virtual void Initialize()
        {
            LabelPrefix = styles.DirectorGroupIcon.normal.background;
        }

        internal static void InitStyles(GUISkin skin)
        {
            if (styles == null)
                styles = new TrackGroupStyles(skin);
        }

        private void Rename()
        {
            renameRequested = true;
            isRenaming = true;
        }

        private void ShowHeaderContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename"), false, Rename);
            menu.AddItem(new GUIContent("Duplicate"), false, RequestDuplicate);
            menu.AddItem(new GUIContent("Delete"), false, RequestDelete);
            menu.AddItem(new GUIContent("Solo"), false, Solo);
            menu.AddItem(new GUIContent("Mute"), false, Mute);
            menu.AddItem(new GUIContent("Active"), false, Active);
            menu.ShowAsContext();
        }

        protected override void RequestDelete()
        {
            Delete();
        }

        private void Solo()
        {
            timelineControl.Children.ForEach(element =>
            {
                element.Wrapper.Data.gameObject.SetActive(element == this);
            });
        }

        private void Mute()
        {
            Wrapper.Data.gameObject.SetActive(false);
        }

        private void Active()
        {
            Wrapper.Data.gameObject.SetActive(true);
            if (State.IsInPreviewMode)
            {
                Wrapper.Data.Initialize();
            }
        }

        public void Update(TimelineControlState state, Rect position, Rect fullHeader, Rect safeHeader, Rect content)
        {
            _position = position;
            State = state;
            UpdateHeaderBackground(fullHeader);
            UpdateContentBackground(content);
            var enable = Wrapper.Data.gameObject.activeSelf;
            if (isExpanded)
            {
                GUI.enabled = enable;
                var header = new Rect(safeHeader.x, safeHeader.y, fullHeader.width, safeHeader.height);
                UpdateChildren(state, header, content);
                GUI.enabled = true;
            }
            GUI.color = enable ? Color.white : Color.gray;
            UpdateHeaderContent(safeHeader);
            GUI.color = Color.white;
        }

        protected virtual void UpdateContentBackground(Rect content)
        {
            if (Selection.Contains(Wrapper.Data.gameObject) && !isRenaming)
                GUI.Box(_position, string.Empty, styles.BackgroundContentSelected);
            else
                GUI.Box(_position, string.Empty, styles.TrackGroupArea);
        }

        protected virtual void UpdateHeaderBackground(Rect position)
        {
            if (Selection.Contains(Wrapper.Data.gameObject) && !isRenaming)
                GUI.Box(position, string.Empty, styles.BackgroundSelected);
        }

        private void UpdateHeaderContent(Rect header)
        {
            var position = new Rect(header.x + 14f, header.y, 16f, 17f);
            var rect2 = new Rect(position.x + position.width, header.y, header.width - (position.x + position.width) - 32f, 17f);
            var name = Wrapper.Data.name;
            isExpanded = EditorGUI.Foldout(new Rect(header.x, header.y, 14f, 17f), isExpanded, GUIContent.none, false);
            GUI.Box(position, LabelPrefix, GUIStyle.none);
            UpdateHeaderControl4(new Rect(header.width - 48f, header.y, 16f, 16f));
            UpdateHeaderControl5(new Rect(header.width - 32f, header.y, 16f, 16f));
            UpdateHeaderControl6(new Rect(header.width - 16f, header.y, 16f, 16f));
            if (isRenaming)
            {
                GUI.SetNextControlName("TrackGroupRename");
                name = EditorGUI.TextField(rect2, GUIContent.none, name);
                if (renameRequested)
                {
                    EditorGUI.FocusTextInControl("TrackGroupRename");
                    renameRequested = false;
                    renameControlID = GUIUtility.keyboardControl;
                }

                if (!EditorGUIUtility.editingTextField || renameControlID != GUIUtility.keyboardControl || Event.current.keyCode == KeyCode.Return ||
                    (Event.current.type == EventType.MouseDown && !rect2.Contains(Event.current.mousePosition)))
                {
                    isRenaming = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    EditorGUIUtility.editingTextField = false;
                }
            }

            if (Wrapper.Data.name != name)
            {
                Undo.RecordObject(Wrapper.Data.gameObject, $"Renamed {Wrapper.Data.name}");
                Wrapper.Data.name = name;
            }

            if (!isRenaming)
            {
                var controlID = GUIUtility.GetControlID(Wrapper.Data.GetInstanceID(), FocusType.Passive, header);
                if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
                {
                    if (header.Contains(Event.current.mousePosition) && Event.current.button == 1)
                    {
                        if (!IsSelected)
                            RequestSelect();
                        ShowHeaderContextMenu();
                        Event.current.Use();
                    }
                    else if (header.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        RequestSelect();
                        Event.current.Use();
                    }
                }

                if (IsSelected)
                    GUI.Label(rect2, name, EditorStyles.whiteLabel);
                else
                    GUI.Label(rect2, name);
            }
        }

        protected virtual void UpdateHeaderControl4(Rect position)
        {
        }

        protected virtual void UpdateHeaderControl5(Rect position)
        {
            var color = GUI.color;
            GUI.color = Wrapper.Children.Any() ? new Color(0f, 53f, 0f) : new Color(53f, 0f, 0f);
            if (GUI.Button(position, string.Empty, styles.AddIcon))
                AddTrackContext();
            GUI.color = color;
        }

        protected virtual void UpdateHeaderControl6(Rect position)
        {
        }

        private void UpdateChildren(TimelineControlState controlState, Rect header, Rect content)
        {
            var y = header.y + 17f;
            var idx = 0;
            foreach (var child in Children)
            {
                child.state = controlState;
                var height = child.Rect.height;
                var pos = new Rect(content.x, y, content.width, height);
                var rect2 = new Rect(header.x, y, header.width, height);
                var rect3 = new Rect(header.x, y, header.width - sortingOptionsWidth - 4f, height);
                if (child.Wrapper.Track == null)
                {
                    Wrapper.HasChanged = true;
                    continue;
                }
                GUI.color = child.Wrapper.Track.gameObject.activeSelf ? Color.white : Color.gray;
                child.UpdateTrackBodyBackground(pos);
                child.UpdateHeaderBackground(rect2, idx++);
                GUILayout.BeginArea(pos);
                child.UpdateChildren(pos);
                GUILayout.EndArea();
                child.UpdateHeaderContents(rect3, rect2);
                GUI.color = Color.white;
                y += height;
            }
        }
    }
}
