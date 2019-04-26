using DirectorEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

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
			this.DirectorGroupIcon = skin.FindStyle("DirectorGroupIcon");
			this.ActorGroupIcon = skin.FindStyle("ActorGroupIcon");
			this.MultiActorGroupIcon = skin.FindStyle("MultiActorGroupIcon");
			this.CharacterGroupIcon = skin.FindStyle("CharacterGroupIcon");
			pickerStyle = skin.FindStyle("Picker");
			this.backgroundSelected = skin.FindStyle("TrackGroupFocused");
			this.backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
			if (this.addIcon == null || this.InspectorIcon == null || this.trackGroupArea == null || this.DirectorGroupIcon == null || this.ActorGroupIcon == null || this.MultiActorGroupIcon == null || this.CharacterGroupIcon == null || this.pickerStyle == null || this.backgroundSelected == null || this.backgroundContentSelected == null)
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

	private Dictionary<TimelineTrackWrapper, TimelineTrackControl> timelineTrackMap = new Dictionary<TimelineTrackWrapper, TimelineTrackControl>();

	private float sortingOptionsWidth = 32f;

	private Rect position;

	private bool renameRequested;

	private bool isRenaming;

	private bool showContext;

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
	}

	internal void BindTrackControls(TrackGroupWrapper trackGroup, List<SidebarControl> newSidebarControls, List<SidebarControl> removedSidebarControls, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
	{
		if (trackGroup.HasChanged)
		{
			bool flag = false;
			foreach (TimelineTrackWrapper current in trackGroup.Tracks)
			{
				TimelineTrackControl timelineTrackControl = null;
				if (!timelineTrackMap.TryGetValue(current, out timelineTrackControl))
				{
					flag = true;
					Type[] arg_5F_0 = DirectorControlHelper.GetAllSubTypes(typeof(TimelineTrackControl));
					Type type = typeof(TimelineTrackControl);
					int num = 2147483647;
					Type[] array = arg_5F_0;
					for (int i = 0; i < array.Length; i++)
					{
						Type type2 = array[i];
						Type type3 = null;
						object[] customAttributes = type2.GetCustomAttributes(typeof(CutsceneTrackAttribute), true);
						for (int j = 0; j < customAttributes.Length; j++)
						{
							CutsceneTrackAttribute cutsceneTrackAttribute = (CutsceneTrackAttribute)customAttributes[j];
							if (cutsceneTrackAttribute != null)
							{
								type3 = cutsceneTrackAttribute.TrackType;
							}
						}
						if (type3 == current.Behaviour.GetType())
						{
							type = type2;
							break;
						}
						if (type3 != null && current.Behaviour.GetType().IsSubclassOf(type3))
						{
							Type type4 = current.Behaviour.GetType();
							int num2 = 0;
							while (type4 != null && type4 != type3)
							{
								type4 = type4.BaseType;
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
			foreach (TimelineTrackWrapper current2 in this.timelineTrackMap.Keys)
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
				this.timelineTrackMap.Remove(current4);
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
			Rect header = new Rect(safeHeader.x, safeHeader.y, fullHeader.width, safeHeader.height);
			UpdateTracks(state, header, content, controlArea);
		}
		updateHeaderContent(safeHeader);
	}

	protected virtual void updateHeaderContent(Rect header)
	{
		Rect arg_9E_0 = new Rect(header.x, header.y, 14f, 17f);
		Rect rect = new Rect(header.x + 14f, header.y, 16f, 17f);
		Rect rect2 = new Rect(rect.x + rect.width, header.y, header.width - (rect.x + rect.width) - 32f, 17f);
		string text = this.trackGroup.Behaviour.name;
		bool flag = EditorGUI.Foldout(arg_9E_0, this.isExpanded, GUIContent.none, false);
		if (flag != this.isExpanded)
		{
			this.isExpanded = flag;
			EditorPrefs.SetBool(base.IsExpandedKey, this.isExpanded);
		}
		GUI.Box(rect, this.LabelPrefix, GUIStyle.none);
		this.updateHeaderControl1(new Rect(header.width - 96f, header.y, 16f, 16f));
		this.updateHeaderControl2(new Rect(header.width - 80f, header.y, 16f, 16f));
		this.updateHeaderControl3(new Rect(header.width - 64f, header.y, 16f, 16f));
		this.updateHeaderControl4(new Rect(header.width - 48f, header.y, 16f, 16f));
		this.updateHeaderControl5(new Rect(header.width - 32f, header.y, 16f, 16f));
		this.updateHeaderControl6(new Rect(header.width - 16f, header.y, 16f, 16f));
		if (this.isRenaming)
		{
			GUI.SetNextControlName("TrackGroupRename");
			text = EditorGUI.TextField(rect2, GUIContent.none, text);
			if (this.renameRequested)
			{
				EditorGUI.FocusTextInControl("TrackGroupRename");
				this.renameRequested = false;
				this.renameControlID = GUIUtility.keyboardControl;
			}
			if (!EditorGUIUtility.editingTextField || this.renameControlID != GUIUtility.keyboardControl || (int)Event.current.keyCode == 13 || (Event.current.type == EventType.MouseDown && !rect2.Contains(Event.current.mousePosition)))
			{
				this.isRenaming = false;
				GUIUtility.hotControl=(0);
				GUIUtility.keyboardControl=(0);
				EditorGUIUtility.editingTextField=(false);
			}
		}
		if (this.trackGroup.Behaviour.name != text)
		{
			Undo.RecordObject(this.trackGroup.Behaviour.gameObject, string.Format("Renamed {0}", this.trackGroup.Behaviour.name));
			trackGroup.Behaviour.name=(text);
		}
		if (!isRenaming)
		{
			string text2 = text;
			Vector2 vector = GUI.skin.label.CalcSize(new GUIContent(text2));
			while (vector.x > rect2.width && text2.Length > 5)
			{
				text2 = text2.Substring(0, text2.Length - 4) + "...";
				vector = GUI.skin.label.CalcSize(new GUIContent(text2));
			}
			int controlID = GUIUtility.GetControlID(trackGroup.Behaviour.GetInstanceID(), (FocusType)2, header);
			if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
			{
				if (header.Contains(Event.current.mousePosition) && (int)Event.current.button == 1)
				{
					if (!base.IsSelected)
					{
						base.RequestSelect();
					}
					showHeaderContextMenu();
					Event.current.Use();
				}
				else if (header.Contains(Event.current.mousePosition) && Event.current.button == 0)
				{
					base.RequestSelect();
					Event.current.Use();
				}
			}
			if (base.IsSelected)
			{
				GUI.Label(rect2, text2, EditorStyles.whiteLabel);
				return;
			}
			GUI.Label(rect2, text2);
		}
	}

	protected virtual void UpdateTracks(DirectorControlState state, Rect header, Rect content, Rect controlArea)
	{
		SortedDictionary<int, TimelineTrackWrapper> sortedDictionary = new SortedDictionary<int, TimelineTrackWrapper>();
		foreach (TimelineTrackWrapper current in timelineTrackMap.Keys)
		{
			timelineTrackMap[current].TargetTrack = current;
			sortedDictionary.Add(current.Ordinal, current);
		}
		float num = header.y + 17f;
		foreach (int current2 in sortedDictionary.Keys)
		{
			TimelineTrackWrapper timelineTrackWrapper = sortedDictionary[current2];
			TimelineTrackControl timelineTrackControl = timelineTrackMap[timelineTrackWrapper];
			timelineTrackControl.Ordinal = new []
			{
				trackGroup.Ordinal,
				current2
			};
			float height = timelineTrackControl.Rect.height;
			Rect rect = new Rect(content.x, num, content.width, height);
			Rect headerBackground = new Rect(header.x, num, header.width, height);
			Rect rect2 = new Rect(header.x, num, header.width - sortingOptionsWidth - 4f, height);
			Rect rect3 = new Rect(rect2.x + rect2.width, num, sortingOptionsWidth / 2f, 16f);
			Rect arg_225_0 = new Rect(rect3.x + sortingOptionsWidth / 2f, num, sortingOptionsWidth / 2f, 16f);
			timelineTrackControl.UpdateTrackBodyBackground(rect);
			timelineTrackControl.UpdateHeaderBackground(headerBackground, current2);
			GUILayout.BeginArea(rect);
			timelineTrackControl.UpdateTrackContents(state, rect, controlArea);
			GUILayout.EndArea();
			timelineTrackControl.UpdateHeaderContents(state, rect2, headerBackground);
			GUI.enabled=(current2 > 0);
			if (GUI.Button(rect3, string.Empty, DirectorControl.DirectorControlStyles.UpArrowIcon))
			{
				TimelineTrackWrapper expr_1CE = timelineTrackWrapper;
				int ordinal = expr_1CE.Ordinal;
				expr_1CE.Ordinal = ordinal - 1;
				TimelineTrackWrapper expr_1F9 = timelineTrackMap[sortedDictionary[current2 - 1]].TargetTrack;
				ordinal = expr_1F9.Ordinal;
				expr_1F9.Ordinal = ordinal + 1;
			}
			GUI.enabled=(current2 < sortedDictionary.Count - 1);
			if (GUI.Button(arg_225_0, string.Empty, DirectorControl.DirectorControlStyles.DownArrowIcon))
			{
				TimelineTrackWrapper expr_22E = timelineTrackWrapper;
				int ordinal = expr_22E.Ordinal;
				expr_22E.Ordinal = ordinal + 1;
				TimelineTrackWrapper expr_259 = timelineTrackMap[sortedDictionary[current2 + 1]].TargetTrack;
				ordinal = expr_259.Ordinal;
				expr_259.Ordinal = ordinal - 1;
			}
			GUI.enabled=(true);
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

    protected virtual void showHeaderContextMenu()
	{
		GenericMenu expr_05 = new GenericMenu();
		expr_05.AddItem(new GUIContent("Rename"), false, (rename));
		expr_05.AddItem(new GUIContent("Duplicate"), false, (duplicate));
		expr_05.AddItem(new GUIContent("Delete"), false, delete);
		expr_05.ShowAsContext();
	}

	private void delete()
	{
		RequestDelete();
	}

	private void duplicate()
	{
		base.RequestDuplicate();
	}

	private void rename()
	{
		this.renameRequested = true;
		this.isRenaming = true;
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

	protected virtual void updateHeaderControl6(Rect position)
	{
		Color color = GUI.color;
		int num = 0;
		foreach (TimelineTrackWrapper arg_21_0 in this.trackGroup.Tracks)
		{
			num++;
		}
		GUI.color=((num > 0) ? new Color(0f, 53f, 0f) : new Color(53f, 0f, 0f));
		if (GUI.Button(position, string.Empty, TrackGroupControl.styles.addIcon))
		{
			this.addTrackContext();
		}
		GUI.color=(color);
	}

	protected virtual void addTrackContext()
	{
	}

	protected virtual void updateContentBackground(Rect content)
	{
		if (Selection.Contains(this.trackGroup.Behaviour.gameObject) && !this.isRenaming)
		{
			GUI.Box(this.position, string.Empty, styles.backgroundContentSelected);
			return;
		}
		GUI.Box(this.position, string.Empty, styles.trackGroupArea);
	}

	protected virtual void updateHeaderBackground(Rect position)
	{
		if (Selection.Contains(this.trackGroup.Behaviour.gameObject) && !this.isRenaming)
		{
			GUI.Box(position, string.Empty, styles.backgroundSelected);
		}
	}

	internal float GetHeight()
	{
		float num = 17f;
		if (this.isExpanded)
		{
			foreach (TimelineTrackControl current in timelineTrackMap.Values)
			{
				num += current.Rect.height;
			}
		}
		return num + 1f;
	}

	internal void Duplicate()
	{
		GameObject gameObject =UnityEngine.Object.Instantiate(this.trackGroup.Behaviour.gameObject) as GameObject;
		string name = this.trackGroup.Behaviour.gameObject.name;
		string value = Regex.Match(name, "(\\d+)$").Value;
		int num = 1;
		if (int.TryParse(value, out num))
		{
			num++;
			gameObject.name=(name.Substring(0, name.Length - value.Length) + num.ToString());
		}
		else
		{
			num = 1;
			gameObject.name=(name.Substring(0, name.Length - value.Length) + " " + num.ToString());
		}
		gameObject.transform.parent=(this.trackGroup.Behaviour.transform.parent);
		Undo.RegisterCreatedObjectUndo(gameObject, "Duplicate " + gameObject.name);
	}

	internal void BoxSelect(Rect selectionBox)
	{
		if (!this.isExpanded)
		{
			return;
		}
		foreach (TimelineTrackWrapper current in this.timelineTrackMap.Keys)
		{
			timelineTrackMap[current].BoxSelect(selectionBox);
		}
	}

	internal new void Delete()
	{
		Undo.DestroyObjectImmediate(this.trackGroup.Behaviour.gameObject);
	}

	internal void DeleteSelectedChildren()
	{
		foreach (TimelineTrackWrapper current in this.timelineTrackMap.Keys)
		{
			TimelineTrackControl timelineTrackControl = this.timelineTrackMap[current];
			timelineTrackControl.DeleteSelectedChildren();
			if (timelineTrackControl.IsSelected)
			{
				timelineTrackControl.Delete();
			}
		}
	}

	internal void DuplicateSelectedChildren()
	{
		foreach (TimelineTrackWrapper current in this.timelineTrackMap.Keys)
		{
			TimelineTrackControl timelineTrackControl = this.timelineTrackMap[current];
			if (timelineTrackControl.IsSelected)
			{
				timelineTrackControl.Duplicate();
			}
		}
	}

	internal List<SidebarControl> GetSidebarControlChildren(bool onlyVisible)
	{
		List<SidebarControl> list = new List<SidebarControl>();
		if (this.isExpanded)
		{
			foreach (TimelineTrackWrapper current in this.timelineTrackMap.Keys)
			{
				TimelineTrackControl item = this.timelineTrackMap[current];
				list.Add(item);
			}
		}
		return list;
	}
}
