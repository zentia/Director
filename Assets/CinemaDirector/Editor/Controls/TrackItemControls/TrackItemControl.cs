using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
	public class TrackItemControl : DirectorBehaviourControl
	{
		private TimelineItemWrapper wrapper;

		private TimelineTrackWrapper track;

		private TimelineTrackControl trackControl;

		protected Rect controlPosition;

		protected int controlID;
		private int drawPriority;

		protected bool renameRequested;

		protected bool isRenaming;

		private int renameControlID;

		protected bool mouseDragActivity;

		protected bool hasSelectionChanged;

		internal event TranslateTrackItemEventHandler RequestTrackItemTranslate;

		internal event TranslateTrackItemEventHandler TrackItemTranslate;

		internal event TrackItemEventHandler TrackItemUpdate;

		public event TrackItemEventHandler AlterTrackItem;

		public TimelineItemWrapper Wrapper
		{
			get
			{
				return wrapper;
			}
			set
			{
				wrapper = value;
				Behaviour = value.TimelineItem;
			}
		}

		public TimelineTrackWrapper Track
		{
			get
			{
				return track;
			}
			set
			{
				track = value;
			}
		}

		public TimelineTrackControl TrackControl
		{
			get
			{
				return trackControl;
			}
			set
			{
				trackControl = value;
			}
		}

		public int DrawPriority
		{
			get
			{
				return drawPriority;
			}
			set
			{
				drawPriority = value;
			}
		}

		public virtual void Initialize(TimelineItemWrapper wrapper, TimelineTrackWrapper track)
		{
			this.wrapper = wrapper;
			this.track = track;
			InitCommand();
		}

		public virtual void PreUpdate(DirectorControlState state, Rect trackPosition)
		{
		}

		public virtual void PostUpdate(DirectorControlState state, bool inArea, EventType type)
		{
		}

		public virtual void HandleInput(DirectorControlState state, Rect trackPosition)
		{
			var timelineItem = wrapper.TimelineItem;
			if (timelineItem == null)
			{
				return;
			}
			float num = wrapper.Firetime * state.Scale.x + state.Translation.x;
			controlPosition = new Rect(num - 8f, 0f, 16f, trackPosition.height);
			controlID = GUIUtility.GetControlID(wrapper.TimelineItem.GetInstanceID(), FocusType.Passive, controlPosition);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (controlPosition.Contains(Event.current.mousePosition) && Event.current.button == 0)
					{
						GUIUtility.hotControl = controlID;
						if (Event.current.control)
						{
							if (IsSelected)
							{
								DirectorWindow.GetSelection().Remove(Wrapper.TimelineItem);
								hasSelectionChanged = true;
							}
							else
							{
								DirectorWindow.GetSelection().Add(Wrapper.TimelineItem);
								hasSelectionChanged = true;
							}
						}
						else if (!IsSelected)
						{
							DirectorWindow.GetSelection().activeObject = timelineItem;
						}
						mouseDragActivity = false;
						Event.current.Use();
					}
					if (controlPosition.Contains(Event.current.mousePosition) && Event.current.button == 1)
					{
						if (!IsSelected)
						{
							DirectorWindow.GetSelection().Add(Wrapper.TimelineItem);
							hasSelectionChanged = true;
						}
						ShowContextMenu(timelineItem);
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						GUIUtility.hotControl = 0;
						if (!mouseDragActivity)
						{
							if (Event.current.control)
							{
								if (!hasSelectionChanged)
								{
									if (IsSelected)
									{
										DirectorWindow.GetSelection().Remove(Wrapper.TimelineItem);
									}
									else
									{
										DirectorWindow.GetSelection().Add(Wrapper.TimelineItem);
									}
								}
							}
							else
							{
								DirectorWindow.GetSelection().activeObject = timelineItem;
							}
						}
						else if (TrackItemUpdate != null)
						{
							TrackItemUpdate(this, new TrackItemEventArgs(wrapper.TimelineItem, wrapper.Firetime));
						}
						hasSelectionChanged = false;
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID && !hasSelectionChanged)
					{
						Undo.RecordObject(timelineItem, string.Format("Changed {0}", timelineItem.name));

						var mousePosition = Event.current.mousePosition;
						float num2 = (mousePosition.x - state.Translation.x) / state.Scale.x;
						num2 = state.SnappedTime(num2);
						if (!mouseDragActivity)
						{
							mouseDragActivity = Wrapper.Firetime != num2;
						}
						if (RequestTrackItemTranslate != null)
						{
							float firetime = num2 - wrapper.Firetime;
							float firetime2 = RequestTrackItemTranslate(this, new TrackItemEventArgs(wrapper.TimelineItem, firetime));
							if (TrackItemTranslate != null)
							{
								TrackItemTranslate(this, new TrackItemEventArgs(wrapper.TimelineItem, firetime2));
							}
						}
					}
					break;
			}
			if (DirectorWindow.GetSelection().Contains(timelineItem))
			{
				if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy")
				{
					Event.current.Use();
				}
				if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Copy")
				{
					DirectorCopyPaste.Copy(timelineItem);
					Event.current.Use();
				}
				ExecuteCommand(Event.current);
			}
		}

        protected override void Paste()
        {
			var genericTrackContral = trackControl;
			if (genericTrackContral != null)
            {
				genericTrackContral.PasteItem(Vector2.zero, track.Behaviour as TimelineTrack, true);
            }
        }

        public override DirectorObject Duplicate(DirectorObject parent = null)
        {
			DirectorCopyPaste.deepCopy = wrapper.TimelineItem;
			var genericTrackContral = trackControl;
			if (genericTrackContral != null)
			{
				var directorObject = genericTrackContral.PasteItem(Vector2.zero, track.Behaviour as TimelineTrack, true, "Duplicate ", Wrapper.TimelineItem);
				if (parent != null)
					directorObject.SetParent(parent);
				return directorObject;
			}
			return null;
		}

        protected override void Record()
        {
        }

        public virtual void Draw(DirectorControlState state)
		{
			var behaviour = wrapper.TimelineItem;
			if (behaviour == null)
			{
				return;
			}
			Color color = GUI.color;
			if (IsSelected)
			{
				GUI.color = new Color(0.5f, 0.6f, 0.905f, 1f);
			}
			else if (DirectorWindow.Instance.cutscene.Duration < behaviour.Firetime)
			{
				GUI.color = Color.gray;
			}
			Rect rect = controlPosition;
			rect.height = 17f;
			GUI.Box(rect, GUIContent.none, TimelineTrackControl.styles.EventItemStyle);
			if (trackControl.isExpanded)
			{
				GUI.Box(new Rect(controlPosition.x, rect.yMax, controlPosition.width, controlPosition.height - rect.height), GUIContent.none, TimelineTrackControl.styles.EventItemBottomStyle);
			}
			GUI.color = color;
		}

		protected virtual void DrawRenameLabel(string name, Rect labelPosition, GUIStyle labelStyle = null)
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
				if (!EditorGUIUtility.editingTextField || renameControlID != GUIUtility.keyboardControl || (int)Event.current.keyCode == 13 || (Event.current.type == EventType.MouseDown && !labelPosition.Contains(Event.current.mousePosition)))
				{
					isRenaming = false;
					GUIUtility.hotControl = (0);
					GUIUtility.keyboardControl = (0);
					EditorGUIUtility.editingTextField = (false);
					int num = DrawPriority;
					DrawPriority = num - 1;
				}
			}
			if (Behaviour.name != name)
			{
				Undo.RecordObject(Behaviour, string.Format("Renamed {0}", Behaviour.name));
				Behaviour.name = (name);
			}
			if (!isRenaming)
			{
				if (IsSelected)
				{
					GUI.Label(labelPosition, Behaviour.name, EditorStyles.whiteLabel);
					return;
				}
				GUI.Label(labelPosition, Behaviour.name);
			}
		}

		protected virtual void ShowContextMenu(DirectorObject behaviour)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Copy"), false, new GenericMenu.MenuFunction2(copyItem), behaviour);
			menu.AddItem(new GUIContent("Delete"), false, new GenericMenu.MenuFunction2(deleteItem), behaviour);
			menu.AddItem(new GUIContent("Record"), false, Record);
			menu.ShowAsContext();
		}

		protected void renameItem(object userData)
		{
			if (userData as Behaviour != null)
			{
				renameRequested = true;
				isRenaming = true;
				int num = DrawPriority;
				DrawPriority = num + 1;
			}
		}

		protected void copyItem(object userData)
		{
			var behaviour = userData as DirectorObject;
			if (behaviour != null)
			{
				DirectorCopyPaste.Copy(behaviour);
			}
		}

        protected void deleteItem(object userData)
		{
			DirectorObject behaviour = userData as DirectorObject;
			if (behaviour != null)
			{
				Behaviour = behaviour;
				RequestDelete();
			}
		}

		internal virtual void BoxSelect(Rect selectionBox)
		{
			Rect rect = new Rect(controlPosition);
			rect.x += trackControl.Rect.x;
			rect.y += trackControl.Rect.y;
			if (rect.Overlaps(selectionBox, true))
			{
				DirectorWindow.GetSelection().Add(wrapper.TimelineItem);
				return;
			}
			DirectorWindow.GetSelection().Remove(wrapper.TimelineItem);
		}

		protected void TriggerTrackItemUpdateEvent()
		{
			if (TrackItemUpdate != null)
			{
				TrackItemUpdate(this, new TrackItemEventArgs(wrapper.TimelineItem, wrapper.Firetime));
			}
		}

		protected void TriggerRequestTrackItemTranslate(float firetime)
		{
			if (RequestTrackItemTranslate != null)
			{
				float firetime2 = firetime - wrapper.Firetime;
				float firetime3 = RequestTrackItemTranslate(this, new TrackItemEventArgs(wrapper.TimelineItem, firetime2));
				if (TrackItemTranslate != null)
				{
					TrackItemTranslate(this, new TrackItemEventArgs(wrapper.TimelineItem, firetime3));
				}
			}
		}

		internal virtual float RequestTranslate(float amount)
		{
			float num = Wrapper.Firetime + amount;
			float num2 = Mathf.Max(0f, num);
			return amount + (num2 - num);
		}

		internal virtual void Translate(float amount)
		{
			Wrapper.Firetime += amount;
		}

		internal virtual void ConfirmTranslate()
		{
			if (AlterTrackItem != null)
			{
				AlterTrackItem(this, new TrackItemEventArgs(wrapper.TimelineItem, wrapper.Firetime));
			}
		}
	}
}