using DirectorEditor;
using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using CinemaDirector;

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

	[method: CompilerGenerated]
	[CompilerGenerated]
	internal event TranslateTrackItemEventHandler RequestTrackItemTranslate;

	[method: CompilerGenerated]
	[CompilerGenerated]
	internal event TranslateTrackItemEventHandler TrackItemTranslate;

	[method: CompilerGenerated]
	[CompilerGenerated]
	internal event TrackItemEventHandler TrackItemUpdate;

	[method: CompilerGenerated]
	[CompilerGenerated]
	public event TrackItemEventHandler AlterTrackItem;

	public TimelineItemWrapper Wrapper
	{
		get
		{
			return this.wrapper;
		}
		set
		{
			this.wrapper = value;
			base.Behaviour = value.Behaviour;
		}
	}

	public TimelineTrackWrapper Track
	{
		get
		{
			return this.track;
		}
		set
		{
			this.track = value;
		}
	}

	public TimelineTrackControl TrackControl
	{
		get
		{
			return this.trackControl;
		}
		set
		{
			this.trackControl = value;
		}
	}

	public int DrawPriority
	{
		get
		{
			return this.drawPriority;
		}
		set
		{
			this.drawPriority = value;
		}
	}

	public virtual void Initialize(TimelineItemWrapper wrapper, TimelineTrackWrapper track)
	{
		this.wrapper = wrapper;
		this.track = track;
	}

	public virtual void PreUpdate(DirectorControlState state, Rect trackPosition)
	{
	}

	public virtual void PostUpdate(DirectorControlState state, bool inArea, EventType type)
	{
	}

	public virtual void HandleInput(DirectorControlState state, Rect trackPosition)
	{
		Behaviour behaviour = this.wrapper.Behaviour;
		if (behaviour == null)
		{
			return;
		}
		float num = this.wrapper.Firetime * state.Scale.x + state.Translation.x;
		this.controlPosition = new Rect(num - 8f, 0f, 16f, trackPosition.height);
		this.controlID = GUIUtility.GetControlID(this.wrapper.Behaviour.GetInstanceID(), (FocusType)2, this.controlPosition);
		switch ((int)Event.current.GetTypeForControl(this.controlID))
		{
		case 0:
			if (this.controlPosition.Contains(Event.current.mousePosition) && (int)Event.current.button == 0)
			{
				GUIUtility.hotControl=(this.controlID);
				if (Event.current.control)
				{
					if (base.IsSelected)
					{
						GameObject[] gameObjects = Selection.gameObjects;
						ArrayUtility.Remove<GameObject>(ref gameObjects, this.Wrapper.Behaviour.gameObject);
						Selection.objects=(gameObjects);
						this.hasSelectionChanged = true;
					}
					else
					{
						GameObject[] gameObjects2 = Selection.gameObjects;
						ArrayUtility.Add<GameObject>(ref gameObjects2, this.Wrapper.Behaviour.gameObject);
						Selection.objects=(gameObjects2);
						this.hasSelectionChanged = true;
					}
				}
				else if (!base.IsSelected)
				{
					Selection.activeInstanceID=(behaviour.GetInstanceID());
				}
				this.mouseDragActivity = false;
				Event.current.Use();
			}
			if (this.controlPosition.Contains(Event.current.mousePosition) && (int)Event.current.button == 1)
			{
				if (!base.IsSelected)
				{
					GameObject[] gameObjects3 = Selection.gameObjects;
					ArrayUtility.Add<GameObject>(ref gameObjects3, this.Wrapper.Behaviour.gameObject);
					Selection.objects=(gameObjects3);
					this.hasSelectionChanged = true;
				}
				this.showContextMenu(behaviour);
				Event.current.Use();
			}
			break;
		case 1:
			if (GUIUtility.hotControl == this.controlID)
			{
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
								ArrayUtility.Remove<GameObject>(ref gameObjects4, this.Wrapper.Behaviour.gameObject);
								Selection.objects=(gameObjects4);
							}
							else
							{
								GameObject[] gameObjects5 = Selection.gameObjects;
								ArrayUtility.Add<GameObject>(ref gameObjects5, this.Wrapper.Behaviour.gameObject);
								Selection.objects=(gameObjects5);
							}
						}
					}
					else
					{
						Selection.activeInstanceID=(behaviour.GetInstanceID());
					}
				}
				else if (this.TrackItemUpdate != null)
				{
					this.TrackItemUpdate(this, new TrackItemEventArgs(this.wrapper.Behaviour, this.wrapper.Firetime));
				}
				this.hasSelectionChanged = false;
			}
			break;
		case 3:
			if (GUIUtility.hotControl == this.controlID && !this.hasSelectionChanged)
			{
				Undo.RecordObject(behaviour, string.Format("Changed {0}", behaviour.name));
				float num2 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num2 = state.SnappedTime(num2);
				if (!this.mouseDragActivity)
				{
					this.mouseDragActivity = (this.Wrapper.Firetime != num2);
				}
				if (this.RequestTrackItemTranslate != null)
				{
					float firetime = num2 - this.wrapper.Firetime;
					float firetime2 = this.RequestTrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, firetime));
					if (this.TrackItemTranslate != null)
					{
						this.TrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, firetime2));
					}
				}
			}
			break;
		}
		if (Selection.activeGameObject == behaviour.gameObject)
		{
			if ((int)(int)Event.current.type == 13 && Event.current.commandName == "Copy")
			{
				Event.current.Use();
			}
			if ((int)Event.current.type == 14 && Event.current.commandName == "Copy")
			{
				DirectorCopyPaste.Copy(behaviour);
				Event.current.Use();
			}
		}
		if ((int)Event.current.type == 4 && (int)Event.current.keyCode == 127 && Selection.activeGameObject == behaviour.gameObject)
		{
			deleteItem(behaviour);
			Event.current.Use();
		}
	}

	public virtual void Draw(DirectorControlState state)
	{
		Behaviour behaviour = this.wrapper.Behaviour;
		if (behaviour == null)
		{
			return;
		}
		Color arg_BF_0 = GUI.color;
		if (base.IsSelected)
		{
			GUI.color=(new Color(0.5f, 0.6f, 0.905f, 1f));
		}
		Rect rect = this.controlPosition;
		rect.height=(17f);
		GUI.Box(rect, GUIContent.none, TimelineTrackControl.styles.EventItemStyle);
		if (this.trackControl.isExpanded)
		{
			GUI.Box(new Rect(this.controlPosition.x, rect.yMax, this.controlPosition.width, this.controlPosition.height - rect.height), GUIContent.none, TimelineTrackControl.styles.EventItemBottomStyle);
		}
		GUI.color=(arg_BF_0);
		Rect labelPosition = new Rect(this.controlPosition.x + 16f, this.controlPosition.y, 128f, this.controlPosition.height);
		string name = behaviour.name;
		this.DrawRenameLabel(name, labelPosition, null);
	}

	protected virtual void DrawRenameLabel(string name, Rect labelPosition, GUIStyle labelStyle = null)
	{
		if (this.isRenaming)
		{
			GUI.SetNextControlName("TrackItemControlRename");
			name = EditorGUI.TextField(labelPosition, GUIContent.none, name);
			if (this.renameRequested)
			{
				EditorGUI.FocusTextInControl("TrackItemControlRename");
				this.renameRequested = false;
				this.renameControlID = GUIUtility.keyboardControl;
			}
			if (!EditorGUIUtility.editingTextField || this.renameControlID != GUIUtility.keyboardControl || (int)Event.current.keyCode == 13 || (Event.current.type == EventType.MouseDown && !labelPosition.Contains(Event.current.mousePosition)))
			{
				this.isRenaming = false;
				GUIUtility.hotControl=(0);
				GUIUtility.keyboardControl=(0);
				EditorGUIUtility.editingTextField=(false);
				int num = this.DrawPriority;
				this.DrawPriority = num - 1;
			}
		}
		if (base.Behaviour.name != name)
		{
			Undo.RecordObject(base.Behaviour.gameObject, string.Format("Renamed {0}", base.Behaviour.name));
			base.Behaviour.name=(name);
		}
		if (!this.isRenaming)
		{
			if (base.IsSelected)
			{
				GUI.Label(labelPosition, base.Behaviour.name, EditorStyles.whiteLabel);
				return;
			}
			GUI.Label(labelPosition, base.Behaviour.name);
		}
	}

	protected virtual void showContextMenu(Behaviour behaviour)
	{
		GenericMenu expr_05 = new GenericMenu();
		expr_05.AddItem(new GUIContent("Rename"), false, new GenericMenu.MenuFunction2(this.renameItem), behaviour);
		expr_05.AddItem(new GUIContent("Copy"), false, new GenericMenu.MenuFunction2(this.copyItem), behaviour);
		expr_05.AddItem(new GUIContent("Delete"), false, new GenericMenu.MenuFunction2(this.deleteItem), behaviour);
		expr_05.ShowAsContext();
	}

	protected void renameItem(object userData)
	{
		if (userData as Behaviour != null)
		{
			this.renameRequested = true;
			this.isRenaming = true;
			int num = this.DrawPriority;
			DrawPriority = num + 1;
		}
	}

	protected void copyItem(object userData)
	{
		Behaviour behaviour = userData as Behaviour;
		if (behaviour != null)
		{
			DirectorCopyPaste.Copy(behaviour);
		}
	}

	protected void deleteItem(object userData)
	{
		Behaviour behaviour = userData as Behaviour;
		if (behaviour != null)
		{
			Behaviour = behaviour;
			RequestDelete();
		}
	}

    protected void loadItem(object userData)
    {
        CinemaActorClipCurve behaviour = userData as CinemaActorClipCurve;
        if (behaviour != null)
        {
            Animator animator = behaviour.Actor.GetComponent<Animator>();
            if (animator == null)
            {
                EditorUtility.DisplayDialog("错误", "没有找到Animator组件", "确定");
                return;
            }

            AnimatorClipInfo[] clip = animator.GetCurrentAnimatorClipInfo(0);
            if (clip == null || clip.Length <= 0)
            {
                EditorUtility.DisplayDialog("错误", "没有找到animator资源", "确定");
                return;
            }

            var bind = AnimationUtility.GetCurveBindings(clip[0].clip);
            if (bind == null)
            {
                EditorUtility.DisplayDialog("错误", "没有找到bindings", "确定");
                return;
            }

            var curve = AnimationUtility.GetEditorCurve(clip[0].clip, bind[0]);
            if (curve == null)
            {
                EditorUtility.DisplayDialog("错误", "没有找到曲线", "确定");
                return;
            }
            behaviour.curveData.Clear();
            AddClipCurveData(behaviour, bind[0], true, curve, behaviour.Actor.transform);
            var data = AnimationUtility.GetEditorCurve(clip[0].clip, bind[1]);
            behaviour.curveData[0].SetCurve(1,data);
            data = AnimationUtility.GetEditorCurve(clip[0].clip, bind[2]);
            behaviour.curveData[0].SetCurve(2,data);

			curve = AnimationUtility.GetEditorCurve(clip[0].clip, bind[3]);

            AddClipCurveDataRotation(behaviour, bind[3], true, curve, behaviour.Actor.transform);
            data = AnimationUtility.GetEditorCurve(clip[0].clip, bind[4]);
            behaviour.curveData[1].SetCurve(1, data);
            data = AnimationUtility.GetEditorCurve(clip[0].clip, bind[5]);
            behaviour.curveData[1].SetCurve(2, data);
        }
    }
    public void AddClipCurveData(CinemaActorClipCurve curveData, EditorCurveBinding component, bool isProperty, AnimationCurve curve, Component com)
    {
        MemberClipCurveData data = new MemberClipCurveData();
        data.SetCurve(0, curve);
        data.Type = component.type.Name;
        data.PropertyName = "localPosition";
        data.IsProperty = isProperty;
        data.PropertyType = PropertyTypeInfo.Vector3;
        curveData.curveData.Add(data);
    }
    public void AddClipCurveDataRotation(CinemaActorClipCurve curveData, EditorCurveBinding component, bool isProperty, AnimationCurve curve, Component com)
    {
        MemberClipCurveData data = new MemberClipCurveData();
        data.SetCurve(0, curve);
        data.Type = component.type.Name;
        data.PropertyName = "localEulerAngles";
        data.IsProperty = isProperty;
        data.PropertyType = PropertyTypeInfo.Vector3;
        curveData.curveData.Add(data);
    }
    internal virtual void BoxSelect(Rect selectionBox)
	{
		Rect rect = new Rect(controlPosition);
		rect.x = rect.x + trackControl.Rect.x;
		rect.y = rect.y + trackControl.Rect.y;
		if (rect.Overlaps(selectionBox, true))
		{
			GameObject[] gameObjects = Selection.gameObjects;
			ArrayUtility.Add(ref gameObjects, wrapper.Behaviour.gameObject);
			Selection.objects = gameObjects;

			return;
		}
		if (Selection.Contains(wrapper.Behaviour.gameObject))
		{
			GameObject[] gameObjects2 = Selection.gameObjects;
			ArrayUtility.Remove(ref gameObjects2, wrapper.Behaviour.gameObject);
			Selection.objects=(gameObjects2);
		}
	}

	internal new void Delete()
	{
		Undo.DestroyObjectImmediate(this.Wrapper.Behaviour.gameObject);
	}

	protected void TriggerTrackItemUpdateEvent()
	{
		if (TrackItemUpdate != null)
		{
			TrackItemUpdate(this, new TrackItemEventArgs(this.wrapper.Behaviour, this.wrapper.Firetime));
		}
	}

	protected void TriggerRequestTrackItemTranslate(float firetime)
	{
		if (this.RequestTrackItemTranslate != null)
		{
			float firetime2 = firetime - this.wrapper.Firetime;
			float firetime3 = this.RequestTrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, firetime2));
			if (this.TrackItemTranslate != null)
			{
				this.TrackItemTranslate(this, new TrackItemEventArgs(this.wrapper.Behaviour, firetime3));
			}
		}
	}

	internal virtual float RequestTranslate(float amount)
	{
		float num = this.Wrapper.Firetime + amount;
		float num2 = Mathf.Max(0f, num);
		return amount + (num2 - num);
	}

	internal virtual void Translate(float amount)
	{
		this.Wrapper.Firetime += amount;
	}

	internal virtual void ConfirmTranslate()
	{
		if (this.AlterTrackItem != null)
		{
			this.AlterTrackItem(this, new TrackItemEventArgs(this.wrapper.Behaviour, this.wrapper.Firetime));
		}
	}
}
