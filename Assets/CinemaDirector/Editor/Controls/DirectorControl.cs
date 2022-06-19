using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    public class DirectorControl : TimeArea
	{
		public static class DirectorControlStyles
		{
			public static GUIStyle UpArrowIcon;
			public static GUIStyle DownArrowIcon;
			public static GUIStyle BoxSelect;
		}
		private CutsceneWrapper cutscene;
		private bool hasLayoutChanged = true;
		private Dictionary<TrackGroupWrapper, TrackGroupControl> trackGroupBinding = new Dictionary<TrackGroupWrapper, TrackGroupControl>();
		private readonly List<SidebarControl> sidebarControls = new List<SidebarControl>();
		private List<TrackItemControl> timelineControls = new List<TrackItemControl>();
		private DirectorControlState directorState = new DirectorControlState();
		public int frameRate;
		private float verticalScrollValue;
		private Texture playButton;
		private Texture pauseButton;
		private Texture stopButton;
		private Texture frameForwardButton;
		private Texture frameBackwardButton;
		private Texture scrubHead;
		private Texture scrubDurationHead;
		private const float HEADER_HEIGHT = 17f;
		private const float SCROLLBAR_WIDTH = 15f;
		private const float TRACK_HEADER_WIDTH_MIN = 256f;
		private const float TRACK_HEADER_WIDTH_MAX = 512f;
		private const float TRACK_HEADER_ICON_WIDTH = 16f;

		private const float TRACK_HEADER_ICON_HEIGHT = 16f;

		private const float MARGIN = 20f;

		private const float SIDEBAR_WIDTH = 4f;
        private float track_header_area_width = 256f;

		private GUISkin customSkin;

		private Rect headerArea;

		private Rect timeRuleArea;

		private Rect bodyArea;

		private Rect trackBodyBackground;

		private Rect trackBodyBackgroundNoVerticalScrollbar;

		private Rect trackBodyBackgroundNoScrollbars;

		private Rect verticalScrollbarArea;

		private Rect previousControlArea;

		private Rect sidebarControlArea;

		private Vector2 mouseDownPosition = Vector2.zero;
		private bool isBoxSelecting;
		private Rect selectionBox;
		public event CutsceneEventHandler PlayCutscene;
		public event CutsceneEventHandler PauseCutscene;
		public event CutsceneEventHandler StopCutscene;
		public event CutsceneEventHandler ScrubCutscene;
		public event CutsceneEventHandler TouchCutscene;
		public event CutsceneEventHandler SetCutsceneTime;
		public event CutsceneEventHandler EnterPreviewMode;
		public event CutsceneEventHandler ExitPreviewMode;
		public event DirectorDragHandler DragPerformed;
		public event CutsceneEventHandler RepaintRequest;

		public ResizeOption ResizeOption
		{
			get
			{
				return directorState.ResizeOption;
			}
			set
			{
				directorState.ResizeOption = value;
			}
		}

		public int DefaultTangentMode
		{
			get
			{
				return directorState.DefaultTangentMode;
			}
			set
			{
				directorState.DefaultTangentMode = value;
			}
		}

		public bool IsSnappingEnabled
		{
			get
			{
				return directorState.IsSnapEnabled;
			}
			set
			{
				directorState.IsSnapEnabled = value;
			}
		}

		public bool InPreviewMode
		{
			get
			{
				return directorState.IsInPreviewMode;
			}
			set
			{
				if (cutscene != null)
				{
					if (!directorState.IsInPreviewMode & value)
					{
						AnimationMode.StartAnimationMode();
						EnterPreviewMode(this, new CinemaDirectorArgs(cutscene.Behaviour));
					}
					else if (directorState.IsInPreviewMode && !value)
					{
						AnimationMode.StopAnimationMode();
						ExitPreviewMode(this, new CinemaDirectorArgs(cutscene.Behaviour));
					}
				}
				directorState.IsInPreviewMode = value;
			}
		}

		public bool InCurveMode
		{
			get
			{
				return directorState.IsInCurveMode;
			}
			set
			{
				directorState.IsInCurveMode = value;
			}
		}

		public bool InMultiMode
        {
			get
            {
				return directorState.InMultiMode;
            }
			set
            {
				directorState.InMultiMode = value;
            }
        }

		public DirectorControl()
		{
			rect = default(Rect);
			frameRate = 30;
			Margin = MARGIN;
			Settings.HorizontalRangeMin = 0f;
			ApplySettings();
		}

		public void OnLoad(GUISkin skin)
		{
			customSkin = skin;
			float num = 0f;
			float num2 = 60f;
			if (EditorPrefs.HasKey("DirectorControl.areaX"))
			{
				num = EditorPrefs.GetFloat("DirectorControl.areaX");
			}
			if (EditorPrefs.HasKey("DirectorControl.areaWidth"))
			{
				num2 = EditorPrefs.GetFloat("DirectorControl.areaWidth");
			}
			if (EditorPrefs.HasKey("DirectorControl.isSnappingEnabled"))
			{
				directorState.IsSnapEnabled = EditorPrefs.GetBool("DirectorControl.isSnappingEnabled");
			}
			float expr_64 = num;
			SetShownHRangeInsideMargins(expr_64, expr_64 + num2);
			if (EditorPrefs.HasKey("DirectorControl.SidebarWidth"))
			{
				track_header_area_width = EditorPrefs.GetFloat("DirectorControl.SidebarWidth");
			}
			if (playButton == null)
			{
				playButton = Resources.Load("Director_PlayIcon", typeof(Texture)) as Texture;
			}
			if (playButton == null)
			{
				Debug.Log("Play button icon missing from Resources folder.");
			}
			if (pauseButton == null)
			{
				pauseButton = (Resources.Load("Director_PauseIcon", typeof(Texture)) as Texture);
			}
			if (pauseButton == null)
			{
				Debug.Log("Pause button missing from Resources folder.");
			}
			if (stopButton == null)
			{
				stopButton = Resources.Load("Director_StopIcon", typeof(Texture)) as Texture;
			}
			if (stopButton == null)
			{
				Debug.Log("Stop button icon missing from Resources folder.");
			}
			if (frameForwardButton == null)
			{
				frameForwardButton = Resources.Load("Director_FrameForwardIcon", typeof(Texture)) as Texture;
			}
			if (frameForwardButton == null)
			{
				Debug.Log("Director_FrameForwardIcon.png missing from Resources folder.");
			}
			if (frameBackwardButton == null)
			{
				frameBackwardButton = Resources.Load("Director_FrameBackwardIcon", typeof(Texture)) as Texture;
			}
			if (frameBackwardButton == null)
			{
				Debug.Log("Director_FrameBackwardIcon.png missing from Resources folder.");
			}
			if (scrubHead == null)
			{
				scrubHead = Resources.Load("Director_Playhead", typeof(Texture)) as Texture;
			}
			if (scrubHead == null)
			{
				Debug.Log("Director_Playhead missing from Resources folder.");
			}
			if (scrubDurationHead == null)
			{
				scrubDurationHead = (Resources.Load("Director_Duration_Playhead", typeof(Texture)) as Texture);
			}
			if (scrubDurationHead == null)
			{
				Debug.Log("Director_Duration_Playhead missing from Resources folder.");
			}
			DirectorControlStyles.BoxSelect = customSkin.FindStyle("BoxSelect");
			DirectorControlStyles.UpArrowIcon = customSkin.FindStyle("UpArrowIcon");
			DirectorControlStyles.DownArrowIcon = customSkin.FindStyle("DownArrowIcon");
			TrackGroupControl.InitStyles(customSkin);
			TimelineTrackControl.InitStyles(customSkin);
		}

		public void OnGUI(Rect controlArea, CutsceneWrapper cs, Rect toolbarArea)
		{
			cutscene = cs;
			updateControlLayout(controlArea);
			drawBackground();
			updateTimelineHeader(headerArea, timeRuleArea);
			if (cutscene != null)
			{
				BindControls(cutscene);
				updateControlState();
				float trackGroupsHeight = getTrackGroupsHeight(cutscene);
				verticalScrollValue = GUI.VerticalScrollbar(verticalScrollbarArea, verticalScrollValue, Mathf.Min(bodyArea.height, trackGroupsHeight), 0f, trackGroupsHeight);
				Translation = new Vector2(Translation.x, verticalScrollValue);
				Rect area = new Rect(bodyArea.x, -Translation.y, bodyArea.width, trackGroupsHeight);
				directorState.Translation = Translation;
				directorState.Scale = Scale;
				GUILayout.BeginArea(bodyArea, string.Empty);
				updateTrackGroups(area, toolbarArea);
				updateDurationBar();
				GUILayout.EndArea();
				updateScrubber();
				BeginViewGUI(true);
				updateUserInput();
				updateDragAndDrop();
			}
		}

		public void OnGlobalEvent()
        {
			foreach (var item in sidebarControls)
            {
				item.OnGlobalEvent();
            }
			foreach (var item in timelineControls)
            {
				item.OnGlobalEvent();
            }
        }

		private void updateUserInput()
		{
			int controlID = GUIUtility.GetControlID("DirectorBody".GetHashCode(), (FocusType)2, trackBodyBackgroundNoVerticalScrollbar);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (trackBodyBackgroundNoVerticalScrollbar.Contains(Event.current.mousePosition) && Event.current.button == 0)
					{
						isBoxSelecting = true;
						mouseDownPosition = Event.current.mousePosition;
						Selection.activeObject = (null);
						GUIUtility.hotControl = (controlID);
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						isBoxSelecting = false;
						selectionBox = default(Rect);
						GUIUtility.hotControl = (0);
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
					{
						float num = Mathf.Clamp(Event.current.mousePosition.x, trackBodyBackgroundNoScrollbars.x, trackBodyBackgroundNoScrollbars.xMax);
						float num2 = Mathf.Clamp(Event.current.mousePosition.y, trackBodyBackgroundNoScrollbars.y, trackBodyBackgroundNoScrollbars.yMax);
						float num3 = Mathf.Min(mouseDownPosition.x, num);
						float num4 = Mathf.Abs(num - mouseDownPosition.x);
						float num5 = Mathf.Min(mouseDownPosition.y, num2);
						float num6 = Mathf.Abs(mouseDownPosition.y - num2);
						selectionBox = new Rect(num3, num5, num4, num6);
						Rect rect = new Rect(selectionBox);
						rect.y = (rect.y - 34f);
						foreach (TrackGroupWrapper current in trackGroupBinding.Keys)
						{
							trackGroupBinding[current].BoxSelect(rect);
						}
					}
					break;
			}
			if (isBoxSelecting)
			{
				GUI.Box(selectionBox, GUIContent.none, DirectorControlStyles.BoxSelect);
			}
		}

		private void updateDragAndDrop()
		{
			Event current = Event.current;
			if ((int)current.type == 15)
			{
				DragAndDrop.PrepareStartDrag();
			}
			if (!bodyArea.Contains(current.mousePosition))
			{
				return;
			}
			EventType type = current.type;
			if (type == EventType.DragUpdated)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
				return;
			}
			if (type != EventType.DragPerform)
			{
				return;
			}
			DragAndDrop.AcceptDrag();
			if (DragPerformed != null)
			{
				DragPerformed(this, new CinemaDirectorDragArgs(cutscene.Behaviour, DragAndDrop.objectReferences));
			}
			current.Use();
		}

		private void updateControlState()
		{
			HScrollMax = cutscene.Duration;
			directorState.TickDistance = GetMajorTickDistance();
			directorState.ScrubberPosition = cutscene.RunningTime;
		}

		private void updateScrubber()
		{
			if (Event.current.type == EventType.Repaint && (InPreviewMode || cutscene.IsPlaying))
			{
				float num = directorState.TimeToPosition(cutscene.RunningTime) + trackBodyBackground.x;
				Color arg_15A_0 = GUI.color;
				GUI.color = new Color(1f, 0f, 0f, 1f);
				Handles.color = new Color(1f, 0f, 0f, 1f);
				if (num > trackBodyBackground.x && num < bodyArea.width)
				{
					GUI.DrawTexture(new Rect(num - 8f, 20f, 16f, 16f), scrubHead);
					Handles.DrawAAPolyLine(1.5f, new Vector2(num, 34f), new Vector2(num, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height + 3f));
				}
				GUI.color = arg_15A_0;
			}
		}

		private void updateDurationBar()
		{
			float num = directorState.TimeToPosition(cutscene.Duration) + trackBodyBackground.x;
			Color color = GUI.color;
			GUI.color = (new Color(0.25f, 0.5f, 0.5f));
			Rect rect = new Rect(num - 8f, bodyArea.height - 13f, 16f, 16f);
			int controlID = GUIUtility.GetControlID("DurationBar".GetHashCode(), (FocusType)2, rect);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (rect.Contains(Event.current.mousePosition))
					{
						GUIUtility.hotControl = (controlID);
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = (0);
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
					{
						Vector2 mousePosition = Event.current.mousePosition;
						mousePosition.x -= trackBodyBackground.x;
						//Undo.RecordObject(cutscene.Behaviour, "Changed Cutscene Duration");
						float x = ViewToDrawingTransformPoint(mousePosition).x;
						cutscene.Duration = directorState.SnappedTime(x);
						Event.current.Use();
					}
					break;
			}
			if (num > trackBodyBackground.x && num < bodyArea.width)
			{
				GUI.DrawTexture(rect, scrubDurationHead);
			}
			GUI.color = (color);
			Handles.color = (new Color(0.25f, 0.5f, 0.5f));
			if (num > trackBodyBackground.x)
			{
				Handles.DrawLine(new Vector3(num, 0f, 0f), new Vector2(num, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height - 13f));
				Handles.DrawLine(new Vector3(num + 1f, 0f, 0f), new Vector2(num + 1f, timeRuleArea.y + trackBodyBackgroundNoVerticalScrollbar.height - 13f));
			}
		}

		private void drawBackground()
		{
			GUI.Box(trackBodyBackground, GUIContent.none, "AnimationKeyframeBackground");
			rect = trackBodyBackgroundNoVerticalScrollbar;
			BeginViewGUI(false);
			SetTickMarkerRanges();
			DrawMajorTicks(trackBodyBackground, frameRate);
			EndViewGUI();
		}

		private void updateControlLayout(Rect controlArea)
		{
			hasLayoutChanged = controlArea != previousControlArea;
			headerArea = new Rect(controlArea.x, controlArea.y, controlArea.width, 17f);
			sidebarControlArea = new Rect(track_header_area_width, headerArea.y + 17f, 4f, controlArea.height - 17f - 15f);
			EditorGUIUtility.AddCursorRect(sidebarControlArea, MouseCursor.ResizeHorizontal);
			int controlID = GUIUtility.GetControlID("SidebarResize".GetHashCode(), FocusType.Passive, sidebarControlArea);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (sidebarControlArea.Contains(Event.current.mousePosition) && Event.current.button == 0)
					{
						GUIUtility.hotControl = (controlID);
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = 0;
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
					{
						track_header_area_width = Mathf.Clamp(Event.current.mousePosition.x, 256f, 512f);
						hasLayoutChanged = true;
					}
					break;
			}
			if (hasLayoutChanged)
			{
				timeRuleArea = new Rect(track_header_area_width + sidebarControlArea.width, controlArea.y, controlArea.width - track_header_area_width - 15f - sidebarControlArea.width, 17f);
				bodyArea = new Rect(controlArea.x, headerArea.y + 17f, controlArea.width - 15f, controlArea.height - 17f - 15f);
				trackBodyBackground = new Rect(controlArea.x + track_header_area_width + sidebarControlArea.width, bodyArea.y, controlArea.width - 15f - track_header_area_width - sidebarControlArea.width, controlArea.height - 17f - 15f);
				trackBodyBackgroundNoVerticalScrollbar = new Rect(controlArea.x + track_header_area_width + sidebarControlArea.width, bodyArea.y, controlArea.width - 15f - track_header_area_width - sidebarControlArea.width, controlArea.height - 17f);
				trackBodyBackgroundNoScrollbars = new Rect(controlArea.x + track_header_area_width + sidebarControlArea.width, bodyArea.y, controlArea.width - 15f - track_header_area_width - sidebarControlArea.width, controlArea.height - 17f - 15f);
				verticalScrollbarArea = new Rect(bodyArea.x + bodyArea.width, bodyArea.y, 15f, controlArea.height - 17f - 15f);
			}
			previousControlArea = controlArea;
		}

		private void BindControls(CutsceneWrapper cutscene)
		{
			var newSidebarControls = new List<SidebarControl>();
			var removedSidebarControls = new List<SidebarControl>();
			var newTimelineControls = new List<TrackItemControl>();
			var removedTimelineControls = new List<TrackItemControl>();
			BindTrackGroupControls(cutscene, newSidebarControls, removedSidebarControls, newTimelineControls, removedTimelineControls);
			foreach (var control in newSidebarControls)
			{
				sidebarControls.Add(control);
				control.DeleteRequest += Control_DeleteRequest;
				control.DuplicateRequest += ItemControl_Duplicate;
				control.SelectRequest += sidebarControl_SelectRequest;
			}
			foreach (var control in removedSidebarControls)
			{
				sidebarControls.Remove(control);
				control.DeleteRequest -= Control_DeleteRequest;
				control.DuplicateRequest -= ItemControl_Duplicate;
				control.SelectRequest -= sidebarControl_SelectRequest;
			}
			foreach (var control in newTimelineControls)
			{
				timelineControls.Add(control);
				control.DeleteRequest += Control_DeleteRequest;
				control.RequestTrackItemTranslate += itemControl_RequestTrackItemTranslate;
				control.TrackItemTranslate += itemControl_TrackItemTranslate;
				control.TrackItemUpdate += itemControl_TrackItemUpdate;
				control.DuplicateRequest += ItemControl_Duplicate;
			}
			foreach (var control in removedTimelineControls)
			{
				timelineControls.Remove(control);
				control.DeleteRequest -= Control_DeleteRequest;
				control.RequestTrackItemTranslate -= itemControl_RequestTrackItemTranslate;
				control.TrackItemTranslate -= itemControl_TrackItemTranslate;
				control.TrackItemUpdate -= itemControl_TrackItemUpdate;
				control.DuplicateRequest -= ItemControl_Duplicate;
			}
		}

		private void BindTrackGroupControls(CutsceneWrapper cutscene, List<SidebarControl> newSidebarControls, List<SidebarControl> removedSidebarControls, List<TrackItemControl> newTimelineControls, List<TrackItemControl> removedTimelineControls)
		{
			if (cutscene.HasChanged)
			{
				foreach (TrackGroupWrapper current in cutscene.TrackGroups)
				{
					if (!trackGroupBinding.ContainsKey(current))
					{
						var arg_5F_0 = DirectorControlHelper.GetAllSubTypes(typeof(TrackGroupControl));
						Type type = typeof(TrackGroupControl);
						int num = 0x7fffffff;
						foreach (var type2 in arg_5F_0)
						{
							Type type3 = null;
							object[] customAttributes = type2.GetCustomAttributes(typeof(CutsceneTrackGroupAttribute), true);
							for (int j = 0; j < customAttributes.Length; j++)
							{
								CutsceneTrackGroupAttribute cutsceneTrackGroupAttribute = (CutsceneTrackGroupAttribute)customAttributes[j];
								if (cutsceneTrackGroupAttribute != null)
								{
									type3 = cutsceneTrackGroupAttribute.TrackGroupType;
								}
							}
							if (type3 == current.Behaviour.GetType())
							{
								type = type2;
								break;
							}
							if (current.Behaviour.GetType().IsSubclassOf(type3))
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
						var trackGroupControl = (TrackGroupControl)Activator.CreateInstance(type);
						trackGroupControl.TrackGroup = current;
						trackGroupControl.DirectorControl = this;
						trackGroupControl.Initialize();
						trackGroupControl.SetExpandedFromEditorPrefs();
						newSidebarControls.Add(trackGroupControl);
						trackGroupBinding.Add(current, trackGroupControl);
					}
				}
				List<TrackGroupWrapper> list = new List<TrackGroupWrapper>();
				foreach (TrackGroupWrapper current2 in trackGroupBinding.Keys)
				{
					bool flag = false;
					foreach (TrackGroupWrapper current3 in cutscene.TrackGroups)
					{
						if (current2.Equals(current3))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						removedSidebarControls.Add(trackGroupBinding[current2]);
						list.Add(current2);
					}
				}
				foreach (TrackGroupWrapper current4 in list)
				{
					trackGroupBinding.Remove(current4);
				}
				SortedDictionary<int, TrackGroupWrapper> sortedDictionary = new SortedDictionary<int, TrackGroupWrapper>();
				List<TrackGroupWrapper> list2 = new List<TrackGroupWrapper>();
				foreach (TrackGroupWrapper current5 in trackGroupBinding.Keys)
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
				using (SortedDictionary<int, TrackGroupWrapper>.ValueCollection.Enumerator enumerator4 = sortedDictionary.Values.GetEnumerator())
				{
					while (enumerator4.MoveNext())
					{
						enumerator4.Current.Ordinal = num3;
						num3++;
					}
				}
				using (List<TrackGroupWrapper>.Enumerator enumerator3 = list2.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						enumerator3.Current.Ordinal = num3;
						num3++;
					}
				}
				cutscene.HasChanged = false;
			}
			foreach (var current6 in trackGroupBinding.Keys)
			{
				trackGroupBinding[current6].BindTrackControls(current6, newSidebarControls, removedSidebarControls, newTimelineControls, removedTimelineControls);
			}
		}

		private void Control_DeleteRequest(object sender, DirectorBehaviourControlEventArgs e)
		{
			foreach (var current in trackGroupBinding.Keys)
			{
				var trackGroupControl = trackGroupBinding[current];
				trackGroupControl.DeleteSelectedChildren();
				if (trackGroupControl.IsSelected)
				{
					trackGroupControl.Delete();
				}
			}
		}

		private void sidebarControl_SelectRequest(object sender, SidebarControlEventArgs e)
		{
			var behaviour = e.Behaviour;
			if (behaviour == null)
			{
				return;
			}
			if (Event.current.control)
			{
				if (DirectorWindow.GetSelection().Contains(behaviour))
				{
					DirectorWindow.GetSelection().Remove(behaviour);
				}
				else
				{
					DirectorWindow.GetSelection().Add(behaviour);
				}
			}
			else
			{
				if (Event.current.shift)
				{
					List<SidebarControl> list = new List<SidebarControl>();
					foreach (TrackGroupWrapper current in trackGroupBinding.Keys)
					{
						TrackGroupControl trackGroupControl = trackGroupBinding[current];
						list.Add(trackGroupControl);
						list.AddRange(trackGroupControl.GetSidebarControlChildren(true));
					}
					SidebarControl sidebarControl = e.SidebarControl;
					SidebarControl sidebarControl2 = e.SidebarControl;
					foreach (SidebarControl current2 in list)
					{
						if (current2.IsSelected)
						{
							if (sidebarControl.CompareTo(current2) > 0)
							{
								sidebarControl = current2;
							}
							if (sidebarControl2.CompareTo(current2) < 0)
							{
								sidebarControl2 = current2;
							}
						}
					}
					using (List<SidebarControl>.Enumerator enumerator2 = list.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							SidebarControl current3 = enumerator2.Current;
							if (!current3.IsSelected && sidebarControl.CompareTo(current3) <= 0 && sidebarControl2.CompareTo(current3) >= 0)
							{
								current3.Select();
							}
						}
						goto IL_195;
					}
				}
				DirectorWindow.GetSelection().activeObject = behaviour;
			}
			IL_195:
			Event.current.Use();
		}

		private void ItemControl_Duplicate(object sender, DirectorBehaviourControlEventArgs e)
		{
			foreach (var current in trackGroupBinding.Keys)
			{
				var trackGroupControl = trackGroupBinding[current];
				if (trackGroupControl.IsSelected)
				{
					trackGroupControl.Duplicate();
				}
				trackGroupControl.DuplicateSelectedChildren();
			}
		}

		private float itemControl_RequestTrackItemTranslate(object sender, TrackItemEventArgs e)
		{
			float num = e.firetime;
			float num2 = e.firetime;
			bool flag = false;
			while (!flag && num2 != 0f)
			{
				foreach (var current in timelineControls)
				{
					if (current.IsSelected)
					{
						if (e.firetime > 0f)
						{
							num2 = Mathf.Min(current.RequestTranslate(num), num2);
						}
						else
						{
							num2 = Mathf.Max(current.RequestTranslate(num), num2);
						}
					}
				}
				if (num2 != num)
				{
					num = num2;
				}
				else
				{
					flag = true;
				}
			}
			return num;
		}

		private float itemControl_TrackItemTranslate(object sender, TrackItemEventArgs e)
		{
			foreach (TrackItemControl current in timelineControls)
			{
				if (current.IsSelected)
				{
					current.Translate(e.firetime);
				}
			}
			return 0f;
		}

		private void itemControl_TrackItemUpdate(object sender, TrackItemEventArgs e)
		{
			foreach (TrackItemControl current in timelineControls)
			{
				if (current.IsSelected)
				{
					current.ConfirmTranslate();
				}
			}
		}

		private void updateTrackGroups(Rect area, Rect controlArea)
		{
			float num = area.y;
			SortedDictionary<int, TrackGroupWrapper> sortedDictionary = new SortedDictionary<int, TrackGroupWrapper>();
			foreach (TrackGroupWrapper current in trackGroupBinding.Keys)
			{
				trackGroupBinding[current].TrackGroup = current;
				sortedDictionary.Add(current.Ordinal, current);
			}
			foreach (int current2 in sortedDictionary.Keys)
			{
				TrackGroupWrapper trackGroupWrapper = sortedDictionary[current2];
				TrackGroupControl trackGroupControl = trackGroupBinding[trackGroupWrapper];
				trackGroupControl.Ordinal = new[]
				{
					current2
				};
				float height = trackGroupControl.GetHeight();
				Rect position = new Rect(area.x, num, area.width, height);
				Rect fullHeader = new Rect(area.x, num, track_header_area_width + sidebarControlArea.width, height);
				Rect safeHeader = new Rect(area.x, num, track_header_area_width - 32f, height);
				Rect rect = new Rect(safeHeader.x + safeHeader.width, num, 16f, 16f);
				Rect arg_1FB_0 = new Rect(rect.x + 16f, num, 16f, 16f);
				Rect content = new Rect(fullHeader.x + fullHeader.width, num, area.width - fullHeader.width, height);
				trackGroupControl.Update(trackGroupWrapper, directorState, position, fullHeader, safeHeader, content, controlArea);
				GUI.enabled = (current2 > 0);
				if (GUI.Button(rect, string.Empty, DirectorControlStyles.UpArrowIcon))
				{
					TrackGroupWrapper expr_1A4 = trackGroupWrapper;
					int ordinal = expr_1A4.Ordinal;
					expr_1A4.Ordinal = ordinal - 1;
					TrackGroupWrapper expr_1CF = trackGroupBinding[sortedDictionary[current2 - 1]].TrackGroup;
					ordinal = expr_1CF.Ordinal;
					expr_1CF.Ordinal = ordinal + 1;
				}
				GUI.enabled = (current2 < sortedDictionary.Count - 1);
				if (GUI.Button(arg_1FB_0, string.Empty, DirectorControlStyles.DownArrowIcon))
				{
					TrackGroupWrapper expr_204 = trackGroupWrapper;
					int ordinal = expr_204.Ordinal;
					expr_204.Ordinal = ordinal + 1;
					TrackGroupWrapper expr_22F = trackGroupBinding[sortedDictionary[current2 + 1]].TrackGroup;
					ordinal = expr_22F.Ordinal;
					expr_22F.Ordinal = ordinal - 1;
				}
				GUI.enabled = true;
				num += height;
			}
		}
		public float TimeToPixel(float time)
		{
			return Mathf.Round(time * frameRate) / frameRate * scale.x + Translation.x;
		}
		public float PixelToTime(float pixelX)
		{
			Rect area = timeRuleArea;
			return ((pixelX - rect.x) * area.width / rect.width + area.x);
		}

        private void updateTimelineHeader(Rect headerArea, Rect timeRulerArea)
		{
			GUILayout.BeginArea(headerArea, string.Empty, EditorStyles.toolbarButton);
			UpdateToolbar();
			GUILayout.BeginArea(timeRulerArea, string.Empty, EditorStyles.toolbarButton);
			GUILayout.EndArea();
			GUILayout.EndArea();
			TimeRuler(timeRulerArea, frameRate);
			if (cutscene == null)
			{
				return;
			}
			int controlID = GUIUtility.GetControlID("TimeRuler".GetHashCode(), FocusType.Passive, timeRulerArea);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					{
						if (!timeRulerArea.Contains(Event.current.mousePosition))
						{
							return;
						}
						GUIUtility.hotControl = controlID;
						Vector2 mousePosition = Event.current.mousePosition;
						mousePosition.x -= timeRulerArea.x;
						InPreviewMode = true;
						float num = ViewToDrawingTransformPoint(mousePosition).x;
						num = Mathf.Max(num, 0f);
						if (cutscene != null)
						{
							directorState.ScrubberPosition = num;
							SetCutsceneTime(this, new CinemaDirectorArgs(cutscene.Behaviour, num));
							return;
						}
						return;
					}
				case EventType.MouseUp:
					if (GUIUtility.hotControl != controlID)
					{
						return;
					}
					GUIUtility.hotControl = 0;
					if (cutscene != null)
					{
						PauseCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour));
						return;
					}
					return;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
					{
						OnScrubCutscene(GetDeltaTime(timeRulerArea));
						Event.current.Use();
						return;
					}
					return;
			}
			if (GUIUtility.hotControl == controlID)
			{
				OnScrubCutscene(GetDeltaTime(timeRulerArea));
			}

			if (timeRulerArea.Contains(Event.current.mousePosition))
			{
				TouchCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour, GetDeltaTime(timeRulerArea)));
			}
		}

        private float GetDeltaTime(Rect timeRulerArea)
        {
	        var mousePosition = Event.current.mousePosition;
	        mousePosition.x -= timeRulerArea.x;
	        return ViewToDrawingTransformPoint(mousePosition).x;
        }

		public void OnScrubCutscene(float deltaTime)
        {
			deltaTime = Mathf.Max(deltaTime, 0f);
			if (cutscene != null)
			{
				ScrubCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour, deltaTime));
				directorState.ScrubberPosition = deltaTime;
			}
		}

		private void UpdateToolbar()
		{
			GUILayoutOption[] array =
			{
				GUILayout.MaxWidth(150f)
			};
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, array);
			GUILayout.FlexibleSpace();
			if (cutscene != null && cutscene.IsPlaying)
			{
				if (GUILayout.Button(pauseButton, EditorStyles.toolbarButton, new GUILayoutOption[0]))
				{
					PauseCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour));
				}
			}
			else if (GUILayout.Button(playButton, EditorStyles.toolbarButton, new GUILayoutOption[0]) && cutscene != null)
			{
				InPreviewMode = true;
				PlayCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour));
			}
			if (GUILayout.Button(stopButton, EditorStyles.toolbarButton, new GUILayoutOption[0]) && cutscene != null)
			{
				InPreviewMode = false;
				StopCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour));
			}
			GUILayout.FlexibleSpace();
			EventType type = Event.current.type;
			if (type == EventType.KeyDown && !EditorGUIUtility.editingTextField && Event.current.keyCode == KeyCode.Space)
			{
				if (!cutscene.IsPlaying)
				{
					InPreviewMode = true;
					PlayCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour));
				}
				else
				{
					PauseCutscene(this, new CinemaDirectorArgs(cutscene.Behaviour));
				}
				Event.current.Use();
			}
			float num = 0f;
			if (cutscene != null)
			{
				num = cutscene.RunningTime;
			}
			GUILayout.Space(10f);
			num = EditorGUILayout.FloatField(num, new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			});
			if (cutscene != null && num != cutscene.RunningTime)
			{
				InPreviewMode = true;
				num = Mathf.Max(num, 0f);
				directorState.ScrubberPosition = num;
				SetCutsceneTime(this, new CinemaDirectorArgs(cutscene.Behaviour, num));
			}
			EditorGUILayout.EndHorizontal();
		}

		private float getTrackGroupsHeight(CutsceneWrapper cutscene)
		{
			float num = 0f;
			foreach (TrackGroupWrapper current in cutscene.TrackGroups)
			{
				if (trackGroupBinding.ContainsKey(current))
				{
					TrackGroupControl trackGroupControl = trackGroupBinding[current];
					num += trackGroupControl.GetHeight();
				}
			}
			return num;
		}

		public void Rescale()
		{
			if (cutscene != null)
			{
				SetShownHRangeInsideMargins(0f, cutscene.Duration);
				return;
			}
			SetShownHRangeInsideMargins(0f, 60f);
		}

		public void ZoomIn()
		{
			Scale *= 1.5f;
		}

		public void ZoomOut()
		{
			Scale *= 0.75f;
		}

		public void OnDisable()
		{
			InPreviewMode = false;
			EditorPrefs.SetFloat("DirectorControl.areaX", shownAreaInsideMargins.x);
			EditorPrefs.SetFloat("DirectorControl.areaWidth", shownAreaInsideMargins.width);
			EditorPrefs.SetBool("DirectorControl.isSnappingEnabled", directorState.IsSnapEnabled);
			EditorPrefs.SetFloat("DirectorControl.SidebarWidth", track_header_area_width);
		}

		public void OnDestroy()
		{
			SortedDictionary<int, TrackGroupWrapper> sortedDictionary = new SortedDictionary<int, TrackGroupWrapper>();
			foreach (TrackGroupWrapper current in trackGroupBinding.Keys)
			{
				trackGroupBinding[current].TrackGroup = current;
				sortedDictionary.Add(current.Ordinal, current);
			}
			foreach (int current2 in sortedDictionary.Keys)
			{
				TrackGroupWrapper trackGroupWrapper = sortedDictionary[current2];
				TrackGroupControl trackGroupControl = trackGroupBinding[trackGroupWrapper];
				trackGroupControl.Ordinal = new[]
				{
					current2
				};
				trackGroupControl.OnDestroy(trackGroupWrapper, directorState);
			}
		}
		public void Repaint()
		{
			if (RepaintRequest != null)
			{
				RepaintRequest(this, new CinemaDirectorArgs(cutscene.Behaviour));
			}
		}
	}
}