using DirectorEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TimelineTrackControl : SidebarControl
{
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
			this.TrackAreaStyle = skin.FindStyle("Track Area");
			this.TrackItemStyle = skin.FindStyle("Track Item");
			this.TrackItemSelectedStyle = skin.FindStyle("TrackItemSelected");
			this.ShotTrackItemStyle = skin.FindStyle("ShotTrackItem");
			this.ShotTrackItemSelectedStyle = skin.FindStyle("ShotTrackItemSelected");
			this.AudioTrackItemStyle = skin.FindStyle("AudioTrackItem");
			this.AudioTrackItemSelectedStyle = skin.FindStyle("AudioTrackItemSelected");
			this.GlobalTrackItemStyle = skin.FindStyle("GlobalTrackItem");
			this.GlobalTrackItemSelectedStyle = skin.FindStyle("GlobalTrackItemSelected");
			this.ActorTrackItemStyle = skin.FindStyle("ActorTrackItem");
			this.ActorTrackItemSelectedStyle = skin.FindStyle("ActorTrackItemSelected");
			this.CurveTrackItemStyle = skin.FindStyle("CurveTrackItem");
			this.CurveTrackItemSelectedStyle = skin.FindStyle("CurveTrackItemSelected");
			this.keyframeStyle = skin.FindStyle("Keyframe");
			curveStyle = skin.FindStyle("Curve");
			this.tangentStyle = skin.FindStyle("TangentHandle");
			this.curveCanvasStyle = skin.FindStyle("CurveCanvas");
			this.compressStyle = skin.FindStyle("CompressVertical");
			this.expandStyle = skin.FindStyle("ExpandVertical");
			this.editCurveItemStyle = skin.FindStyle("EditCurveItem");
			this.EventItemStyle = skin.FindStyle("EventItem");
			this.EventItemBottomStyle = skin.FindStyle("EventItemBottom");
			this.keyframeContextStyle = skin.FindStyle("KeyframeContext");
			this.TrackSidebarBG1 = skin.FindStyle("TrackSidebarBG");
			this.TrackSidebarBG2 = skin.FindStyle("TrackSidebarBGAlt");
			this.backgroundSelected = skin.FindStyle("TrackGroupFocused");
			this.backgroundContentSelected = skin.FindStyle("TrackGroupContentFocused");
		}
	}

	public static TrackStyles styles;

	public const float ROW_HEIGHT = 17f;

	protected const int INDENT_AMOUNT = 14;

	protected const int TRACK_ICON_WIDTH = 16;

	protected Rect trackArea = new Rect(0f, 0f, 0f, 17f);

	protected DirectorControlState state;

	private Dictionary<TimelineItemWrapper, TrackItemControl> itemMap = new Dictionary<TimelineItemWrapper, TrackItemControl>();

	private TimelineTrackWrapper targetTrack;

	private TrackGroupControl trackGroupControl;

	protected bool renameRequested;

	protected bool isRenaming;

	private int renameControlID;

	public Rect Rect
	{
		get
		{
			calculateHeight();
			return this.trackArea;
		}
	}

	public TimelineTrackWrapper TargetTrack
	{
		get
		{
			return this.targetTrack;
		}
		set
		{
			this.targetTrack = value;
			base.Behaviour = this.targetTrack.Behaviour;
		}
	}

	public DirectorControlState State
	{
		set
		{
			this.state = value;
		}
	}

	public IEnumerable<TrackItemControl> Controls
	{
		get
		{
			return this.itemMap.Values;
		}
	}

	public TrackGroupControl TrackGroupControl
	{
		get
		{
			return this.trackGroupControl;
		}
		set
		{
			this.trackGroupControl = value;
		}
	}

	public new bool IsSelected
	{
		get
		{
			return Selection.Contains(this.TargetTrack.Behaviour.gameObject);
		}
	}

	internal static void InitStyles(GUISkin skin)
	{
		if (TimelineTrackControl.styles == null)
		{
			TimelineTrackControl.styles = new TimelineTrackControl.TrackStyles(skin);
		}
	}

	public virtual void Initialize()
	{
	}

	internal void BindTimelineItemControls(TimelineTrackWrapper track, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
	{
		if (this.TargetTrack.HasChanged)
		{
			foreach (TimelineItemWrapper current in track.Items)
			{
				TrackItemControl trackItemControl = null;
				if (!this.itemMap.TryGetValue(current, out trackItemControl))
				{
					Type[] arg_62_0 = DirectorControlHelper.GetAllSubTypes(typeof(TrackItemControl));
					Type type = typeof(TrackItemControl);
					int num = 2147483647;
					int drawPriority = 0;
					Type[] array = arg_62_0;
					for (int i = 0; i < array.Length; i++)
					{
						Type type2 = array[i];
						object[] customAttributes = type2.GetCustomAttributes(typeof(CutsceneItemControlAttribute), true);
						for (int j = 0; j < customAttributes.Length; j++)
						{
							CutsceneItemControlAttribute cutsceneItemControlAttribute = (CutsceneItemControlAttribute)customAttributes[j];
							if (cutsceneItemControlAttribute != null)
							{
								int subTypeDepth = DirectorControlHelper.GetSubTypeDepth(current.Behaviour.GetType(), cutsceneItemControlAttribute.ItemType);
								if (subTypeDepth < num)
								{
									type = type2;
									num = subTypeDepth;
									drawPriority = cutsceneItemControlAttribute.DrawPriority;
								}
							}
						}
					}
					trackItemControl = (TrackItemControl)Activator.CreateInstance(type);
					trackItemControl.DrawPriority = drawPriority;
					trackItemControl.Initialize(current, this.TargetTrack);
					trackItemControl.TrackControl = this;
					this.initializeTrackItemControl(trackItemControl);
					newTimelineControls.Add(trackItemControl);
					this.itemMap.Add(current, trackItemControl);
				}
			}
			List<TimelineItemWrapper> list = new List<TimelineItemWrapper>();
			foreach (TimelineItemWrapper current2 in this.itemMap.Keys)
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
					this.prepareTrackItemControlForRemoval(this.itemMap[current2]);
					removedTimelineControls.Add(this.itemMap[current2]);
					list.Add(current2);
				}
			}
			foreach (TimelineItemWrapper current4 in list)
			{
				this.itemMap.Remove(current4);
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

	public virtual void calculateHeight()
	{
		this.trackArea.height=(17f);
		if (this.isExpanded)
		{
			this.trackArea.height=(17f * (float)this.expandedSize);
		}
	}

	public virtual void UpdateHeaderBackground(Rect position, int ordinal)
	{
		if (Selection.Contains(this.TargetTrack.Behaviour.gameObject) && !this.isRenaming)
		{
			GUI.Box(position, string.Empty, TimelineTrackControl.styles.backgroundSelected);
			return;
		}
		if (ordinal % 2 == 0)
		{
			GUI.Box(position, string.Empty, TimelineTrackControl.styles.TrackSidebarBG2);
			return;
		}
		GUI.Box(position, string.Empty, TimelineTrackControl.styles.TrackSidebarBG1);
	}

	public virtual void UpdateHeaderContents(DirectorControlState state, Rect position, Rect headerBackground)
	{
		Rect rect = new Rect(position.x + 14f, position.y, 14f, position.height);
		Rect rect2 = new Rect(rect.x + rect.width, position.y, position.width - 14f - 96f - 14f, position.height);
		string text = this.TargetTrack.Behaviour.name;
		bool flag = EditorGUI.Foldout(rect, this.isExpanded, GUIContent.none, false);
		if (flag != this.isExpanded)
		{
			this.isExpanded = flag;
			EditorPrefs.SetBool(base.IsExpandedKey, this.isExpanded);
		}
		this.updateHeaderControl1(new Rect(position.width - 80f, position.y, 16f, 16f));
		this.updateHeaderControl2(new Rect(position.width - 64f, position.y, 16f, 16f));
		this.updateHeaderControl3(new Rect(position.width - 48f, position.y, 16f, 16f));
		this.updateHeaderControl4(new Rect(position.width - 32f, position.y, 16f, 16f));
		this.updateHeaderControl5(new Rect(position.width - 16f, position.y, 16f, 16f));
		int controlID = GUIUtility.GetControlID(this.TargetTrack.Behaviour.GetInstanceID(), (FocusType)2, position);
		if (this.isRenaming)
		{
			GUI.SetNextControlName("TrackRename");
			text = EditorGUI.TextField(rect2, GUIContent.none, text);
			if (this.renameRequested)
			{
				EditorGUI.FocusTextInControl("TrackRename");
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
		if (this.TargetTrack.Behaviour.name != text)
		{
			Undo.RecordObject(this.TargetTrack.Behaviour.gameObject, string.Format("Renamed {0}", this.TargetTrack.Behaviour.name));
			this.TargetTrack.Behaviour.name=(text);
		}
		if (!this.isRenaming)
		{
			string text2 = text;
			Vector2 vector = GUI.skin.label.CalcSize(new GUIContent(text2));
			while (vector.x > rect2.width && text2.Length > 5)
			{
				text2 = text2.Substring(0, text2.Length - 4) + "...";
				vector = GUI.skin.label.CalcSize(new GUIContent(text2));
			}
			if (Selection.Contains(this.TargetTrack.Behaviour.gameObject))
			{
				GUI.Label(rect2, text2, EditorStyles.whiteLabel);
			}
			else
			{
				GUI.Label(rect2, text2);
			}
			if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
			{
				if (position.Contains(Event.current.mousePosition) && (int)Event.current.button == 1)
				{
					if (!this.IsSelected)
					{
						base.RequestSelect();
					}
					this.showHeaderContextMenu();
					Event.current.Use();
					return;
				}
				if (position.Contains(Event.current.mousePosition) && (int)Event.current.button == 0)
				{
					base.RequestSelect();
					Event.current.Use();
				}
			}
		}
	}

	protected void showHeaderContextMenu()
	{
		GenericMenu expr_05 = new GenericMenu();
		expr_05.AddItem(new GUIContent("Rename"), false, new GenericMenu.MenuFunction(this.rename));
		expr_05.AddItem(new GUIContent("Duplicate"), false, new GenericMenu.MenuFunction(this.duplicate));
		expr_05.AddItem(new GUIContent("Delete"), false, new GenericMenu.MenuFunction(this.delete));
		expr_05.ShowAsContext();
	}

	private void delete()
	{
		base.RequestDelete();
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

	protected virtual void showBodyContextMenu(Event current)
	{
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

	public virtual void UpdateTrackBodyBackground(Rect position)
	{
		GUI.Box(position, string.Empty, styles.TrackAreaStyle);
	}

	public virtual void UpdateTrackContents(DirectorControlState state, Rect position, Rect headerBackground)
	{
		this.state = state;
		trackArea = position;
		List<KeyValuePair<int, TimelineItemWrapper>> list = new List<KeyValuePair<int, TimelineItemWrapper>>();
		foreach (TimelineItemWrapper current in itemMap.Keys)
		{
			TrackItemControl trackItemControl = itemMap[current];
			trackItemControl.Wrapper = current;
			trackItemControl.Track = TargetTrack;
			trackItemControl.PreUpdate(state, position);
			KeyValuePair<int, TimelineItemWrapper> item = new KeyValuePair<int, TimelineItemWrapper>(trackItemControl.DrawPriority, current);
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
        foreach (KeyValuePair<int, TimelineItemWrapper> current3 in list)
		{
			TrackItemControl expr_111 = itemMap[current3.Value];
			expr_111.Draw(state);
			expr_111.PostUpdate(state, mark, typeForControl);
		}
		if (typeForControl == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 1 && !Event.current.alt && !Event.current.shift && !Event.current.control)
		{
			showBodyContextMenu(Event.current);
			Event.current.Use();
		}
	}

    public void OnDestroy()
    {
        
    }

	private void control_DeleteRequest(object sender, DirectorBehaviourControlEventArgs e)
	{
		delete();
	}

	private static int Comparison(KeyValuePair<int, TimelineItemWrapper> x, KeyValuePair<int, TimelineItemWrapper> y)
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

	internal new void Delete()
	{
		Undo.DestroyObjectImmediate(this.TargetTrack.Behaviour.gameObject);
	}

	internal void BoxSelect(Rect selectionBox)
	{
		foreach (TimelineItemWrapper current in this.itemMap.Keys)
		{
			itemMap[current].BoxSelect(selectionBox);
		}
	}

	internal void Duplicate()
	{
		GameObject gameObject =UnityEngine.Object.Instantiate(this.TargetTrack.Behaviour.gameObject) as GameObject;
		string name = this.TargetTrack.Behaviour.gameObject.name;
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
		gameObject.transform.parent=(this.TargetTrack.Behaviour.transform.parent);
		Undo.RegisterCreatedObjectUndo(gameObject, "Duplicate " + gameObject.name);
	}

	internal void DeleteSelectedChildren()
	{
		foreach (TimelineItemWrapper current in this.itemMap.Keys)
		{
			TrackItemControl trackItemControl = this.itemMap[current];
			if (trackItemControl.IsSelected)
			{
				trackItemControl.Delete();
			}
		}
	}
}
