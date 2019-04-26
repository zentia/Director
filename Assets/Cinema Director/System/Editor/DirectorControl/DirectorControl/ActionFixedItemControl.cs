using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class ActionFixedItemControl : ActionItemControl
{
	private static float mouseDownOffset = -1f;

	[method: CompilerGenerated]
	[CompilerGenerated]
	public event ActionFixedItemEventHandler AlterFixedAction;

	public override void HandleInput(DirectorControlState state, Rect trackPosition)
	{
		CinemaActionFixedWrapper cinemaActionFixedWrapper = base.Wrapper as CinemaActionFixedWrapper;
		if (cinemaActionFixedWrapper == null)
		{
			return;
		}
		if (this.isRenaming)
		{
			return;
		}
		float num = state.TimeToPosition(cinemaActionFixedWrapper.Firetime);
		float num2 = state.TimeToPosition(cinemaActionFixedWrapper.Firetime + cinemaActionFixedWrapper.Duration);
		this.controlPosition = new Rect(num, 0f, num2 - num, trackPosition.height);
		bool expr_67 = this.controlPosition.width < 15f;
		float num3 = expr_67 ? 0f : 5f;
		Rect rect = new Rect(num, 0f, num3, trackPosition.height);
		Rect rect2 = new Rect(num + num3, 0f, num2 - num - 2f * num3, trackPosition.height);
		Rect rect3 = new Rect(num2 - num3, 0f, num3, trackPosition.height);
		EditorGUIUtility.AddCursorRect(rect2, (MouseCursor)5);
		if (!expr_67)
		{
			EditorGUIUtility.AddCursorRect(rect, (MouseCursor)3);
			EditorGUIUtility.AddCursorRect(rect3, (MouseCursor)3);
		}
		this.controlID = GUIUtility.GetControlID(cinemaActionFixedWrapper.Behaviour.GetInstanceID(), (FocusType)2, this.controlPosition);
		int controlID = GUIUtility.GetControlID(cinemaActionFixedWrapper.Behaviour.GetInstanceID(), (FocusType)2, rect);
		int controlID2 = GUIUtility.GetControlID(cinemaActionFixedWrapper.Behaviour.GetInstanceID(), (FocusType)2, rect2);
		int controlID3 = GUIUtility.GetControlID(cinemaActionFixedWrapper.Behaviour.GetInstanceID(), (FocusType)2, rect3);
		if (Event.current.GetTypeForControl(this.controlID) == EventType.MouseDown && rect2.Contains(Event.current.mousePosition) && (int)Event.current.button == 1)
		{
			if (!base.IsSelected)
			{
				GameObject[] gameObjects = Selection.gameObjects;
				ArrayUtility.Add<GameObject>(ref gameObjects, base.Wrapper.Behaviour.gameObject);
				Selection.objects=(gameObjects);
				this.hasSelectionChanged = true;
			}
			this.showContextMenu(cinemaActionFixedWrapper.Behaviour);
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
				ActionFixedItemControl.mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionFixedWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID2)
			{
				ActionFixedItemControl.mouseDownOffset = -1f;
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
			if (GUIUtility.hotControl == controlID2)
			{
				Undo.RecordObject(base.Behaviour, string.Format("Changed {0}", base.Behaviour.name));
				float num4 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num4 = state.SnappedTime(num4 - ActionFixedItemControl.mouseDownOffset);
				if (!this.mouseDragActivity)
				{
					this.mouseDragActivity = (base.Wrapper.Firetime != num4);
				}
				base.TriggerRequestTrackItemTranslate(num4);
			}
			break;
		}
		switch ((int)Event.current.GetTypeForControl(controlID))
		{
		case 0:
			if (rect.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID);
				ActionFixedItemControl.mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionFixedWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID)
			{
				ActionFixedItemControl.mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
			}
			break;
		case 3:
			if (GUIUtility.hotControl == controlID)
			{
				float num5 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num5 = state.SnappedTime(num5);
				if (num5 >= 0f)
				{
					float num6 = cinemaActionFixedWrapper.InTime - (cinemaActionFixedWrapper.Firetime - num5);
					num6 = Mathf.Clamp(num6, 0f, cinemaActionFixedWrapper.ItemLength);
					float num7 = num6 - cinemaActionFixedWrapper.InTime;
					cinemaActionFixedWrapper.InTime = num6;
					cinemaActionFixedWrapper.Firetime += num7;
					if (this.AlterFixedAction != null)
					{
						this.AlterFixedAction(this, new ActionFixedItemEventArgs(cinemaActionFixedWrapper.Behaviour, cinemaActionFixedWrapper.Firetime, cinemaActionFixedWrapper.Duration, cinemaActionFixedWrapper.InTime, cinemaActionFixedWrapper.OutTime));
					}
				}
			}
			break;
		}
		switch ((int)Event.current.GetTypeForControl(controlID3))
		{
		case 0:
			if (rect3.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID3);
				ActionFixedItemControl.mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - cinemaActionFixedWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == controlID3)
			{
				ActionFixedItemControl.mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
			}
			break;
		case 3:
			if (GUIUtility.hotControl == controlID3)
			{
				float num8 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num8 = state.SnappedTime(num8);
				float num9 = num8 - (cinemaActionFixedWrapper.Firetime - cinemaActionFixedWrapper.InTime);
				Undo.RecordObject(cinemaActionFixedWrapper.Behaviour, string.Format("Changed {0}", cinemaActionFixedWrapper.Behaviour.name));
				cinemaActionFixedWrapper.OutTime = Mathf.Clamp(num9, 0f, cinemaActionFixedWrapper.ItemLength);
				if (this.AlterFixedAction != null)
				{
					this.AlterFixedAction(this, new ActionFixedItemEventArgs(cinemaActionFixedWrapper.Behaviour, cinemaActionFixedWrapper.Firetime, cinemaActionFixedWrapper.Duration, cinemaActionFixedWrapper.InTime, cinemaActionFixedWrapper.OutTime));
				}
			}
			break;
		}
		if (Selection.activeGameObject == Wrapper.Behaviour.gameObject)
		{
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Copy")
			{
				Event.current.Use();
			}
			if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Copy")
			{
				DirectorCopyPaste.Copy(Wrapper.Behaviour);
				Event.current.Use();
			}
		}
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && Selection.activeGameObject == Wrapper.Behaviour.gameObject)
		{
			deleteItem(Wrapper.Behaviour);
			Event.current.Use();
		}
	}

	internal override void ConfirmTranslate()
	{
		CinemaActionFixedWrapper cinemaActionFixedWrapper = Wrapper as CinemaActionFixedWrapper;
		if (cinemaActionFixedWrapper == null)
		{
			return;
		}
		if (this.AlterFixedAction != null)
		{
			this.AlterFixedAction(this, new ActionFixedItemEventArgs(cinemaActionFixedWrapper.Behaviour, cinemaActionFixedWrapper.Firetime, cinemaActionFixedWrapper.Duration, cinemaActionFixedWrapper.InTime, cinemaActionFixedWrapper.OutTime));
		}
	}
}
