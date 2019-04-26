using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class ActionItemControl : TrackItemControl
{
	protected const float ITEM_RESIZE_HANDLE_SIZE = 5f;

	private static float mouseDownOffset = -1f;

	protected Texture actionIcon;

	[method: CompilerGenerated]
	[CompilerGenerated]
	public event ActionItemEventHandler AlterAction;

	public override void HandleInput(DirectorControlState state, Rect trackPosition)
	{
		CinemaActionWrapper cinemaActionWrapper = base.Wrapper as CinemaActionWrapper;
		if (cinemaActionWrapper == null)
		{
			return;
		}
		if (this.isRenaming)
		{
			return;
		}
		float num = cinemaActionWrapper.Firetime * state.Scale.x + state.Translation.x;
		float num2 = (cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration) * state.Scale.x + state.Translation.x;
		this.controlPosition = new Rect(num, 0f, num2 - num, trackPosition.height);
		Rect rect = new Rect(num, 0f, 5f, this.controlPosition.height);
		Rect rect2 = new Rect(num + 5f, 0f, num2 - num - 10f, this.controlPosition.height);
		Rect rect3 = new Rect(num2 - 5f, 0f, 5f, this.controlPosition.height);
		EditorGUIUtility.AddCursorRect(rect, (MouseCursor)3);
		EditorGUIUtility.AddCursorRect(rect2, (MouseCursor)5);
		EditorGUIUtility.AddCursorRect(rect3, (MouseCursor)3);
		this.controlID = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), (FocusType)2, this.controlPosition);
		int controlID = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect);
		int controlID2 = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect2);
		int controlID3 = GUIUtility.GetControlID(base.Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect3);
		if (Event.current.GetTypeForControl(this.controlID) == EventType.MouseDown && rect2.Contains(Event.current.mousePosition) && (int)Event.current.button == 1)
		{
			if (!base.IsSelected)
			{
				GameObject[] gameObjects = Selection.gameObjects;
				ArrayUtility.Add<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
				Selection.objects=(gameObjects);
				this.hasSelectionChanged = true;
			}
			this.showContextMenu(base.Wrapper.Behaviour);
			Event.current.Use();
		}
		switch ((int)Event.current.GetTypeForControl(controlID2))
		{
		case 0:
			if (rect2.Contains(Event.current.mousePosition) && (int)Event.current.button == 0)
			{
				GUIUtility.hotControl=(controlID2);
				if (Event.current.control)
				{
					if (base.IsSelected)
					{
						GameObject[] gameObjects2 = Selection.gameObjects;
						ArrayUtility.Remove<GameObject>(ref gameObjects2, base.Wrapper.Behaviour.gameObject);
						Selection.objects=(gameObjects2);
						this.hasSelectionChanged = true;
					}
					else
					{
						GameObject[] gameObjects3 = Selection.gameObjects;
						ArrayUtility.Add<GameObject>(ref gameObjects3, base.Wrapper.Behaviour.gameObject);
						Selection.objects=(gameObjects3);
						this.hasSelectionChanged = true;
					}
				}
				else if (!base.IsSelected)
				{
					Selection.activeInstanceID=(base.Behaviour.GetInstanceID());
				}
				this.mouseDragActivity = false;
				ActionItemControl.mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID2)
			{
				ActionItemControl.mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
				if (!this.mouseDragActivity)
				{
					if (Event.current.control)
					{
						if (!this.hasSelectionChanged)
						{
							if (base.IsSelected)
							{
								GameObject[] gameObjects4 = Selection.gameObjects;
								ArrayUtility.Remove<GameObject>(ref gameObjects4, base.Wrapper.Behaviour.gameObject);
								Selection.objects=(gameObjects4);
							}
							else
							{
								GameObject[] gameObjects5 = Selection.gameObjects;
								ArrayUtility.Add<GameObject>(ref gameObjects5, base.Wrapper.Behaviour.gameObject);
								Selection.objects=(gameObjects5);
							}
						}
					}
					else
					{
						Selection.activeInstanceID=(base.Behaviour.GetInstanceID());
					}
				}
				else
				{
					base.TriggerTrackItemUpdateEvent();
				}
				this.hasSelectionChanged = false;
			}
			break;
		case 3:
			if (GUIUtility.hotControl == controlID2 && !this.hasSelectionChanged)
			{
				Undo.RecordObject(base.Behaviour, string.Format("Changed {0}", base.Behaviour.name));
				float num3 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num3 = state.SnappedTime(num3 - ActionItemControl.mouseDownOffset);
				if (!this.mouseDragActivity)
				{
					this.mouseDragActivity = (base.Wrapper.Firetime != num3);
				}
				base.TriggerRequestTrackItemTranslate(num3);
			}
			break;
		}
		switch ((int)Event.current.GetTypeForControl(controlID))
		{
		case 0:
			if (rect.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID);
				ActionItemControl.mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID)
			{
				ActionItemControl.mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
				if (this.AlterAction != null)
				{
					this.AlterAction(this, new ActionItemEventArgs(cinemaActionWrapper.Behaviour, cinemaActionWrapper.Firetime, cinemaActionWrapper.Duration));
				}
			}
			break;
		case 3:
			if (GUIUtility.hotControl == controlID)
			{
				float num4 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num4 = state.SnappedTime(num4);
				float num5 = 0f;
				float num6 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
				using (IEnumerator<TimelineItemWrapper> enumerator = base.Track.Items.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						CinemaActionWrapper cinemaActionWrapper2 = enumerator.Current as CinemaActionWrapper;
						if (cinemaActionWrapper2 != null && cinemaActionWrapper2.Behaviour != base.Wrapper.Behaviour)
						{
							float num7 = cinemaActionWrapper2.Firetime + cinemaActionWrapper2.Duration;
							if (num7 <= base.Wrapper.Firetime)
							{
								num5 = Mathf.Max(num5, num7);
							}
						}
					}
				}
				num4 = Mathf.Max(num5, num4);
				num4 = Mathf.Min(num6, num4);
				cinemaActionWrapper.Duration += base.Wrapper.Firetime - num4;
				cinemaActionWrapper.Firetime = num4;
			}
			break;
		}
		switch ((int)Event.current.GetTypeForControl(controlID3))
		{
		case 0:
			if (rect3.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID3);
				ActionItemControl.mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - base.Wrapper.Firetime;
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID3)
			{
				ActionItemControl.mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
				if (this.AlterAction != null)
				{
					this.AlterAction(this, new ActionItemEventArgs(cinemaActionWrapper.Behaviour, cinemaActionWrapper.Firetime, cinemaActionWrapper.Duration));
				}
			}
			break;
		case 3:
			if (GUIUtility.hotControl == controlID3)
			{
				float num8 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num8 = state.SnappedTime(num8);
				float num9 = float.PositiveInfinity;
				using (IEnumerator<TimelineItemWrapper> enumerator = base.Track.Items.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						CinemaActionWrapper cinemaActionWrapper3 = enumerator.Current as CinemaActionWrapper;
						if (cinemaActionWrapper3 != null && cinemaActionWrapper3.Behaviour != base.Wrapper.Behaviour)
						{
							float num10 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
							if (cinemaActionWrapper3.Firetime >= num10)
							{
								num9 = Mathf.Min(num9, cinemaActionWrapper3.Firetime);
							}
						}
					}
				}
				num8 = Mathf.Clamp(num8, base.Wrapper.Firetime, num9);
				cinemaActionWrapper.Duration = num8 - base.Wrapper.Firetime;
			}
			break;
		}
		if (Selection.activeGameObject == base.Wrapper.Behaviour.gameObject)
		{
			if (Event.current.type == (EventType)13 && Event.current.commandName == "Copy")
			{
				Event.current.Use();
			}
			if (Event.current.type == (EventType)14 && Event.current.commandName == "Copy")
			{
				DirectorCopyPaste.Copy(base.Wrapper.Behaviour);
				Event.current.Use();
			}
		}
		if (Event.current.type == (EventType)4 && Event.current.keyCode == (KeyCode)127 && Selection.activeGameObject == base.Wrapper.Behaviour.gameObject)
		{
			base.deleteItem(base.Wrapper.Behaviour);
			Event.current.Use();
		}
	}

	public override void Draw(DirectorControlState state)
	{
		if (base.Wrapper.Behaviour == null)
		{
			return;
		}
		string text = base.Behaviour.name;
		if (this.isRenaming)
		{
			GUI.Box(this.controlPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
			GUI.SetNextControlName("TrackItemControlRename");
			text = EditorGUI.TextField(this.controlPosition, GUIContent.none, text);
			if (this.renameRequested)
			{
				EditorGUI.FocusTextInControl("TrackItemControlRename");
				this.renameRequested = false;
			}
			if (!base.IsSelected || Event.current.keyCode == (KeyCode)13 || ((Event.current.type == EventType.MouseDown || Event.current.type == (EventType)11) && !this.controlPosition.Contains(Event.current.mousePosition)))
			{
				this.isRenaming = false;
				GUIUtility.hotControl=(0);
				GUIUtility.keyboardControl=(0);
				int drawPriority = base.DrawPriority;
				base.DrawPriority = drawPriority - 1;
			}
		}
		if (base.Behaviour.name != text)
		{
			Undo.RecordObject(base.Behaviour.gameObject, string.Format("Renamed {0}", base.Behaviour.name));
			base.Behaviour.name=(text);
		}
		if (!this.isRenaming)
		{
			if (base.IsSelected)
			{
				GUI.Box(this.controlPosition, new GUIContent(text), TimelineTrackControl.styles.TrackItemSelectedStyle);
				return;
			}
			GUI.Box(this.controlPosition, new GUIContent(text), TimelineTrackControl.styles.TrackItemStyle);
		}
	}

	private bool doActionsConflict(float firetime, float endtime, float newFiretime, float newEndtime)
	{
		return (newFiretime >= firetime && newFiretime < endtime) || (firetime >= newFiretime && firetime < newEndtime) || newFiretime < 0f;
	}

	internal override float RequestTranslate(float amount)
	{
		CinemaActionWrapper cinemaActionWrapper = base.Wrapper as CinemaActionWrapper;
		if (cinemaActionWrapper == null)
		{
			return 0f;
		}
		float num = base.Wrapper.Firetime + amount;
		float num2 = base.Wrapper.Firetime + amount;
		bool flag = true;
		float num3 = 0f;
		float num4 = float.PositiveInfinity;
		float newEndtime = num2 + cinemaActionWrapper.Duration;
		using (IEnumerator<TimelineItemWrapper> enumerator = base.Track.Items.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				CinemaActionWrapper cinemaActionWrapper2 = enumerator.Current as CinemaActionWrapper;
				if (cinemaActionWrapper2 != null && cinemaActionWrapper2.Behaviour != cinemaActionWrapper.Behaviour && !Selection.Contains(cinemaActionWrapper2.Behaviour.gameObject))
				{
					float num5 = cinemaActionWrapper2.Firetime + cinemaActionWrapper2.Duration;
					float num6 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
					if (this.doActionsConflict(cinemaActionWrapper2.Firetime, num5, num2, newEndtime))
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
		CinemaActionWrapper cinemaActionWrapper = base.Wrapper as CinemaActionWrapper;
		if (cinemaActionWrapper == null)
		{
			return;
		}
		if (AlterAction != null)
		{
			AlterAction(this, new ActionItemEventArgs(cinemaActionWrapper.Behaviour, cinemaActionWrapper.Firetime, cinemaActionWrapper.Duration));
		}
	}
}
