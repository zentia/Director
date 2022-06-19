using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
	public class TimelineTrackControl : SidebarControl
	{
        protected virtual void showBodyContextMenu(Event evt)
        {
            var itemTrack = TargetTrack.Behaviour as TimelineTrack;
            if (itemTrack == null)
                return;
            GenericMenu createMenu = new GenericMenu();
            var fireTime = (evt.mousePosition.x - state.Translation.x) / state.Scale.x;
            createMenu.AddItem(new GUIContent("Add New"), false, AddKeyframe, fireTime);
            var b = DirectorCopyPaste.Peek();
            var pasteContext = new PasteContext(evt.mousePosition, itemTrack);
            if (b != null)
            {
                createMenu.AddItem(new GUIContent("Paste"), false, pasteItem, pasteContext);
            }
            else
            {
                createMenu.AddDisabledItem(new GUIContent("Paste"));
            }
            createMenu.ShowAsContext();
        }

        public void AddKeyframe(object userData)
        {
            var fireTime = (float)userData;
			var itemTrack = TargetTrack.Behaviour as TimelineTrack;
            DirectorObject first = null;
            if (itemTrack.Children.Count > 0)
            {
                first = itemTrack.Children[0];
            }
            var item = itemTrack.CreateChild(first) as TimelineItem;
            item.Firetime = fireTime;
            var action = item as TimelineAction;
            if (action != null)
                action.Duration = 1;
            Undo.RegisterCreatedObjectUndo(item, string.Format("Create {0}", item.name));
		}

        private void pasteItem(object userData)
        {
            PasteContext data = userData as PasteContext;
            if (data != null)
            {
                PasteItem(data.mousePosition, data.track);
            }
        }

        public DirectorObject PasteItem(Vector2 mousePosition, TimelineTrack track, bool ignore = false, string key = "Pasted ", TimelineItem timelineItem = null)
        {
            var clone = track.CreateChild(timelineItem) as TimelineItem;
            if (!ignore)
            {
                float firetime = (mousePosition.x - state.Translation.x) / state.Scale.x;
                clone.Firetime = firetime;
            }
            else
            {
                var deepCopy = DirectorCopyPaste.deepCopy as TimelineItem;
                if (deepCopy != null)
                {
                    clone.Firetime = deepCopy.Firetime + 0.1f;
                }
            }
            Undo.RegisterCreatedObjectUndo(clone, key + clone.name);
            return clone;
        }

        private class PasteContext
        {
            public Vector2 mousePosition;
            public TimelineTrack track;

            public PasteContext(Vector2 mousePosition, TimelineTrack track)
            {
                this.mousePosition = mousePosition;
                this.track = track;
            }
        }

		public class TrackStyles
		{
			public GUIStyle TrackAreaStyle;

			public GUIStyle backgroundSelected;

			public GUIStyle backgroundContentSelected;

			public GUIStyle TrackSidebarBG1;

			public GUIStyle TrackSidebarBG2;

			public GUIStyle TrackItemStyle;

			public GUIStyle AudioTrackItemStyle;

			public GUIStyle ShotTrackItemStyle;

			public GUIStyle GlobalTrackItemStyle;

			public GUIStyle ActorTrackItemStyle;

			public GUIStyle CurveTrackItemStyle;

			public GUIStyle TrackItemSelectedStyle;

			public GUIStyle AudioTrackItemSelectedStyle;

			public GUIStyle ShotTrackItemSelectedStyle;

			public GUIStyle GlobalTrackItemSelectedStyle;

			public GUIStyle ActorTrackItemSelectedStyle;

			public GUIStyle CurveTrackItemSelectedStyle;

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
				TrackAreaStyle = skin.FindStyle("Track Area");
				TrackItemStyle = skin.FindStyle("Track Item");
				TrackItemSelectedStyle = skin.FindStyle("TrackItemSelected");
				ShotTrackItemStyle = skin.FindStyle("ShotTrackItem");
				ShotTrackItemSelectedStyle = skin.FindStyle("ShotTrackItemSelected");
				AudioTrackItemStyle = skin.FindStyle("AudioTrackItem");
				AudioTrackItemSelectedStyle = skin.FindStyle("AudioTrackItemSelected");
				GlobalTrackItemStyle = skin.FindStyle("GlobalTrackItem");
				GlobalTrackItemSelectedStyle = skin.FindStyle("GlobalTrackItemSelected");
				ActorTrackItemStyle = skin.FindStyle("ActorTrackItem");
				ActorTrackItemSelectedStyle = skin.FindStyle("ActorTrackItemSelected");
				CurveTrackItemStyle = skin.FindStyle("CurveTrackItem");
				CurveTrackItemSelectedStyle = skin.FindStyle("CurveTrackItemSelected");
				keyframeStyle = skin.FindStyle("Keyframe");
				curveStyle = skin.FindStyle("Curve");
				tangentStyle = skin.FindStyle("TangentHandle");
				curveCanvasStyle = skin.FindStyle("CurveCanvas");
				compressStyle = skin.FindStyle("CompressVertical");
				expandStyle = skin.FindStyle("ExpandVertical");
				editCurveItemStyle = skin.FindStyle("EditCurveItem");
				EventItemStyle = skin.FindStyle("EventItem");
				EventItemBottomStyle = skin.FindStyle("EventItemBottom");
				keyframeContextStyle = skin.FindStyle("KeyframeContext");
				TrackSidebarBG1 = skin.FindStyle("TrackSidebarBG");
				TrackSidebarBG2 = skin.FindStyle("TrackSidebarBGAlt");
				backgroundSelected = skin.FindStyle("TrackGroupFocused");
				backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
			}
		}

		public static TrackStyles styles;

		public const float ROW_HEIGHT = 17f;

		protected const int INDENT_AMOUNT = 14;

		protected const int TRACK_ICON_WIDTH = 16;

		protected Rect trackArea = new Rect(0f, 0f, 0f, 17f);

		protected DirectorControlState state;

		protected Dictionary<TimelineItemWrapper, TrackItemControl> itemMap = new Dictionary<TimelineItemWrapper, TrackItemControl>();

		protected TimelineTrackWrapper targetTrack;

		private TrackGroupControl trackGroupControl;

		protected bool renameRequested;

		protected bool isRenaming;

		private int renameControlID;

		public Rect Rect
		{
			get
			{
				CalculateHeight();
				return trackArea;
			}
		}

		public TimelineTrackWrapper TargetTrack
		{
			get
			{
				return targetTrack;
			}
			set
			{
				targetTrack = value;
				Behaviour = targetTrack.Behaviour;
			}
		}

		public IEnumerable<TrackItemControl> Controls
		{
			get
			{
				return itemMap.Values;
			}
		}

		public TrackGroupControl TrackGroupControl
		{
			get
			{
				return trackGroupControl;
			}
			set
			{
				trackGroupControl = value;
			}
		}

		public new bool IsSelected
		{
			get
			{
				return DirectorWindow.GetSelection().Contains(TargetTrack.Behaviour);
			}
		}

		internal static void InitStyles(GUISkin skin)
		{
			if (styles == null)
			{
				styles = new TrackStyles(skin);
			}
		}

		public virtual void Initialize()
		{
			InitCommand();
		}

		public void UnInitialize()
        {

        }

		internal void BindTimelineItemControls(TimelineTrackWrapper track, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
		{
			if (TargetTrack.HasChanged)
			{
				foreach (var current in track.Items)
				{
					if (!itemMap.ContainsKey(current))
					{
						var types = DirectorControlHelper.GetAllSubTypes(typeof(TrackItemControl));
						Type type = typeof(TrackItemControl);
						int num = 2147483647;
						int drawPriority = 0;
						foreach (var type2 in types)
						{
							var cutsceneItemControlAttribute = type2.GetCustomAttributes(typeof(CutsceneItemControlAttribute), true).FirstOrDefault() as CutsceneItemControlAttribute;
							if (cutsceneItemControlAttribute != null)
							{
								int subTypeDepth = DirectorControlHelper.GetSubTypeDepth(current.TimelineItem.GetType(), cutsceneItemControlAttribute.ItemType);
								if (subTypeDepth < num)
								{
									type = type2;
									num = subTypeDepth;
									drawPriority = cutsceneItemControlAttribute.DrawPriority;
								}
							}
						}
						var trackItemControl = (TrackItemControl)Activator.CreateInstance(type);
						trackItemControl.DrawPriority = drawPriority;
						trackItemControl.Initialize(current, TargetTrack);
						trackItemControl.TrackControl = this;
						initializeTrackItemControl(trackItemControl);
						newTimelineControls.Add(trackItemControl);
						itemMap.Add(current, trackItemControl);
					}
				}
				List<TimelineItemWrapper> list = new List<TimelineItemWrapper>();
				foreach (TimelineItemWrapper current2 in itemMap.Keys)
				{
					bool flag = false;
					foreach (TimelineItemWrapper current3 in track.Items)
					{
						if (current2.Equals(current3))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						prepareTrackItemControlForRemoval(itemMap[current2]);
						removedTimelineControls.Add(itemMap[current2]);
						list.Add(current2);
					}
				}
				foreach (TimelineItemWrapper current4 in list)
				{
					itemMap.Remove(current4);
				}
			}
			track.HasChanged = false;
		}

		protected virtual void initializeTrackItemControl(TrackItemControl control)
		{
		}

		protected virtual void prepareTrackItemControlForRemoval(TrackItemControl control)
		{
		}

		public virtual void CalculateHeight()
		{
			trackArea.height = ROW_HEIGHT;
			if (isExpanded)
			{
				trackArea.height = ROW_HEIGHT * expandedSize;
			}
		}

		protected void showHeaderContextMenu()
		{
			GenericMenu expr_05 = new GenericMenu();
			expr_05.AddItem(new GUIContent("Rename"), false, new GenericMenu.MenuFunction(this.Rename));
			expr_05.AddItem(new GUIContent("Duplicate"), false, new GenericMenu.MenuFunction(this.RequestDuplicate));
			expr_05.AddItem(new GUIContent("Delete"), false, new GenericMenu.MenuFunction(this.RequestDelete));
			expr_05.ShowAsContext();
		}

		public virtual void UpdateHeaderBackground(Rect position, int ordinal)
		{
			if (DirectorWindow.GetSelection().Contains(TargetTrack.Behaviour) && !isRenaming)
			{
				GUI.Box(position, string.Empty, styles.backgroundSelected);
				return;
			}
			if (ordinal % 2 == 0)
			{
				GUI.Box(position, string.Empty, styles.TrackSidebarBG2);
				return;
			}
			GUI.Box(position, string.Empty, styles.TrackSidebarBG1);
		}

		protected virtual void updateHeaderControl1(Rect position)
		{
		}

		protected virtual void updateHeaderControl2(Rect position)
		{
		}

		protected virtual void updateHeaderControl3(Rect position)
		{
		}

		protected virtual void updateHeaderControl4(Rect position)
		{
		}

		protected virtual void updateHeaderControl5(Rect position)
		{
		}

		public virtual void UpdateHeaderContents(DirectorControlState state, Rect position, Rect headerBackground)
		{
			var color = GUI.color;
			var track = TargetTrack.Behaviour as TimelineTrack;
			if (!track.enabled)
				GUI.color = Color.grey;
			Rect rect = new Rect(position.x + 14f, position.y, 14f, position.height);
			Rect rect2 = new Rect(rect.x + rect.width, position.y, position.width - 14f - 96f - 14f, position.height);
			var text = track.trackName;
			bool flag = EditorGUI.Foldout(rect, isExpanded, GUIContent.none, false);
			if (flag != isExpanded)
			{
				isExpanded = flag;
				EditorPrefs.SetBool(IsExpandedKey, isExpanded);
			}
			updateHeaderControl1(new Rect(position.width - 80f, position.y, 16f, 16f));
			this.updateHeaderControl2(new Rect(position.width - 64f, position.y, 16f, 16f));
			this.updateHeaderControl3(new Rect(position.width - 48f, position.y, 16f, 16f));
			this.updateHeaderControl4(new Rect(position.width - 32f, position.y, 16f, 16f));
			this.updateHeaderControl5(new Rect(position.width - 16f, position.y, 16f, 16f));
			int controlID = GUIUtility.GetControlID(TargetTrack.Behaviour.GetInstanceID(), FocusType.Passive, position);
			if (isRenaming)
			{
				GUI.SetNextControlName("TrackRename");
				text = EditorGUI.TextField(rect2, GUIContent.none, text);
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
			if (track.trackName != text)
			{
				Undo.RecordObject(TargetTrack.Behaviour, string.Format("Renamed {0}", TargetTrack.Behaviour.name));
				TargetTrack.Behaviour.name = text;
				track.trackName = text;
				TargetTrack.Behaviour.Dirty = true;
			}
			else if (track.trackName != track.name)
			{
				track.name = track.trackName;
			}
			if (!isRenaming)
			{
				string text2 = text;
				Vector2 vector = GUI.skin.label.CalcSize(new GUIContent(text2));
				rect2.width += TRACK_ICON_WIDTH;
				if (DirectorWindow.GetSelection().Contains(TargetTrack.Behaviour))
				{
					GUI.Label(rect2, text2, EditorStyles.whiteLabel);
				}
				else
				{
					GUI.Label(rect2, text2);
				}
				if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
				{
					if (position.Contains(Event.current.mousePosition) && Event.current.button == 1)
					{
						if (!IsSelected)
						{
							RequestSelect();
						}
						ShowHeaderContextMenu();
						Event.current.Use();
						return;
					}
					if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
					{
						RequestSelect();
						Event.current.Use();
					}
				}
				if (DirectorWindow.GetSelection().activeObject == targetTrack.Behaviour)
                {
					ExecuteCommand(Event.current);
				}
			}

			GUI.color = color;
		}

		protected void ShowHeaderContextMenu()
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Rename"), false, Rename);
			menu.AddItem(new GUIContent("Duplicate"), false, RequestDuplicate);
			menu.AddItem(new GUIContent("Delete"), false, RequestDelete);
			menu.ShowAsContext();
		}

		protected override void Rename()
		{
			renameRequested = true;
			isRenaming = true;
		}

		public virtual void UpdateTrackBodyBackground(Rect position)
		{
			GUI.Box(position, string.Empty, styles.TrackAreaStyle);
		}

		public virtual void UpdateTrackContents(DirectorControlState state, Rect position, Rect headerBackground)
		{
			this.state = state;
			trackArea = position;
			var list = new List<KeyValuePair<int, TimelineItemWrapper>>();
			foreach (var key in itemMap.Keys)
			{
				TrackItemControl trackItemControl = itemMap[key];
				trackItemControl.Wrapper = key;
				trackItemControl.Track = TargetTrack;
				trackItemControl.PreUpdate(state, position);
				var item = new KeyValuePair<int, TimelineItemWrapper>(trackItemControl.DrawPriority, key);
				list.Add(item);
			}
			list.Sort(Comparison);
			foreach (KeyValuePair<int, TimelineItemWrapper> current2 in list)
			{
				itemMap[current2.Value].HandleInput(state, position);
			}
			list.Reverse();
			Rect rect = new Rect(0f, 0f, position.width, position.height);
			int controlID = GUIUtility.GetControlID(TargetTrack.Behaviour.GetInstanceID(), FocusType.Passive, rect);
			EventType typeForControl = Event.current.GetTypeForControl(controlID);
			headerBackground.height += 100;
			bool mark = headerBackground.Contains(Event.current.mousePosition);
			foreach (var current3 in list)
			{
				var trackItemControl = itemMap[current3.Value];
				trackItemControl.Draw(state);
				trackItemControl.PostUpdate(state, mark, typeForControl);
			}
			var current = Event.current;
			if (rect.Contains(current.mousePosition))
            {
				if (typeForControl == EventType.MouseDown && current.button == 1 && !current.alt && !current.shift && !current.control)
				{
					showBodyContextMenu(current);
					current.Use();
				}
			}
		}

		protected static int Comparison(KeyValuePair<int, TimelineItemWrapper> x, KeyValuePair<int, TimelineItemWrapper> y)
		{
			int result = 0;
			if (x.Key < y.Key)
			{
				result = 1;
			}
			else if (x.Key > y.Key)
			{
				result = -1;
			}
			return result;
		}

		internal void BoxSelect(Rect selectionBox)
		{
			foreach (TimelineItemWrapper current in itemMap.Keys)
			{
				itemMap[current].BoxSelect(selectionBox);
			}
		}

		public override DirectorObject Duplicate(DirectorObject parent = null)
		{
			if (parent == null)
				parent = TargetTrack.Behaviour.Parent;
			var directorObject = DirectorObject.Create(TargetTrack.Behaviour, parent) as TimelineTrack;
			var track = TargetTrack.Behaviour as TimelineTrack;
			directorObject.Template = track.Template;
			var name = TargetTrack.Behaviour.name;
			var value = Regex.Match(name, "(\\d+)$").Value;
			int num;
			if (int.TryParse(value, out num))
			{
				num++;
				directorObject.name = name.Substring(0, name.Length - value.Length) + num.ToString();
			}
			else
			{
				num = 1;
				directorObject.name = name.Substring(0, name.Length - value.Length) + " " + num.ToString();
			}
			Undo.RegisterCreatedObjectUndo(directorObject, "Duplicate " + directorObject.name);
			return directorObject;
		}

		internal void DuplicateSelectedChildren(DirectorObject parent)
		{
			foreach (var value in itemMap.Values)
            {
				if (value.IsSelected)
                {
					value.Duplicate(parent);
                }
            }
		}

		internal void DeleteSelectedChildren()
		{
			foreach (TimelineItemWrapper current in itemMap.Keys)
			{
				TrackItemControl trackItemControl = itemMap[current];
				if (trackItemControl.IsSelected)
				{
					trackItemControl.Delete();
				}
			}
		}
	}
}