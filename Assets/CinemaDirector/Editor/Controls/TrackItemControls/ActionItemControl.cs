using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
	public class ActionItemControl : TrackItemControl
	{
		protected const float ITEM_RESIZE_HANDLE_SIZE = 5f;

		private static float mouseDownOffset = -1f;

		protected Texture actionIcon;

		public event ActionItemEventHandler AlterAction;

		public override void HandleInput(DirectorControlState state, Rect trackPosition)
		{
			CinemaActionWrapper cinemaActionWrapper = Wrapper as CinemaActionWrapper;
			if (cinemaActionWrapper == null)
			{
				return;
			}
			if (isRenaming)
			{
				return;
			}
			float num = cinemaActionWrapper.Firetime * state.Scale.x + state.Translation.x;
			float num2 = (cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration) * state.Scale.x + state.Translation.x;
			controlPosition = new Rect(num, 0f, num2 - num, trackPosition.height);
			Rect rect = new Rect(num, 0f, 5f, controlPosition.height);
			Rect rect2 = new Rect(num + 5f, 0f, num2 - num - 10f, controlPosition.height);
			Rect rect3 = new Rect(num2 - 5f, 0f, 5f, controlPosition.height);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
			EditorGUIUtility.AddCursorRect(rect2, MouseCursor.SlideArrow);
			EditorGUIUtility.AddCursorRect(rect3, MouseCursor.ResizeHorizontal);
			this.controlID = GUIUtility.GetControlID(Wrapper.TimelineItem.GetInstanceID(), FocusType.Passive, controlPosition);
			int controlID = GUIUtility.GetControlID(Wrapper.TimelineItem.GetInstanceID(), FocusType.Passive, rect);
			int controlID2 = GUIUtility.GetControlID(Wrapper.TimelineItem.GetInstanceID(), FocusType.Passive, rect2);
			int controlID3 = GUIUtility.GetControlID(Wrapper.TimelineItem.GetInstanceID(), FocusType.Passive, rect3);
			if (Event.current.GetTypeForControl(this.controlID) == EventType.MouseDown && rect2.Contains(Event.current.mousePosition) && Event.current.button == 1)
			{
				if (!IsSelected)
				{
					DirectorWindow.GetSelection().Add(Wrapper.TimelineItem);
					hasSelectionChanged = true;
				}
				ShowContextMenu(Wrapper.TimelineItem);
				Event.current.Use();
			}
			switch (Event.current.GetTypeForControl(controlID2))
			{
				case EventType.MouseDown:
					if (rect2.Contains(Event.current.mousePosition) && Event.current.button == 0)
					{
						GUIUtility.hotControl = controlID2;
						if (Event.current.control)
						{
							if (IsSelected)
							{
								DirectorWindow.GetSelection().Remove(Wrapper.TimelineItem);
							}
							else
							{
								DirectorWindow.GetSelection().Add(Wrapper.TimelineItem);
							}
							hasSelectionChanged = true;
						}
						else if (!IsSelected)
						{
							DirectorWindow.GetSelection().activeObject = Behaviour;
						}
						mouseDragActivity = false;
						mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionWrapper.Firetime;
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID2)
					{
						mouseDownOffset = -1f;
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
								DirectorWindow.GetSelection().activeObject = Behaviour;
							}
						}
						else
						{
							TriggerTrackItemUpdateEvent();
						}
						hasSelectionChanged = false;
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID2 && !hasSelectionChanged)
					{
						Undo.RecordObject(Behaviour, string.Format("Changed {0}", Behaviour.name));
						float num3 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
						num3 = state.SnappedTime(num3 - mouseDownOffset);
						if (!mouseDragActivity)
						{
							mouseDragActivity = Wrapper.Firetime != num3;
						}
						TriggerRequestTrackItemTranslate(num3);
					}
					break;
			}
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:
					if (rect.Contains(Event.current.mousePosition))
					{
						GUIUtility.hotControl = (controlID);
						mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionWrapper.Firetime;
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID)
					{
						mouseDownOffset = -1f;
						GUIUtility.hotControl = 0;
						if (AlterAction != null)
						{
							AlterAction(this, new ActionItemEventArgs(cinemaActionWrapper.TimelineItem, cinemaActionWrapper.Firetime, cinemaActionWrapper.Duration));
						}
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID)
					{
						float num4 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
						num4 = state.SnappedTime(num4);
						float num5 = 0f;
						float num6 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
						using (IEnumerator<TimelineItemWrapper> enumerator = Track.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								CinemaActionWrapper cinemaActionWrapper2 = enumerator.Current as CinemaActionWrapper;
								if (cinemaActionWrapper2 != null && cinemaActionWrapper2.TimelineItem != Wrapper.TimelineItem)
								{
									float num7 = cinemaActionWrapper2.Firetime + cinemaActionWrapper2.Duration;
									if (num7 <= Wrapper.Firetime)
									{
										num5 = Mathf.Max(num5, num7);
									}
								}
							}
						}
						num4 = Mathf.Max(num5, num4);
						num4 = Mathf.Min(num6, num4);
						cinemaActionWrapper.Duration += Wrapper.Firetime - num4;
						cinemaActionWrapper.Firetime = num4;
					}
					break;
			}
			switch (Event.current.GetTypeForControl(controlID3))
			{
				case EventType.MouseDown:
					if (rect3.Contains(Event.current.mousePosition))
					{
						GUIUtility.hotControl = (controlID3);
						mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - Wrapper.Firetime;
						Event.current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID3)
					{
						mouseDownOffset = -1f;
						GUIUtility.hotControl = (0);
						if (AlterAction != null)
						{
							AlterAction(this, new ActionItemEventArgs(cinemaActionWrapper.TimelineItem, cinemaActionWrapper.Firetime, cinemaActionWrapper.Duration));
						}
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlID3)
					{
						float num8 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
						num8 = state.SnappedTime(num8);
						float num9 = float.PositiveInfinity;
						using (IEnumerator<TimelineItemWrapper> enumerator = Track.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								CinemaActionWrapper cinemaActionWrapper3 = enumerator.Current as CinemaActionWrapper;
								if (cinemaActionWrapper3 != null && cinemaActionWrapper3.TimelineItem != Wrapper.TimelineItem)
								{
									float num10 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
									if (cinemaActionWrapper3.Firetime >= num10)
									{
										num9 = Mathf.Min(num9, cinemaActionWrapper3.Firetime);
									}
								}
							}
						}
						num8 = Mathf.Clamp(num8, Wrapper.Firetime, num9);
						cinemaActionWrapper.Duration = num8 - Wrapper.Firetime;
					}
					break;
			}
			if (DirectorWindow.GetSelection().activeObject == Wrapper.TimelineItem)
			{
				if (Event.current.type == EventType.ValidateCommand)
				{
					if (Event.current.commandName == "Copy")
						Event.current.Use();
				}
				if (Event.current.type == EventType.ExecuteCommand)
				{
					if (Event.current.commandName == "Copy")
                    {
						DirectorCopyPaste.Copy(Wrapper.TimelineItem);
						Event.current.Use();
					}
				}
				ExecuteCommand(Event.current);
			}
		}

		public override void Draw(DirectorControlState state)
		{
			if (Wrapper.TimelineItem == null)
			{
				return;
			}
			string text = Behaviour.name;
			if (isRenaming)
			{
				GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
				GUI.SetNextControlName("TrackItemControlRename");
				text = EditorGUI.TextField(controlPosition, GUIContent.none, text);
				if (renameRequested)
				{
					EditorGUI.FocusTextInControl("TrackItemControlRename");
					renameRequested = false;
				}
				if (!IsSelected || Event.current.keyCode == KeyCode.Return || (Event.current.type == EventType.MouseDown || Event.current.type == EventType.Ignore && !controlPosition.Contains(Event.current.mousePosition)))
				{
					isRenaming = false;
					GUIUtility.hotControl = (0);
					GUIUtility.keyboardControl = (0);
					int drawPriority = DrawPriority;
					DrawPriority = drawPriority - 1;
				}
			}
			if (Behaviour.name != text)
			{
				Undo.RecordObject(Behaviour, string.Format("Renamed {0}", Behaviour.name));
				Behaviour.name = text;
			}
			if (!isRenaming)
			{
				if (IsSelected)
				{
					GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
					return;
				}
				GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemStyle);
			}
		}

		private bool doActionsConflict(float firetime, float endtime, float newFiretime, float newEndtime)
		{
			//return (newFiretime >= firetime && newFiretime < endtime) || (firetime >= newFiretime && firetime < newEndtime) || newFiretime < 0f;
			return false;
		}

        internal override float RequestTranslate(float amount)
        {
            CinemaActionWrapper cinemaActionWrapper = Wrapper as CinemaActionWrapper;
            if (cinemaActionWrapper == null)
            {
                return 0f;
            }
            float num = Wrapper.Firetime + amount;
            float num2 = Wrapper.Firetime + amount;
            bool flag = true;
            float num3 = 0f;
            float num4 = float.PositiveInfinity;
            float newEndtime = num2 + cinemaActionWrapper.Duration;
            using (IEnumerator<TimelineItemWrapper> enumerator = Track.Items.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    CinemaActionWrapper cinemaActionWrapper2 = enumerator.Current as CinemaActionWrapper;
                    if (cinemaActionWrapper2 != null && cinemaActionWrapper2.TimelineItem != cinemaActionWrapper.TimelineItem && !DirectorWindow.GetSelection().Contains(cinemaActionWrapper2.TimelineItem))
                    {
                        float num5 = cinemaActionWrapper2.Firetime + cinemaActionWrapper2.Duration;
                        float num6 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
                        if (doActionsConflict(cinemaActionWrapper2.Firetime, num5, num2, newEndtime))
                        {
                            flag = false;
                        }
                        if (num5 <= cinemaActionWrapper.Firetime)
                        {
                            num3 = Mathf.Max(num3, num5);
                        }
                        if (cinemaActionWrapper2.Firetime >= num6)
                        {
                            num4 = Mathf.Min(num4, cinemaActionWrapper2.Firetime);
                        }
                    }
                }
            }
            float num7;
            if (flag)
            {
                num7 = Mathf.Max(0f, num2);
            }
            else
            {
                num2 = Mathf.Max(num3, num2);
                num2 = Mathf.Min(num4 - cinemaActionWrapper.Duration, num2);
                num7 = num2;
            }
            return amount + (num7 - num);
        }

        internal override void ConfirmTranslate()
		{
			CinemaActionWrapper cinemaActionWrapper = Wrapper as CinemaActionWrapper;
			if (cinemaActionWrapper == null)
			{
				return;
			}
			if (AlterAction != null)
			{
				AlterAction(this, new ActionItemEventArgs(cinemaActionWrapper.TimelineItem, cinemaActionWrapper.Firetime, cinemaActionWrapper.Duration));
			}
		}
	}
}