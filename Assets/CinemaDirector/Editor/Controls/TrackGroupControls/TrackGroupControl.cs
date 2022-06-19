using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
	public class TrackGroupControl : SidebarControl
	{
		public class TrackGroupStyles
		{
			public GUIStyle addIcon;

			public GUIStyle InspectorIcon;

			public GUIStyle trackGroupArea;

			public GUIStyle pickerStyle;

			public GUIStyle backgroundSelected;

			public GUIStyle backgroundContentSelected;

			public GUIStyle DirectorGroupIcon;

			public GUIStyle ActorGroupIcon;

			public GUIStyle MultiActorGroupIcon;

			public GUIStyle CharacterGroupIcon;

			public TrackGroupStyles(GUISkin skin)
			{
				addIcon = skin.FindStyle("Add");
				InspectorIcon = skin.FindStyle("InspectorIcon");
				trackGroupArea = skin.FindStyle("Track Group Area");
				DirectorGroupIcon = skin.FindStyle("DirectorGroupIcon");
				ActorGroupIcon = skin.FindStyle("ActorGroupIcon");
				MultiActorGroupIcon = skin.FindStyle("MultiActorGroupIcon");
				CharacterGroupIcon = skin.FindStyle("CharacterGroupIcon");
				pickerStyle = skin.FindStyle("Picker");
				backgroundSelected = skin.FindStyle("TrackGroupFocused");
				backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
				if (addIcon == null || InspectorIcon == null || trackGroupArea == null || DirectorGroupIcon == null || ActorGroupIcon == null || MultiActorGroupIcon == null || CharacterGroupIcon == null || this.pickerStyle == null || this.backgroundSelected == null || this.backgroundContentSelected == null)
				{
					Debug.Log("Cinema Director GUI Skin not loaded properly. Please delete the guiskin file in the Resources folder and re-import.");
				}
			}
		}

		public static TrackGroupStyles styles;

		protected TrackGroupWrapper trackGroup;

		private DirectorControl directorControl;

		protected DirectorControlState state;

		protected const int DEFAULT_ROW_HEIGHT = 17;

		protected const int TRACK_GROUP_ICON_WIDTH = 16;

		protected Texture LabelPrefix;
		[SerializeField]
		private Dictionary<TimelineTrackWrapper, TimelineTrackControl> timelineTrackMap = new Dictionary<TimelineTrackWrapper, TimelineTrackControl>();

		private float sortingOptionsWidth = 32f;

		private Rect position;

		private bool renameRequested;
		[SerializeField]
		private bool isRenaming;

		private int renameControlID;

		public TrackGroupWrapper TrackGroup
		{
			get
			{
				return trackGroup;
			}
			set
			{
				trackGroup = value;
				Behaviour = trackGroup.Behaviour;
			}
		}

		public DirectorControl DirectorControl
		{
			get
			{
				return directorControl;
			}
			set
			{
				directorControl = value;
			}
		}

		internal static void InitStyles(GUISkin skin)
		{
			if (styles == null)
			{
				styles = new TrackGroupStyles(skin);
			}
		}

		public virtual void Initialize()
		{
			LabelPrefix = styles.DirectorGroupIcon.normal.background;
			InitCommand();
		}

		internal void BindTrackControls(TrackGroupWrapper trackGroup, List<SidebarControl> newSidebarControls, List<SidebarControl> removedSidebarControls, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
		{
			if (trackGroup.HasChanged)
			{
				bool flag = false;
				foreach (var current in trackGroup.Tracks)
				{
					TimelineTrackControl timelineTrackControl;
					if (!timelineTrackMap.TryGetValue(current, out timelineTrackControl))
					{
						flag = true;
						var type = typeof(TimelineTrackControl);
						var allSubTypes = DirectorControlHelper.GetAllSubTypes(type);
						int num = 0x7fffffff;
						foreach (var type2 in allSubTypes)
                        {
                            System.Type c = null;
                            foreach (CutsceneTrackAttribute attribute in type2.GetCustomAttributes(typeof(CutsceneTrackAttribute), true))
                            {
                                if (attribute != null)
                                {
                                    c = attribute.TrackType;
                                }
                            }
							if (c == current.Behaviour.GetType())
							{
								type = type2;
								break;
							}
							if (c != null && current.Behaviour.GetType().IsSubclassOf(c))
							{
								System.Type baseType = current.Behaviour.GetType();
								int num2 = 0;
								while (baseType != null && baseType != c)
								{
									baseType = baseType.BaseType;
									num2++;
								}
								if (num2 <= num)
								{
									num = num2;
									type = type2;
								}
							}
						}
						timelineTrackControl = (TimelineTrackControl)Activator.CreateInstance(type);
						timelineTrackControl.Initialize();
						timelineTrackControl.TrackGroupControl = this;
						timelineTrackControl.TargetTrack = current;
						timelineTrackControl.SetExpandedFromEditorPrefs();
						newSidebarControls.Add(timelineTrackControl);
						timelineTrackMap.Add(current, timelineTrackControl);
					}
				}
				List<TimelineTrackWrapper> list = new List<TimelineTrackWrapper>();
				foreach (var current2 in timelineTrackMap.Keys)
				{
					bool flag2 = false;
					foreach (TimelineTrackWrapper current3 in trackGroup.Tracks)
					{
						if (current2.Equals(current3))
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						removedSidebarControls.Add(timelineTrackMap[current2]);
						list.Add(current2);
					}
				}
				foreach (TimelineTrackWrapper current4 in list)
				{
					flag = true;
					timelineTrackMap.Remove(current4);
				}
				if (flag)
				{
					SortedDictionary<int, TimelineTrackWrapper> sortedDictionary = new SortedDictionary<int, TimelineTrackWrapper>();
					List<TimelineTrackWrapper> list2 = new List<TimelineTrackWrapper>();
					foreach (TimelineTrackWrapper current5 in timelineTrackMap.Keys)
					{
						if (current5.Ordinal >= 0 && !sortedDictionary.ContainsKey(current5.Ordinal))
						{
							sortedDictionary.Add(current5.Ordinal, current5);
						}
						else
						{
							list2.Add(current5);
						}
					}
					int num3 = 0;
					using (SortedDictionary<int, TimelineTrackWrapper>.ValueCollection.Enumerator enumerator4 = sortedDictionary.Values.GetEnumerator())
					{
						while (enumerator4.MoveNext())
						{
							enumerator4.Current.Ordinal = num3;
							num3++;
						}
					}
					using (List<TimelineTrackWrapper>.Enumerator enumerator3 = list2.GetEnumerator())
					{
						while (enumerator3.MoveNext())
						{
							enumerator3.Current.Ordinal = num3;
							num3++;
						}
					}
				}
			}
			foreach (TimelineTrackWrapper current6 in timelineTrackMap.Keys)
			{
				timelineTrackMap[current6].BindTimelineItemControls(current6, newTimelineControls, removedTimelineControls);
			}
			trackGroup.HasChanged = false;
		}

		public virtual void Update(TrackGroupWrapper trackGroup, DirectorControlState state, Rect position, Rect fullHeader, Rect safeHeader, Rect content, Rect controlArea)
		{
			this.position = position;
			this.trackGroup = trackGroup;
			this.state = state;
			if (trackGroup.Behaviour == null)
			{
				return;
			}
			updateHeaderBackground(fullHeader);
			updateContentBackground(content);
			if (isExpanded)
			{
				var header = new Rect(safeHeader.x, safeHeader.y, fullHeader.width, safeHeader.height);
				UpdateTracks(state, header, content, controlArea);
			}
			UpdateHeaderContent(safeHeader);
		}

		protected virtual void UpdateHeaderContent(Rect header)
		{
			var arg_9E_0 = new Rect(header.x, header.y, 14f, 17f);
			Rect rect = new Rect(header.x + 14f, header.y, 16f, 17f);
			Rect rect2 = new Rect(rect.x + rect.width, header.y, header.width - (rect.x + rect.width) - 32f, 17f);
			string text = trackGroup.Behaviour.name;
			bool flag = EditorGUI.Foldout(arg_9E_0, isExpanded, GUIContent.none, false);
			if (flag != isExpanded)
			{
				isExpanded = flag;
				EditorPrefs.SetBool(IsExpandedKey, isExpanded);
			}
			GUI.Box(rect, LabelPrefix, GUIStyle.none);
			updateHeaderControl6(new Rect(header.width - 16f, header.y, 16f, 16f));
			if (isRenaming)
			{
				GUI.SetNextControlName("TrackGroupRename");
				text = EditorGUI.TextField(rect2, GUIContent.none, text);
				if (renameRequested)
				{
					EditorGUI.FocusTextInControl("TrackGroupRename");
					renameRequested = false;
					renameControlID = GUIUtility.keyboardControl;
				}
				if (!EditorGUIUtility.editingTextField || renameControlID != GUIUtility.keyboardControl || Event.current.keyCode == KeyCode.Return || (Event.current.type == EventType.MouseDown && !rect2.Contains(Event.current.mousePosition)))
				{
					isRenaming = false;
					GUIUtility.hotControl = (0);
					GUIUtility.keyboardControl = (0);
					EditorGUIUtility.editingTextField = (false);
				}
			}
			if (trackGroup.Behaviour.name != text)
			{
				Undo.RecordObject(trackGroup.Behaviour, string.Format("Renamed {0}", trackGroup.Behaviour.name));
				trackGroup.Behaviour.name = (text);
			}
			if (!isRenaming)
			{
				string text2 = text;
				var vector = GUI.skin.label.CalcSize(new GUIContent(text2));
				var controlID = GUIUtility.GetControlID(trackGroup.Behaviour.GetInstanceID(), FocusType.Passive, header);
				var current = Event.current;
				if (header.Contains(current.mousePosition))
                {
					if (current.GetTypeForControl(controlID) == EventType.MouseDown)
					{
						if (current.button == 1)
						{
							if (!IsSelected)
							{
								RequestSelect();
							}
							ShowHeaderContextMenu();
							current.Use();
						}
						else if (current.button == 0)
						{
							RequestSelect();
							current.Use();
						}
					}
					ExecuteCommand(current);
				}
				if (IsSelected)
				{
					GUI.Label(rect2, text2, EditorStyles.whiteLabel);
					return;
				}
				GUI.Label(rect2, text2);
			}
		}

		protected virtual void UpdateTracks(DirectorControlState state, Rect header, Rect content, Rect controlArea)
		{
			var sortedDictionary = new SortedDictionary<int, TimelineTrackWrapper>();
			foreach (var current in timelineTrackMap.Keys)
			{
				timelineTrackMap[current].TargetTrack = current;
				sortedDictionary.Add(current.Ordinal, current);
			}
			float num = header.y + 17f;
			foreach (int key in sortedDictionary.Keys)
			{
				var timelineTrackWrapper = sortedDictionary[key];
				if (timelineTrackWrapper.Behaviour == null)
					continue;
				var timelineTrackControl = timelineTrackMap[timelineTrackWrapper];
				timelineTrackControl.Ordinal = new[]
				{
					trackGroup.Ordinal,
					key
				};
				float height = timelineTrackControl.Rect.height;
				Rect rect = new Rect(content.x, num, content.width, height);
				Rect headerBackground = new Rect(header.x, num, header.width, height);
				Rect rect2 = new Rect(header.x, num, header.width - sortingOptionsWidth - 4f, height);
				Rect rect3 = new Rect(rect2.x + rect2.width, num, sortingOptionsWidth / 2f, 16f);
				Rect arg_225_0 = new Rect(rect3.x + sortingOptionsWidth / 2f, num, sortingOptionsWidth / 2f, 16f);
				timelineTrackControl.UpdateTrackBodyBackground(rect);
				timelineTrackControl.UpdateHeaderBackground(headerBackground, key);
				GUILayout.BeginArea(rect);
				timelineTrackControl.UpdateTrackContents(state, rect, controlArea);
				GUILayout.EndArea();
				timelineTrackControl.UpdateHeaderContents(state, rect2, headerBackground);
				GUI.enabled = key > 0;
				if (GUI.Button(rect3, string.Empty, DirectorControl.DirectorControlStyles.UpArrowIcon))
				{
					var expr_1CE = timelineTrackWrapper;
					int ordinal = expr_1CE.Ordinal;
					expr_1CE.Ordinal = ordinal - 1;
					var expr_1F9 = timelineTrackMap[sortedDictionary[key - 1]].TargetTrack;
					ordinal = expr_1F9.Ordinal;
					expr_1F9.Ordinal = ordinal + 1;
				}
				GUI.enabled = (key < sortedDictionary.Count - 1);
				if (GUI.Button(arg_225_0, string.Empty, DirectorControl.DirectorControlStyles.DownArrowIcon))
				{
					var expr_22E = timelineTrackWrapper;
					int ordinal = expr_22E.Ordinal;
					expr_22E.Ordinal = ordinal + 1;
					var expr_259 = timelineTrackMap[sortedDictionary[key + 1]].TargetTrack;
					ordinal = expr_259.Ordinal;
					expr_259.Ordinal = ordinal - 1;
				}
				GUI.enabled = true;
				num += height;
			}
		}

		public void OnDestroy(TrackGroupWrapper trackGroup, DirectorControlState state)
		{
			SortedDictionary<int, TimelineTrackWrapper> sortedDictionary = new SortedDictionary<int, TimelineTrackWrapper>();
			foreach (TimelineTrackWrapper current in timelineTrackMap.Keys)
			{
				timelineTrackMap[current].TargetTrack = current;
				sortedDictionary.Add(current.Ordinal, current);
			}
			foreach (int current2 in sortedDictionary.Keys)
			{
				TimelineTrackWrapper timelineTrackWrapper = sortedDictionary[current2];
				TimelineTrackControl timelineTrackControl = timelineTrackMap[timelineTrackWrapper];
				timelineTrackControl.Ordinal = new[]
				{
					trackGroup.Ordinal,
					current2
				};
			}
		}

		protected virtual void ShowHeaderContextMenu()
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Rename"), false, Rename);
			menu.AddItem(new GUIContent("Duplicate"), false, duplicate);
			menu.AddItem(new GUIContent("Delete"), false, delete);
			menu.ShowAsContext();
		}

        protected override void Rename()
        {
			renameRequested = true;
			isRenaming = true;
        }

        private void delete()
		{
			RequestDelete();
		}

		private void duplicate()
		{
			RequestDuplicate();
		}

		protected virtual void updateHeaderControl6(Rect position)
		{
			Color color = GUI.color;
			int num = 0;
			foreach (var _ in trackGroup.Tracks)
			{
				num++;
			}
			GUI.color = ((num > 0) ? new Color(0f, 53f, 0f) : new Color(53f, 0f, 0f));
			if (GUI.Button(position, string.Empty, styles.addIcon))
			{
				addTrackContext();
			}
			GUI.color = (color);
		}

		protected virtual void addTrackContext()
		{
		}
		
		
		protected virtual void updateContentBackground(Rect content)
		{
			if (DirectorWindow.GetSelection().Contains(trackGroup.Behaviour) && !isRenaming)
			{
				GUI.Box(position, string.Empty, styles.backgroundContentSelected);
				return;
			}
			GUI.Box(position, string.Empty, styles.trackGroupArea);
		}

		protected virtual void updateHeaderBackground(Rect position)
		{
			if (DirectorWindow.GetSelection().Contains(trackGroup.Behaviour) && !isRenaming)
			{
				GUI.Box(position, string.Empty, styles.backgroundSelected);
			}
		}

		internal float GetHeight()
		{
			float num = DEFAULT_ROW_HEIGHT;
			if (isExpanded)
			{
				foreach (var current in timelineTrackMap.Values)
				{
					num += current.Rect.height;
				}
			}
			return num + 1f;
		}

		public override DirectorObject Duplicate(DirectorObject parent = null)
		{
			var name = trackGroup.Behaviour.name;
			var directorObject = DirectorObject.Create(trackGroup.Behaviour, trackGroup.Behaviour.Parent, name);

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

		internal void BoxSelect(Rect selectionBox)
		{
			if (!isExpanded)
			{
				return;
			}
			foreach (TimelineTrackWrapper current in timelineTrackMap.Keys)
			{
				timelineTrackMap[current].BoxSelect(selectionBox);
			}
		}

		internal void DeleteSelectedChildren()
		{
			foreach (TimelineTrackWrapper current in timelineTrackMap.Keys)
			{
				TimelineTrackControl timelineTrackControl = timelineTrackMap[current];
				timelineTrackControl.DeleteSelectedChildren();
				if (timelineTrackControl.IsSelected)
				{
					timelineTrackControl.Delete();
				}
			}
		}

		internal void DuplicateSelectedChildren()
		{
			foreach (var current in timelineTrackMap.Keys)
			{
				var timelineTrackControl = timelineTrackMap[current];
				if (timelineTrackControl.IsSelected)
				{
					timelineTrackControl.Duplicate();
				}
				timelineTrackControl.DuplicateSelectedChildren(null);
			}
		}

		internal List<SidebarControl> GetSidebarControlChildren(bool onlyVisible)
		{
			List<SidebarControl> list = new List<SidebarControl>();
			if (isExpanded)
			{
				foreach (TimelineTrackWrapper current in timelineTrackMap.Keys)
				{
					TimelineTrackControl item = timelineTrackMap[current];
					list.Add(item);
				}
			}
			return list;
		}
	}
}