using DirectorEditor;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class CinemaCurveClipItemControl : ActionItemControl
{
	private class KeyframeContext
	{
		public CinemaAnimationCurveWrapper curveWrapper;
		public int key;
		public CinemaKeyframeWrapper ckw;
		public KeyframeContext(CinemaAnimationCurveWrapper curveWrapper, int key, CinemaKeyframeWrapper ckw)
		{
			this.curveWrapper = curveWrapper;
			this.key = key;
			this.ckw = ckw;
		}
	}
	private class CurveContext
	{
		public CinemaAnimationCurveWrapper curveWrapper;

		public DirectorControlState state;

		public float time;

		public CurveContext(CinemaAnimationCurveWrapper curveWrapper, DirectorControlState state, float time)
		{
			this.curveWrapper = curveWrapper;
			this.state = state;
			this.time = time;
		}
	}
	private class CurvesContext
	{
		public DirectorControlState state;

		public CinemaClipCurveWrapper wrapper;

		public float time;

		public CurvesContext(CinemaClipCurveWrapper wrapper, float time, DirectorControlState state)
		{
			this.wrapper = wrapper;
			this.time = time;
			this.state = state;
		}
	}
	private bool isEditing;
	protected bool isAutoResizeEnabled = true;
	protected bool isCurveClipEmpty = true;
	protected bool isFolded;
	private bool haveCurvesChanged;
    protected CinemaCurveSelection selection = ScriptableObject.CreateInstance<CinemaCurveSelection>();
	
	private SortedList<float, int> keyframeTimes = new SortedList<float, int>();
	private Rect viewingSpace = new Rect(0f, 0f, 1f, 1f);
	private Rect timelineItemPosition;
	private Rect masterKeysPosition;
	private Rect curveCanvasPosition;
	private Rect curveTrackSafeArea;
    private bool m_ValidRect;
    private Vector2 m_StartPoint;
    private Vector2 m_EndPoint;
	private float scrollBarPosition;
	private static float mouseDownOffset = -1f;
	private const float SAFE_ZONE_BUFFER = 8f;
	private const float DEFAULT_TRACK_LOWER_RANGE = 0f;
	private const float DEFAULT_TRACK_UPPER_RANGE = 1f;
	private const float SAFE_ZONE_BUFFER_WIDTH = 4f;
	private const float THRESHOLD = 0.01f;
	private const float TANGENT_HANDLE_LENGTH = 30f;
	private const int INDENT_AMOUNT = 12;
	private const float ONE_THIRD = 0.333333343f;
	[method: CompilerGenerated]
	[CompilerGenerated]
	public event CurveClipItemEventHandler TranslateCurveClipItem;
	[method: CompilerGenerated]
	[CompilerGenerated]
	public event CurveClipItemEventHandler AlterFiretime;
	[method: CompilerGenerated]
	[CompilerGenerated]
	public event CurveClipItemEventHandler AlterDuration;
	[method: CompilerGenerated]
	[CompilerGenerated]
	public event CurveClipSrubberEventHandler SnapScrubber;
	[method: CompilerGenerated]
	[CompilerGenerated]
	public event CurveClipWrapperEventHandler CurvesChanged;
	[method: CompilerGenerated]
	[CompilerGenerated]
	internal event CurveClipWrapperEventHandler RequestEdit;
	public bool IsEditing
	{
		get
		{
			return isEditing;
		}
		set
		{
			isEditing = value;
		}
	}
	public bool HaveCurvesChanged
	{
		get
		{
			return haveCurvesChanged;
		}
		set
		{
			haveCurvesChanged = value;
		}
	}
	public abstract void UpdateCurveWrappers(CinemaClipCurveWrapper clipWrapper);
	public override void PreUpdate(DirectorControlState state, Rect trackPosition)
	{
		CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
		if (cinemaClipCurveWrapper == null)
		{
			return;
		}
		isCurveClipEmpty = cinemaClipCurveWrapper.IsEmpty;
		isFolded = (trackPosition.height == 17f);
		if (GUIUtility.hotControl == 0)
		{
			UpdateCurveWrappers(cinemaClipCurveWrapper);
			viewingSpace = getViewingArea(isAutoResizeEnabled, cinemaClipCurveWrapper);
		}
		float num = cinemaClipCurveWrapper.Firetime * state.Scale.x + state.Translation.x;
		float num2 = (cinemaClipCurveWrapper.Firetime + cinemaClipCurveWrapper.Duration) * state.Scale.x + state.Translation.x;
		controlPosition = new Rect(num, 0f, num2 - num, trackPosition.height);
		if (isCurveClipEmpty || this.isFolded)
		{
			timelineItemPosition = controlPosition;
		}
		else
		{
			timelineItemPosition = new Rect(controlPosition.x, this.controlPosition.y, this.controlPosition.width, 17f);
		}
		if (isEditing)
		{
			masterKeysPosition = new Rect(controlPosition.x, timelineItemPosition.y + this.timelineItemPosition.height, this.controlPosition.width, 17f);
			curveCanvasPosition = new Rect(controlPosition.x, this.masterKeysPosition.y + this.masterKeysPosition.height, this.controlPosition.width, trackPosition.height - this.timelineItemPosition.height - this.masterKeysPosition.height);
		}
		else
		{
			curveCanvasPosition = new Rect(controlPosition.x, timelineItemPosition.y + timelineItemPosition.height, controlPosition.width, trackPosition.height - this.timelineItemPosition.height);
		}
		curveTrackSafeArea = new Rect(curveCanvasPosition.x, curveCanvasPosition.y + 8f, curveCanvasPosition.width, curveCanvasPosition.height - 16f);
		if (!isCurveClipEmpty)
		{
			updateKeyframes(cinemaClipCurveWrapper, state);
		}
	}
	private void updateKeyframes(CinemaClipCurveWrapper clipWrapper, DirectorControlState state)
	{
		float num = viewingSpace.height - viewingSpace.y;
		float num2 = curveTrackSafeArea.y + curveTrackSafeArea.height;
		for (int i = 0; i < clipWrapper.MemberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = clipWrapper.MemberCurves[i];
			for (int j = 0; j < cinemaMemberCurveWrapper.AnimationCurves.Length; j++)
			{
				CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = cinemaMemberCurveWrapper.AnimationCurves[j];
				for (int k = 0; k < cinemaAnimationCurveWrapper.KeyframeCount; k++)
				{
					Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
					Vector2 screenPosition = new Vector2(state.TimeToPosition(keyframe.time), num2 - (keyframe.value - viewingSpace.y) / num * curveTrackSafeArea.height);
					CinemaKeyframeWrapper keyframeWrapper = cinemaAnimationCurveWrapper.GetKeyframeWrapper(k);
					keyframeWrapper.ScreenPosition = screenPosition;
					if (k < cinemaAnimationCurveWrapper.KeyframeCount - 1)
					{
						float num3 = Mathf.Abs(cinemaAnimationCurveWrapper.GetKeyframe(k + 1).time - keyframe.time) * 0.333333343f;
						float num4 = keyframe.outTangent;
						if (float.IsPositiveInfinity(keyframe.outTangent))
						{
							num4 = 0f;
						}
						Vector2 vector = new Vector2(keyframe.time + num3, keyframe.value + num3 * num4);
						Vector2 outTangentControlPointPosition = new Vector2(vector.x * state.Scale.x + state.Translation.x, num2 - (vector.y - viewingSpace.y) / num * curveTrackSafeArea.height);
						keyframeWrapper.OutTangentControlPointPosition = outTangentControlPointPosition;
					}
					if (k > 0)
					{
						float num5 = Mathf.Abs(cinemaAnimationCurveWrapper.GetKeyframe(k - 1).time - keyframe.time) * 0.333333343f;
						float num6 = keyframe.inTangent;
						if (float.IsPositiveInfinity(keyframe.inTangent))
						{
							num6 = 0f;
						}
						Vector2 vector2 = new Vector2(keyframe.time - num5, keyframe.value - num5 * num6);
						Vector2 inTangentControlPointPosition = new Vector2(vector2.x * state.Scale.x + state.Translation.x, num2 - (vector2.y - this.viewingSpace.y) / num * this.curveTrackSafeArea.height);
						keyframeWrapper.InTangentControlPointPosition = inTangentControlPointPosition;
					}
				}
			}
		}
	}

    private CinemaMemberCurveWrapper GetOnlySelfMemberCurveWrapper(CinemaMemberCurveWrapper[] memberCurves)
    {
        for (int i = 0; i < memberCurves.Length; i++)
        {
            if (memberCurves[i].onlySelf)
                return memberCurves[i];
        }

        return null;
    }

    private void updateMasterKeysCurve(CinemaMemberCurveWrapper cinemaMemberCurveWrapper)
    {
        CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
        for (int j = 0; j < animationCurves.Length; j++)
        {
            CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
            if (cinemaAnimationCurveWrapper.IsVisible)
            {
                for (int k = 0; k < cinemaAnimationCurveWrapper.KeyframeCount; k++)
                {
                    Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
                    if (!keyframeTimes.ContainsKey(keyframe.time))
                    {
                        keyframeTimes.Add(keyframe.time, cinemaAnimationCurveWrapper.GetKeyframeWrapper(k).GetHash());
                    }
                }
            }
        }
    }
	private void updateMasterKeys(CinemaClipCurveWrapper clipWrapper)
	{
		keyframeTimes = new SortedList<float, int>();
		CinemaMemberCurveWrapper[] memberCurves = clipWrapper.MemberCurves;
	    var onlySeleCurves = GetOnlySelfMemberCurveWrapper(memberCurves);
	    if (onlySeleCurves != null)
	    {
	        updateMasterKeysCurve(onlySeleCurves);
	    }
	    else
	    {
	        for (int i = 0; i < memberCurves.Length; i++)
	        {
	            CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
	            if (cinemaMemberCurveWrapper.IsVisible)
	            {
	                updateMasterKeysCurve(cinemaMemberCurveWrapper);
	            }
	        }
        }
		
		for (int l = 0; l < keyframeTimes.Count; l++)
		{
			keyframeTimes[keyframeTimes.Keys[l]] = GUIUtility.GetControlID("MasterKeyframe".GetHashCode(), FocusType.Passive);
		}
	}
	public override void HandleInput(DirectorControlState state, Rect trackPosition)
	{
		CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
		if (cinemaClipCurveWrapper == null)
		{
			return;
		}
		handleItemInput(cinemaClipCurveWrapper, state, trackPosition);
		if (!isCurveClipEmpty && IsEditing && !isFolded)
		{
			handleKeyframeInput(cinemaClipCurveWrapper, state);
			updateMasterKeys(cinemaClipCurveWrapper);
			handleMasterKeysInput(cinemaClipCurveWrapper, state);
		}
	}
	private void handleKeyframeInput(CinemaClipCurveWrapper clipCurveWrapper, DirectorControlState state)
	{
		float num = viewingSpace.height - viewingSpace.y;
		int controlID = GUIUtility.GetControlID("KeyframeControl".GetHashCode(), (FocusType)2);
		CinemaMemberCurveWrapper[] memberCurves = clipCurveWrapper.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
			CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
				if (cinemaAnimationCurveWrapper.IsVisible)
				{
					for (int k = 0; k < cinemaAnimationCurveWrapper.KeyframeCount; k++)
					{
						Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
						bool flag = selection.Type == cinemaMemberCurveWrapper.Type && selection.Property == cinemaMemberCurveWrapper.PropertyName && selection.KeyId == k && selection.CurveId == cinemaAnimationCurveWrapper.Id;
						bool flag2 = keyframe.time == clipCurveWrapper.Firetime || keyframe.time == clipCurveWrapper.Firetime + clipCurveWrapper.Duration;
						Vector2 keyframeScreenPosition = cinemaAnimationCurveWrapper.GetKeyframeScreenPosition(k);
						Rect rect = new Rect(keyframeScreenPosition.x - 4f, keyframeScreenPosition.y - 4f, 8f, 8f);
						switch (Event.current.GetTypeForControl(controlID))
						{
						case EventType.MouseDown:
							if (rect.Contains(Event.current.mousePosition))
							{
								GUIUtility.hotControl=(controlID);
								Selection.activeInstanceID=Wrapper.Behaviour.GetInstanceID();
								selection.Type = cinemaMemberCurveWrapper.Type;
								selection.Property = cinemaMemberCurveWrapper.PropertyName;
								selection.CurveId = cinemaAnimationCurveWrapper.Id;
								selection.KeyId = k;
								Event.current.Use();
							}
							break;
						case EventType.MouseUp:
							if (GUIUtility.hotControl == controlID && Event.current.button == 1)
							{
								if (flag)
								{
									showKeyframeContextMenu(cinemaAnimationCurveWrapper, k, flag2);
									GUIUtility.hotControl=0;
									Event.current.Use();
								}
							}
							else if (GUIUtility.hotControl == controlID && flag)
							{
								GUIUtility.hotControl=(0);
								Event.current.Use();
								if (CurvesChanged != null)
								{
									CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
									if (cinemaClipCurveWrapper != null)
									{
										CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
									}
									haveCurvesChanged = true;
								}
							}
							break;
						case EventType.MouseDrag:
							if (GUIUtility.hotControl == controlID && Event.current.button == 0 && flag)
							{
								Keyframe keyframe2 = cinemaAnimationCurveWrapper.GetKeyframe(k);
								float num2 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
								float num3 = (curveTrackSafeArea.y + curveTrackSafeArea.height - Event.current.mousePosition.y) / curveTrackSafeArea.height * num + viewingSpace.y;
								num2 = state.SnappedTime(num2);
								if (flag2)
								{
									num2 = keyframe2.time;
								}
								Keyframe kf = new Keyframe(num2, num3, keyframe2.inTangent, keyframe2.outTangent);
								kf.tangentMode = keyframe2.tangentMode;
								if ((num2 > clipCurveWrapper.Firetime && num2 < clipCurveWrapper.Firetime + clipCurveWrapper.Duration) | flag2)
								{
									selection.KeyId = cinemaAnimationCurveWrapper.MoveKey(k, kf);
									CinemaClipCurveWrapper cinemaClipCurveWrapper2 = Wrapper as CinemaClipCurveWrapper;
									if (cinemaClipCurveWrapper2 != null)
									{
										CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper2));
									}
									haveCurvesChanged = true;
								}
							}
							break;
						}
					}
				}
			}
		}
		handleKeyframeTangentInput(clipCurveWrapper, state, num);
	}
	private void handleKeyframeTangentInput(CinemaClipCurveWrapper clipCurveWrapper, DirectorControlState state, float verticalRange)
	{
		CinemaMemberCurveWrapper[] memberCurves = clipCurveWrapper.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
			CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
				for (int k = 0; k < cinemaAnimationCurveWrapper.KeyframeCount; k++)
				{
					Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
					CinemaKeyframeWrapper keyframeWrapper = cinemaAnimationCurveWrapper.GetKeyframeWrapper(k);
					bool arg_C4_0 = this.selection.Type == cinemaMemberCurveWrapper.Type && this.selection.Property == cinemaMemberCurveWrapper.PropertyName && this.selection.KeyId == k && this.selection.CurveId == cinemaAnimationCurveWrapper.Id;
					if (keyframe.time != clipCurveWrapper.Firetime)
					{
						bool arg_C3_0 = keyframe.time == clipCurveWrapper.Firetime + clipCurveWrapper.Duration;
					}
					if (arg_C4_0 && !cinemaAnimationCurveWrapper.IsAuto(k))
					{
						if (k > 0 && !cinemaAnimationCurveWrapper.IsLeftLinear(k) && !cinemaAnimationCurveWrapper.IsLeftConstant(k))
						{
							Vector2 vector = new Vector2(keyframeWrapper.InTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.InTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
							vector.Normalize();
							vector *= 30f;
							Rect rect = new Rect(keyframeWrapper.ScreenPosition.x + vector.x - 4f, keyframeWrapper.ScreenPosition.y + vector.y - 4f, 8f, 8f);
							int controlID = GUIUtility.GetControlID("TangentIn".GetHashCode(), (FocusType)2);
							switch ((int)Event.current.GetTypeForControl(controlID))
							{
							case 0:
								if (rect.Contains(Event.current.mousePosition))
								{
									GUIUtility.hotControl=(controlID);
									Event.current.Use();
								}
								break;
							case 1:
								if (GUIUtility.hotControl == controlID)
								{
									GUIUtility.hotControl=(0);
									if (CurvesChanged != null)
									{
										CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
										if (cinemaClipCurveWrapper != null)
										{
											CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
										}
										haveCurvesChanged = true;
									}
								}
								break;
							case 3:
								if (GUIUtility.hotControl == controlID)
								{
									Vector2 vector2 = new Vector2((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x, (curveTrackSafeArea.y + this.curveTrackSafeArea.height - Event.current.mousePosition.y) / this.curveTrackSafeArea.height * verticalRange + this.viewingSpace.y) - new Vector2(keyframe.time, keyframe.value);
									float num = vector2.y / vector2.x;
									float num2 = keyframe.outTangent;
									if (cinemaAnimationCurveWrapper.IsFreeSmooth(k))
									{
										num2 = num;
									}
									Keyframe kf = new Keyframe(keyframe.time, keyframe.value, num, num2);
									kf.tangentMode=(keyframe.tangentMode);
									cinemaAnimationCurveWrapper.MoveKey(k, kf);
									CinemaClipCurveWrapper cinemaClipCurveWrapper2 = base.Wrapper as CinemaClipCurveWrapper;
									if (cinemaClipCurveWrapper2 != null)
									{
										CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper2));
									}
									haveCurvesChanged = true;
								}
								break;
							}
						}
						if (k < cinemaAnimationCurveWrapper.KeyframeCount - 1 && !cinemaAnimationCurveWrapper.IsRightLinear(k) && !cinemaAnimationCurveWrapper.IsRightConstant(k))
						{
							Vector2 vector3 = new Vector2(keyframeWrapper.OutTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.OutTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
							vector3.Normalize();
							vector3 *= 30f;
							Rect rect2 = new Rect(keyframeWrapper.ScreenPosition.x + vector3.x - 4f, keyframeWrapper.ScreenPosition.y + vector3.y - 4f, 8f, 8f);
							int controlID2 = GUIUtility.GetControlID("TangentOut".GetHashCode(), (FocusType)2);
							switch (Event.current.GetTypeForControl(controlID2))
							{
							case EventType.MouseDown:
								if (rect2.Contains(Event.current.mousePosition))
								{
									GUIUtility.hotControl=(controlID2);
									Event.current.Use();
								}
								break;
							case EventType.MouseUp:
								if (GUIUtility.hotControl == controlID2)
								{
									GUIUtility.hotControl=(0);
									if (CurvesChanged != null)
									{
										CinemaClipCurveWrapper cinemaClipCurveWrapper3 = Wrapper as CinemaClipCurveWrapper;
										if (cinemaClipCurveWrapper3 != null)
										{
											CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper3));
										}
										haveCurvesChanged = true;
									}
								}
								break;
							case EventType.MouseDrag:
								if (GUIUtility.hotControl == controlID2)
								{
									Vector2 vector4 = new Vector2((Event.current.mousePosition.x - state.Translation.x) / state.Scale.x, (curveTrackSafeArea.y + curveTrackSafeArea.height - Event.current.mousePosition.y) / curveTrackSafeArea.height * verticalRange + viewingSpace.y);
									Vector2 vector5 = new Vector2(keyframe.time, keyframe.value) - vector4;
									float num3 = vector5.y / vector5.x;
									float num4 = keyframe.inTangent;
									if (cinemaAnimationCurveWrapper.IsFreeSmooth(k))
									{
										num4 = num3;
									}
									Keyframe kf2 = new Keyframe(keyframe.time, keyframe.value, num4, num3);
									kf2.tangentMode=(keyframe.tangentMode);
									cinemaAnimationCurveWrapper.MoveKey(k, kf2);
									CinemaClipCurveWrapper cinemaClipCurveWrapper4 = Wrapper as CinemaClipCurveWrapper;
									if (cinemaClipCurveWrapper4 != null)
									{
										CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper4));
									}
									haveCurvesChanged = true;
								}
								break;
							}
						}
					}
				}
			}
		}
	}
	private void handleCurveCanvasInput(CinemaClipCurveWrapper clipCurveWrapper, DirectorControlState state)
	{
		int controlID = GUIUtility.GetControlID("CurveCanvas".GetHashCode(), FocusType.Passive);
        Event evt = Event.current;
	    Vector2 mousePos = evt.mousePosition;
	    switch (Event.current.GetTypeForControl(controlID))
	    {
            case EventType.MouseDown:
                if (controlPosition.Contains(Event.current.mousePosition))
                {
                    if (curveCanvasPosition.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 1)
                        {
                            float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                            CurvesContext context = new CurvesContext(clipCurveWrapper, time, state);
                            showCurveCanvasContextMenu(context);
                            Event.current.Use();
                        }
                    }
                }
                if (Event.current.button == 0)
                {
                    m_ValidRect = false;
                    m_StartPoint = Event.current.mousePosition;
                    evt.Use();
                }
                break;
            case EventType.Repaint:
                if (m_ValidRect)
                    DirectorControl.DirectorControlStyles.BoxSelect.Draw(GetCurrentPixelRect(), GUIContent.none, false, false, false, false);
                break;
            case EventType.MouseUp:
                if (Event.current.button == 0 && m_ValidRect)
                {
                    List<CinemaKeyframeWrapper> toBeUnselected = new List<CinemaKeyframeWrapper>();
                    List<CinemaKeyframeWrapper> toBeSelected = new List<CinemaKeyframeWrapper>();
                    m_EndPoint = Event.current.mousePosition;
                    Rect rect = GetCurrentTimeRect();
                    foreach (var memberCurve in clipCurveWrapper.MemberCurves)
                    {
                        foreach (var animationCurve in memberCurve.AnimationCurves)
                        {
                            if (!animationCurve.IsVisible)
                                continue;
                            for (int i = 0; i < animationCurve.KeyframeCount; i++)
                            {
                                Vector2 position = animationCurve.GetKeyframeScreenPosition(i);
                                if (rect.Contains(position))
                                {
                                    CinemaKeyframeWrapper keyframe = animationCurve.GetKeyframeWrapper(i);
                                    if (!toBeSelected.Contains(keyframe) && !toBeUnselected.Contains(keyframe))
                                    {
                                        if (!KeyIsSelected(keyframe.GetHash()))
                                            toBeSelected.Add(keyframe);
                                        else
                                            toBeUnselected.Add(keyframe);
                                    }
                                }
                            }
                        }
                    }
                    if (toBeSelected.Count == 0)
                        foreach (var keyframe in toBeUnselected)
                        {
                            UnselectKey(keyframe);
                        }

                    foreach (var keyframe in toBeSelected)
                    {
                        SelectKey(keyframe);
                    }
                }
                else
                {
                    ClearKeySelections();
                }
                evt.Use();
                break;
            case EventType.MouseDrag:
                m_ValidRect = Mathf.Abs((mousePos - m_StartPoint).x) > 1f;

                if (m_ValidRect)
                    m_EndPoint = new Vector2(mousePos.x, mousePos.y);
                evt.Use();
                break;
	    }
	}
	private void handleMasterKeysInput(CinemaClipCurveWrapper clipCurveWrapper, DirectorControlState state)
	{
		bool flag = false;
		for (int i = 0; i < keyframeTimes.Count; i++)
		{
			float num = keyframeTimes.Keys[i];
			float num2 = num * state.Scale.x + state.Translation.x;
			Rect rect = new Rect(num2 - 4f, masterKeysPosition.y + 4f, 8f, 8f);
			int num3 = keyframeTimes.Values[i];
			if (flag)
			{
				break;
			}
			switch (Event.current.GetTypeForControl(num3))
			{
			case EventType.MouseDown:
				if (rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
				{
					GUIUtility.hotControl = num3;
					selection.Reset();
					Event.current.Use();
				}
				else if (rect.Contains(Event.current.mousePosition) && Event.current.button == 1)
				{
					float time = num;
					CurvesContext context = new CurvesContext(clipCurveWrapper, time, state);
					showMasterKeyContextMenu(context);
					Event.current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == num3)
				{
					GUIUtility.hotControl=(0);
					if (CurvesChanged != null)
					{
						CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
						if (cinemaClipCurveWrapper != null)
						{
							CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
						}
					}
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == num3 && Event.current.button == 0)
				{
					Undo.RecordObject(Wrapper.Behaviour, "Moved Keyframes");
					float num4 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
					num4 = Mathf.Clamp(num4, clipCurveWrapper.Firetime, clipCurveWrapper.Firetime + clipCurveWrapper.Duration);
					bool flag3 = false;
                    int index = Mathf.RoundToInt(num4 * DirectorWindow.directorControl.frameRate);
                    num4 = (float)index / DirectorWindow.directorControl.frameRate;
					if (Event.current.delta.x == 0f)
					{
						flag3 = true;
					}
					else if (num4 < keyframeTimes.Keys[i])
					{
                        if (i > 0 && Math.Abs(num4 - keyframeTimes.Keys[i - 1]) < 0.01f)
                        {
                            flag3 = true;
                        }
                        else if (i > 0 && num4 < keyframeTimes.Keys[i - 1])
                        {
                            GUIUtility.hotControl = keyframeTimes.Values[i - 1];
                        }
                    }
                    else
                    {
                        if (i < keyframeTimes.Keys.Count - 1 && Math.Abs(num4 - keyframeTimes.Keys[i + 1]) < 0.01f)
                        {
                            flag3 = true;
                        }
                        else if (i < keyframeTimes.Keys.Count - 1 && num4 > keyframeTimes.Keys[i + 1])
                        {
                            GUIUtility.hotControl = keyframeTimes.Values[i + 1];
                        }
                    }
					if (!flag3 && num4 != num)
					{
					    float deltaTime = num4 - num;
						CinemaMemberCurveWrapper[] memberCurves = clipCurveWrapper.MemberCurves;
						for (int j = 0; j < memberCurves.Length; j++)
						{
							CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[j];
    						if (cinemaMemberCurveWrapper.IsVisible)
							{
								CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
								for (int k = 0; k < animationCurves.Length; k++)
								{
									CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[k];
									if (cinemaAnimationCurveWrapper.IsVisible)
									{
                                        bool change = false;
                                        if (deltaTime >= 0)
                                        {
                                            for (int l = cinemaAnimationCurveWrapper.KeyframeCount - 1; l >= 0; l--)
                                            {
                                                Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(l);
                                                if (KeyIsSelected(cinemaAnimationCurveWrapper.GetKeyframeWrapper(l).GetHash()))
                                                {
                                                    cinemaAnimationCurveWrapper.GetKeyframeWrapper(l);
                                                    Keyframe kf = new Keyframe(keyframe.time + deltaTime, keyframe.value, keyframe.inTangent, keyframe.outTangent);
                                                    kf.tangentMode = keyframe.tangentMode;
                                                    cinemaAnimationCurveWrapper.MoveKey(l, kf);
                                                    flag = true;
                                                    change = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int l = 0; l < cinemaAnimationCurveWrapper.KeyframeCount; l++)
                                            {
                                                Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(l);
                                                if (KeyIsSelected(cinemaAnimationCurveWrapper.GetKeyframeWrapper(l).GetHash()))
                                                {
                                                    cinemaAnimationCurveWrapper.GetKeyframeWrapper(l);
                                                    Keyframe kf = new Keyframe(keyframe.time + deltaTime, keyframe.value, keyframe.inTangent, keyframe.outTangent);
                                                    kf.tangentMode = keyframe.tangentMode;
                                                    cinemaAnimationCurveWrapper.MoveKey(l, kf);
                                                    flag = true;
                                                    change = true;
                                                }
                                            }
                                        }
                                        if (change)
                                        {
                                            CinemaClipCurveWrapper cinemaClipCurveWrapper2 = Wrapper as CinemaClipCurveWrapper;
                                            if (cinemaClipCurveWrapper2 != null)
                                            {
                                                CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper2));
                                            }
                                            haveCurvesChanged = true;
                                        }
                                    }
								}
							}
						}
					}
				}
				break;
			}
		}
	}
	private void handleItemInput(CinemaClipCurveWrapper clipCurveWrapper, DirectorControlState state, Rect trackPosition)
	{
		if (isRenaming)
		{
			return;
		}
		float num = clipCurveWrapper.Firetime * state.Scale.x + state.Translation.x;
		float num2 = (clipCurveWrapper.Firetime + clipCurveWrapper.Duration) * state.Scale.x + state.Translation.x;
		Rect rect = new Rect(num, 0f, 5f, timelineItemPosition.height);
		Rect rect2 = new Rect(num + 5f, 0f, num2 - num - 10f, timelineItemPosition.height);
		Rect rect3 = new Rect(num2 - 5f, 0f, 5f, timelineItemPosition.height);
		EditorGUIUtility.AddCursorRect(rect, (MouseCursor)3);
		EditorGUIUtility.AddCursorRect(rect2, (MouseCursor)5);
		EditorGUIUtility.AddCursorRect(rect3, (MouseCursor)3);
		this.controlID = GUIUtility.GetControlID(Wrapper.Behaviour.GetInstanceID(), FocusType.Passive, timelineItemPosition);
		int controlID = GUIUtility.GetControlID(Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect);
		int controlID2 = GUIUtility.GetControlID(Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect2);
		int controlID3 = GUIUtility.GetControlID(Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect3);
		if (Event.current.GetTypeForControl(this.controlID) == EventType.MouseDown && rect2.Contains(Event.current.mousePosition) && Event.current.button == 1)
		{
			if (!IsSelected)
			{
				GameObject[] gameObjects = Selection.gameObjects;
				ArrayUtility.Add(ref gameObjects, Wrapper.Behaviour.gameObject);
				Selection.objects = gameObjects;
				hasSelectionChanged = true;
			}
			showContextMenu(Wrapper.Behaviour);
			Event.current.Use();
		}
		switch (Event.current.GetTypeForControl(controlID2))
		{
		case EventType.MouseDown:
			if (rect2.Contains(Event.current.mousePosition) && Event.current.button == 0)
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
				else if (Event.current.clickCount >= 2)
				{
					CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
					if (cinemaClipCurveWrapper != null && this.RequestEdit != null)
					{
						this.RequestEdit(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
					}
				}
				else if (!IsSelected)
				{
					Selection.activeInstanceID=(base.Behaviour.GetInstanceID());
				}
				mouseDragActivity = false;
				mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - clipCurveWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case EventType.MouseUp:
			if (GUIUtility.hotControl == controlID2)
			{
				mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
				if (!mouseDragActivity)
				{
					if (Event.current.control)
					{
						if (!this.hasSelectionChanged)
						{
							if (base.IsSelected)
							{
								GameObject[] gameObjects4 = Selection.gameObjects;
								ArrayUtility.Remove(ref gameObjects4, base.Wrapper.Behaviour.gameObject);
								Selection.objects=(gameObjects4);
							}
							else
							{
								GameObject[] gameObjects5 = Selection.gameObjects;
								ArrayUtility.Add(ref gameObjects5, base.Wrapper.Behaviour.gameObject);
								Selection.objects=(gameObjects5);
							}
						}
					}
					else
					{
						Selection.activeInstanceID= Behaviour.GetInstanceID();
					}
				}
				else
				{
					base.TriggerTrackItemUpdateEvent();
				}
				this.hasSelectionChanged = false;
			}
			break;
		case EventType.MouseDrag:
			if (GUIUtility.hotControl == controlID2 && !this.hasSelectionChanged)
			{
				Undo.RecordObject(base.Behaviour, string.Format("Changed {0}", base.Behaviour.name));
				float num3 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num3 = state.SnappedTime(num3 - mouseDownOffset);
				if (!this.mouseDragActivity)
				{
					this.mouseDragActivity = (base.Wrapper.Firetime != num3);
				}
				TriggerRequestTrackItemTranslate(num3);
				base.TriggerTrackItemUpdateEvent();
			}
			break;
		}
		switch (Event.current.GetTypeForControl(controlID))
		{
		case EventType.MouseDown:
			if (rect.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID);
				mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - clipCurveWrapper.Firetime;
				Event.current.Use();
			}
			break;
		case EventType.MouseUp:
			if (GUIUtility.hotControl == controlID)
			{
				mouseDownOffset = -1f;
				GUIUtility.hotControl = 0;
				CinemaClipCurveWrapper cinemaClipCurveWrapper2 = Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper2 != null)
				{
					CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper2));
				}
				this.haveCurvesChanged = true;
			}
			break;
		case EventType.MouseDrag:
			if (GUIUtility.hotControl == controlID)
			{
				float num4 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num4 = state.SnappedTime(num4);
				float num5 = 0f;
				float num6 = clipCurveWrapper.Firetime + clipCurveWrapper.Duration;
				using (IEnumerator<TimelineItemWrapper> enumerator = base.Track.Items.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						CinemaActionWrapper cinemaActionWrapper = enumerator.Current as CinemaActionWrapper;
						if (cinemaActionWrapper != null && cinemaActionWrapper.Behaviour != base.Wrapper.Behaviour)
						{
							float num7 = cinemaActionWrapper.Firetime + cinemaActionWrapper.Duration;
							if (num7 <= base.Wrapper.Firetime)
							{
								num5 = Mathf.Max(num5, num7);
							}
						}
					}
				}
				num4 = Mathf.Max(num5, num4);
				num4 = Mathf.Min(num6, num4);
				if (state.ResizeOption == ResizeOption.Crop)
				{
					clipCurveWrapper.CropFiretime(num4);
				}
				else if (state.ResizeOption == ResizeOption.Scale)
				{
					clipCurveWrapper.ScaleFiretime(num4);
				}
				CinemaClipCurveWrapper cinemaClipCurveWrapper3 = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper3 != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper3));
				}
				this.haveCurvesChanged = true;
			}
			break;
		}
		switch (Event.current.GetTypeForControl(controlID3))
		{
		case EventType.MouseDown:
			if (rect3.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID3);
				mouseDownOffset = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x - base.Wrapper.Firetime;
				Event.current.Use();
			}
			break;
		case EventType.MouseUp:
			if (GUIUtility.hotControl == controlID3)
			{
				mouseDownOffset = -1f;
				GUIUtility.hotControl=(0);
				CinemaClipCurveWrapper cinemaClipCurveWrapper4 = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper4 != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper4));
				}
				this.haveCurvesChanged = true;
			}
			break;
		case EventType.MouseDrag:
			if (GUIUtility.hotControl == controlID3)
			{
				float num8 = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
				num8 = state.SnappedTime(num8);
				float num9 = float.PositiveInfinity;
				using (IEnumerator<TimelineItemWrapper> enumerator = base.Track.Items.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						CinemaActionWrapper cinemaActionWrapper2 = enumerator.Current as CinemaActionWrapper;
						if (cinemaActionWrapper2 != null && cinemaActionWrapper2.Behaviour != base.Wrapper.Behaviour)
						{
							float num10 = clipCurveWrapper.Firetime + clipCurveWrapper.Duration;
							if (cinemaActionWrapper2.Firetime >= num10)
							{
								num9 = Mathf.Min(num9, cinemaActionWrapper2.Firetime);
							}
						}
					}
				}
				num8 = Mathf.Clamp(num8, Wrapper.Firetime, num9);
				if (state.ResizeOption == ResizeOption.Crop)
				{
					clipCurveWrapper.CropDuration(num8 - base.Wrapper.Firetime);
				}
				else if (state.ResizeOption == ResizeOption.Scale)
				{
					clipCurveWrapper.ScaleDuration(num8 - base.Wrapper.Firetime);
				}
				CinemaClipCurveWrapper cinemaClipCurveWrapper5 = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper5 != null)
				{
					CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper5));
				}
				haveCurvesChanged = true;
			}
			break;
		}
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == (KeyCode)127 && Selection.activeGameObject == base.Wrapper.Behaviour.gameObject)
		{
			deleteItem(Wrapper.Behaviour);
			Event.current.Use();
		}
	}
	internal override void Translate(float amount)
	{
		CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
		if (cinemaClipCurveWrapper == null)
		{
			return;
		}
		cinemaClipCurveWrapper.TranslateCurves(amount);
		this.haveCurvesChanged = true;
	}
	internal override void ConfirmTranslate()
	{
		CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
		if (cinemaClipCurveWrapper == null)
		{
			return;
		}
		if (this.TranslateCurveClipItem != null)
		{
			this.TranslateCurveClipItem(this, new CurveClipItemEventArgs(cinemaClipCurveWrapper.Behaviour, cinemaClipCurveWrapper.Firetime, cinemaClipCurveWrapper.Duration));
		}
		haveCurvesChanged = true;
	}
	public override void Draw(DirectorControlState state)
	{
		CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
		if (cinemaClipCurveWrapper == null)
		{
			return;
		}
		drawCurveItem();
		if (!this.isCurveClipEmpty && !this.isFolded)
		{
			drawCurveCanvas(state, cinemaClipCurveWrapper);
			if (IsEditing)
			{
				drawMasterKeys(state);
				handleCurveCanvasInput(cinemaClipCurveWrapper, state);
			}
		}
	}
	protected void drawCurveItem()
	{
		if (Wrapper.Behaviour == null)
		{
			return;
		}
		if (base.TrackControl.isExpanded)
		{
			if (base.IsSelected)
			{
				GUI.Box(timelineItemPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
			}
			else
			{
				GUI.Box(this.timelineItemPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemStyle);
			}
		}
		else if (base.IsSelected)
		{
			GUI.Box(this.timelineItemPosition, GUIContent.none, TimelineTrackControl.styles.CurveTrackItemSelectedStyle);
		}
		else
		{
			GUI.Box(this.timelineItemPosition, GUIContent.none, TimelineTrackControl.styles.CurveTrackItemStyle);
		}
		Color arg_100_0 = GUI.color;
		GUI.color=(new Color(0.71f, 0.14f, 0.8f));
		Rect rect = timelineItemPosition;
		rect.x=(rect.x + 4f);
		rect.width= 16;
		rect.height= 16;
		GUI.Box(rect, this.actionIcon, GUIStyle.none);
		GUI.color=(arg_100_0);
		Rect controlPosition = this.controlPosition;
		controlPosition.x=(rect.xMax);
		controlPosition.width=(controlPosition.width - (rect.width + 4f));
		controlPosition.height=(17f);
		base.DrawRenameLabel(Wrapper.Behaviour.name, controlPosition);
	}
	protected void drawMasterKeys(DirectorControlState state)
	{
		for (int i = 0; i < keyframeTimes.Count; i++)
		{
			float num = keyframeTimes.Keys[i] * state.Scale.x + state.Translation.x;
			Rect arg_A8_0 = new Rect(num - 6f, masterKeysPosition.y + 4f, 12f, 12f);
			int num2 = keyframeTimes.Values[i];
			Color color = GUI.color;
		    if (GUIUtility.hotControl == num2 || KeyIsSelected(keyframeTimes.Values[i]))
		    {
		        GUI.color = new Color(0.5f, 0.6f, 0.905f, 1f);
		    }
		    else
		    {
		        GUI.color = Color.red;
		    }
			GUI.Label(arg_A8_0, string.Empty, TimelineTrackControl.styles.keyframeStyle);
			GUI.color = color;
		}
	}

    protected void drawOnlyCurveCanvas(DirectorControlState state, CinemaMemberCurveWrapper cinemaMemberCurveWrapper, CinemaClipCurveWrapper clipCurveWrapper)
    {
        CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
        for (int j = 0; j < animationCurves.Length; j++)
        {
            CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
            if (cinemaMemberCurveWrapper.IsVisible && cinemaAnimationCurveWrapper.IsVisible)
            {
                drawCurve(curveTrackSafeArea, cinemaAnimationCurveWrapper, state, viewingSpace);
                if (IsEditing)
                {
                    drawKeyframes(curveTrackSafeArea, cinemaMemberCurveWrapper, cinemaAnimationCurveWrapper, state, viewingSpace, clipCurveWrapper);
                }
            }
        }
    }
	protected void drawCurveCanvas(DirectorControlState state, CinemaClipCurveWrapper clipCurveWrapper)
	{
		GUI.Box(curveCanvasPosition, GUIContent.none, TimelineTrackControl.styles.curveCanvasStyle);
		CinemaMemberCurveWrapper[] memberCurves = clipCurveWrapper.MemberCurves;
	    var onlySelfMember = GetOnlySelfMemberCurveWrapper(memberCurves);
	    if (onlySelfMember != null)
	    {
	        drawOnlyCurveCanvas(state, onlySelfMember, clipCurveWrapper);
            return;
	    }
        for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
		    drawOnlyCurveCanvas(state, cinemaMemberCurveWrapper, clipCurveWrapper);
        }
	}
	protected void drawCurve(Rect position, CinemaAnimationCurveWrapper wrapper, DirectorControlState state, Rect view)
	{
		float arg_07_0 = view.height;
		float arg_0F_0 = view.y;
		for (int i = 0; i < wrapper.KeyframeCount - 1; i++)
		{
			CinemaKeyframeWrapper keyframeWrapper = wrapper.GetKeyframeWrapper(i);
			CinemaKeyframeWrapper keyframeWrapper2 = wrapper.GetKeyframeWrapper(i + 1);
			if (float.IsPositiveInfinity(wrapper.GetKeyframe(i).outTangent) || float.IsPositiveInfinity(wrapper.GetKeyframe(i + 1).inTangent))
			{
				Handles.DrawBezier(new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), wrapper.Color, null, 4f);
				Handles.DrawBezier(new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper2.ScreenPosition.y), new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper2.ScreenPosition.y), new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), wrapper.Color, null, 4f);
			}
			else
			{
				Handles.DrawBezier(new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y), new Vector3(keyframeWrapper2.ScreenPosition.x, keyframeWrapper2.ScreenPosition.y), new Vector3(keyframeWrapper.OutTangentControlPointPosition.x, keyframeWrapper.OutTangentControlPointPosition.y), new Vector3(keyframeWrapper2.InTangentControlPointPosition.x, keyframeWrapper2.InTangentControlPointPosition.y), wrapper.Color, null, 4f);
			}
		}
	}
	protected void drawKeyframes(Rect position, CinemaMemberCurveWrapper memberWrapper, CinemaAnimationCurveWrapper wrapper, DirectorControlState state, Rect view, CinemaClipCurveWrapper clipCurveWrapper)
	{
		for (int i = 0; i < wrapper.KeyframeCount; i++)
		{
			CinemaKeyframeWrapper keyframeWrapper = wrapper.GetKeyframeWrapper(i);
			bool arg_BD_0 = KeyIsSelected(wrapper.GetKeyframeWrapper(i).GetHash());
			Color color = GUI.color;
			if (arg_BD_0)
			{
				if (i > 0 && !wrapper.IsAuto(i) && !wrapper.IsLeftLinear(i) && !wrapper.IsLeftConstant(i))
				{
					Vector2 vector = new Vector2(keyframeWrapper.InTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.InTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
					vector.Normalize();
					vector *= 30f;
					Handles.color= Color.gray;
					Handles.DrawLine(new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y, 0f), new Vector3(keyframeWrapper.ScreenPosition.x + vector.x, keyframeWrapper.ScreenPosition.y + vector.y, 0f));
					GUI.Label(new Rect(keyframeWrapper.ScreenPosition.x + vector.x - 4f, keyframeWrapper.ScreenPosition.y + vector.y - 4f, 8f, 8f), string.Empty, TimelineTrackControl.styles.tangentStyle);
				}
				if (i < wrapper.KeyframeCount - 1 && !wrapper.IsAuto(i) && !wrapper.IsRightLinear(i) && !wrapper.IsRightConstant(i))
				{
					Vector2 vector2 = new Vector2(keyframeWrapper.OutTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.OutTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
					vector2.Normalize();
					vector2 *= 30f;
					Handles.color = Color.gray;
					Handles.DrawLine(new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y, 0f), new Vector3(keyframeWrapper.ScreenPosition.x + vector2.x, keyframeWrapper.ScreenPosition.y + vector2.y, 0f));
					GUI.Label(new Rect(keyframeWrapper.ScreenPosition.x + vector2.x - 5f, keyframeWrapper.ScreenPosition.y + vector2.y - 5f, 10f, 10f), string.Empty, TimelineTrackControl.styles.tangentStyle);
				}
				GUI.color= wrapper.Color;
			}
			else
			{
			    GUI.color = Color.black;
            }
			GUI.Label(new Rect(keyframeWrapper.ScreenPosition.x - 6f, keyframeWrapper.ScreenPosition.y - 4f, 12f, 12f), string.Empty, TimelineTrackControl.styles.keyframeStyle);
			GUI.color = color;
		}
	}
	private Rect getViewingArea(bool isResizeEnabled, CinemaClipCurveWrapper clipCurveWrapper)
	{
		Rect result = new Rect(3.40282347E+38f, 3.40282347E+38f, -3.40282347E+38f, -3.40282347E+38f);
		CinemaMemberCurveWrapper[] memberCurves = clipCurveWrapper.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
			if (cinemaMemberCurveWrapper.IsVisible)
			{
				CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
				for (int j = 0; j < animationCurves.Length; j++)
				{
					CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
					if (cinemaAnimationCurveWrapper.IsVisible)
					{
						for (int k = 0; k < cinemaAnimationCurveWrapper.KeyframeCount; k++)
						{
							Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
							result.x=(Mathf.Min(result.x, keyframe.time));
							result.width=(Mathf.Max(result.width, keyframe.time));
							result.y=(Mathf.Min(result.y, keyframe.value));
							result.height=(Mathf.Max(result.height, keyframe.value));
							if (k > 0)
							{
								Keyframe keyframe2 = cinemaAnimationCurveWrapper.GetKeyframe(k - 1);
								float num = Mathf.Abs(keyframe.time - keyframe2.time) * 0.333333f;
								float num2 = cinemaAnimationCurveWrapper.Evaluate(keyframe2.time + num);
								float num3 = cinemaAnimationCurveWrapper.Evaluate(keyframe.time - num);
								float num4 = cinemaAnimationCurveWrapper.Evaluate(keyframe2.time + Mathf.Abs(keyframe.time - keyframe2.time) * 0.5f);
								result.y=(Mathf.Min(new float[]
								{
									result.y,
									num2,
									num3,
									num4
								}));
								result.height=(Mathf.Max(new float[]
								{
									result.height,
									num2,
									num3,
									num4
								}));
							}
						}
					}
				}
			}
		}
		if (result.height - result.y == 0f)
		{
			result.y=(result.y + 0f);
			result.height=(result.y + 1f);
		}
		if (!isResizeEnabled)
		{
			result.y = viewingSpace.y;
			result.height = viewingSpace.height;
		}
		return result;
	}
	private void showMasterKeyContextMenu(CurvesContext context)
	{
		GenericMenu genericMenu = new GenericMenu();
		genericMenu.AddItem(new GUIContent("Snap scrubber"), false, snapScrubber, context);
		genericMenu.AddItem(new GUIContent("删除帧"), false, deleteKeyframes, context);
	    genericMenu.AddItem(new GUIContent("复制"), false, copyKeyframes, context);
	    genericMenu.AddItem(new GUIContent("粘贴"), false, pasteKeyframes, context);
        genericMenu.ShowAsContext();
	}
	private void snapScrubber(object userData)
	{
		CurvesContext curvesContext = userData as CurvesContext;
		if (curvesContext != null && SnapScrubber != null)
		{
			SnapScrubber(this, new CurveClipScrubberEventArgs(Wrapper.Behaviour, curvesContext.time));
			curvesContext.state.IsInPreviewMode = true;
			curvesContext.state.ScrubberPosition = curvesContext.time;
		}
	}
	private void showCurveCanvasContextMenu(CurvesContext context)
	{
		GenericMenu expr_05 = new GenericMenu();
		expr_05.AddItem(new GUIContent("Add Keyframe"), false, addKeyframes, context);
		expr_05.ShowAsContext();
	}
	private void addKeyframes(object userData)
	{
		CurvesContext curvesContext = userData as CurvesContext;
		if (curvesContext != null)
		{
			CinemaMemberCurveWrapper[] memberCurves = curvesContext.wrapper.MemberCurves;
			for (int i = 0; i < memberCurves.Length; i++)
			{
				CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
				for (int j = 0; j < animationCurves.Length; j++)
				{
					CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
					cinemaAnimationCurveWrapper.AddKey(curvesContext.time, cinemaAnimationCurveWrapper.Evaluate(curvesContext.time), curvesContext.state.DefaultTangentMode);
				}
			}
			if (CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			haveCurvesChanged = true;
		}
	}
	private void deleteKey(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null)
		{
			keyframeContext.curveWrapper.RemoveKey(keyframeContext.key);
			if (CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			selection.CurveId = -1;
			selection.KeyId = -1;
		}
    }
	private void deleteKeyframes(object userData)
	{
		CurvesContext curvesContext = userData as CurvesContext;
		if (curvesContext != null)
		{
			CinemaMemberCurveWrapper[] memberCurves = curvesContext.wrapper.MemberCurves;
			for (int i = 0; i < memberCurves.Length; i++)
			{
				CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
				for (int j = 0; j < animationCurves.Length; j++)
				{
                    bool mark = false;
                    for (int l = animationCurves[j].KeyframeCount - 1; l >= 0; l--)
                    {
                        Keyframe keyframe = animationCurves[j].GetKeyframe(l);
                        if (KeyIsSelected(animationCurves[j].GetKeyframeWrapper(l).GetHash()))
                        {
                            animationCurves[j].curve.RemoveKey(l);
                            mark = true;
                        }
                    }
                    if (mark)
                    {
                        animationCurves[j].initializeKeyframeWrappers();
                    }
                }
			}
            if (CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			haveCurvesChanged = true;
		}
	}
    private void copyKeyframes(object userData)
    {
        CurvesContext curvesContext = userData as CurvesContext;
        if (curvesContext != null)
        {
            DirectorCopyPaste.time = curvesContext.time;
        }
    }
    private void pasteKeyframes(object userData)
    {
        CurvesContext curvesContext = userData as CurvesContext;
        if (curvesContext != null)
        {
            CinemaMemberCurveWrapper[] memberCurves = curvesContext.wrapper.MemberCurves;
            for (int i = 0; i < memberCurves.Length; i++)
            {
                CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
                for (int j = 0; j < animationCurves.Length; j++)
                {
                    animationCurves[j].PauseAtTime(DirectorCopyPaste.time, curvesContext.time);
                }
            }
            if (CurvesChanged != null)
            {
                CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
                if (cinemaClipCurveWrapper != null)
                {
                    CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
                }
            }
            haveCurvesChanged = true;
        }
    }
    public class CurveMenuInfo
    {
        public CinemaMemberCurveWrapper[] memberCurves;
        public CinemaMemberCurveWrapper cinemaMemberCurveWrapper;
    }
    public class CurveAnimationMemnInfo
    {
        public CinemaMemberCurveWrapper[] memberCurves;
        public CinemaMemberCurveWrapper cinemaMemberCurveWrapper;
        public CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper;
    }
    internal void UpdateHeaderArea(DirectorControlState state, Rect controlHeaderArea)
	{
		CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
		if (cinemaClipCurveWrapper == null)
		{
			return;
		}
		Rect rect = new Rect(controlHeaderArea.x, controlHeaderArea.y, controlHeaderArea.width, controlHeaderArea.height - 17f);
		Rect position = new Rect(controlHeaderArea.x, controlHeaderArea.y + controlHeaderArea.height - 17f, controlHeaderArea.width, 17f);
		GUILayout.BeginArea(rect);
		Rect rect2 = new Rect(controlHeaderArea.width - 15f, 0f, 15f, rect.height);
		scrollBarPosition = GUI.VerticalScrollbar(rect2, scrollBarPosition, Mathf.Min(rect.height, cinemaClipCurveWrapper.RowCount * 17f), 0f, cinemaClipCurveWrapper.RowCount * 17f);
		float num = controlHeaderArea.width - 12f - rect2.width;
		int num2 = 0;
		CinemaMemberCurveWrapper[] memberCurves = cinemaClipCurveWrapper.MemberCurves;
        for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
			Rect rect3 = new Rect(0f, 17f * num2 - scrollBarPosition, num * 0.66f, 17f);
			Rect rect4 = new Rect(num * 0.8f + 4f, 17f * num2 - scrollBarPosition, 32f, 16f);
			string userFriendlyName = DirectorControlHelper.GetUserFriendlyName(cinemaMemberCurveWrapper.Type, cinemaMemberCurveWrapper.PropertyName);
			string text = (userFriendlyName == string.Empty) ? cinemaMemberCurveWrapper.Type : string.Format("{0}.{1}", cinemaMemberCurveWrapper.Type, userFriendlyName);
			cinemaMemberCurveWrapper.IsFoldedOut = EditorGUI.Foldout(rect3, cinemaMemberCurveWrapper.IsFoldedOut, new GUIContent(text, cinemaMemberCurveWrapper.Texture));
			GUI.Box(rect4, string.Empty, TimelineTrackControl.styles.keyframeContextStyle);
			int controlID = GUIUtility.GetControlID(Wrapper.Behaviour.GetInstanceID(), FocusType.Passive, rect4);
		    num2++;
            if (cinemaMemberCurveWrapper.IsFoldedOut)
            {
                CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
                for (int j = 0; j < animationCurves.Length; j++)
                {
                    CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
                    Rect rect5 = new Rect(12f, 17f * num2 - scrollBarPosition, num * 0.5f, 17f);
                    Rect rect6 = new Rect(rect5.x + rect5.width, 17f * num2 - scrollBarPosition, num * 0.3f, 17f);
                    Rect rect7 = new Rect(rect6.x + rect6.width + 4f, 17f * num2 - scrollBarPosition, 32f, 16f);
                    string text2 = (userFriendlyName == string.Empty) ? cinemaAnimationCurveWrapper.Label : string.Format("{0}.{1}", userFriendlyName, cinemaAnimationCurveWrapper.Label);
                    EditorGUI.LabelField(rect5, text2);
                    float num3 = cinemaAnimationCurveWrapper.Evaluate(state.ScrubberPosition);
                    GUIStyle toolbarTextField = EditorStyles.toolbarTextField;
                    float num4 = EditorGUI.FloatField(rect6, num3, toolbarTextField);
                    if (num4 != num3 && state.ScrubberPosition >= cinemaClipCurveWrapper.Firetime && state.ScrubberPosition <= cinemaClipCurveWrapper.Firetime + cinemaClipCurveWrapper.Duration)
                    {
                        updateOrAddKeyframe(cinemaAnimationCurveWrapper, state, num4);
                        if (CurvesChanged != null)
                        {
                            CinemaClipCurveWrapper cinemaClipCurveWrapper2 = Wrapper as CinemaClipCurveWrapper;
                            if (cinemaClipCurveWrapper2 != null)
                            {
                                CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper2));
                            }
                        }
                        haveCurvesChanged = true;
                    }
                    Color color = GUI.color;
                    GUI.color = (cinemaAnimationCurveWrapper.Color);
                    GUI.Box(rect7, string.Empty, TimelineTrackControl.styles.keyframeContextStyle);
                    int controlID2 = GUIUtility.GetControlID(Wrapper.Behaviour.GetInstanceID(), (FocusType)2, rect7);
                    if (Event.current.GetTypeForControl(controlID2) == EventType.MouseDown && rect7.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        GenericMenu genericMenu = new GenericMenu();
                        genericMenu.AddItem(new GUIContent("Visible"), cinemaAnimationCurveWrapper.IsVisible, toggleCurveVisibility, cinemaAnimationCurveWrapper);
                        CurveAnimationMemnInfo cami = new CurveAnimationMemnInfo
                        {
                            memberCurves = memberCurves,
                            cinemaMemberCurveWrapper = cinemaMemberCurveWrapper,
                            cinemaAnimationCurveWrapper = cinemaAnimationCurveWrapper,
                        };
                        genericMenu.AddItem(new GUIContent("仅显示自己"), cinemaAnimationCurveWrapper.onlySelf, toggleCurveVisibility2, cami);
                        if (state.ScrubberPosition >= cinemaClipCurveWrapper.Firetime && state.ScrubberPosition <= cinemaClipCurveWrapper.Firetime + cinemaClipCurveWrapper.Duration)
                        {
                            genericMenu.AddSeparator(string.Empty);
                            CurveContext curveContext = new CurveContext(cinemaAnimationCurveWrapper, state, state.ScrubberPosition);
                            genericMenu.AddItem(new GUIContent("Add Key"), false, addKeyToCurve, curveContext);
                        }
                        genericMenu.DropDown(new Rect(rect7.x, rect7.y + rect7.height, 0f, 0f));
                    }
                    GUI.color = color;
                    num2++;
                }
            }
            if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown && rect4.Contains(Event.current.mousePosition) && Event.current.button == 0)
			{
				GenericMenu expr_223 = new GenericMenu();
			    CurveMenuInfo cmi = new CurveMenuInfo
			    {
			        memberCurves = memberCurves,
			        cinemaMemberCurveWrapper = cinemaMemberCurveWrapper,
			    };
                expr_223.AddItem(new GUIContent("Visible"), cinemaMemberCurveWrapper.IsVisible, toggleMemberVisibility, cinemaMemberCurveWrapper);
			    expr_223.AddItem(new GUIContent("仅显示自己"), cinemaMemberCurveWrapper.onlySelf, toggleMemberVisibility2, cmi);
                expr_223.DropDown(new Rect(rect4.x, rect4.y + rect4.height, 0f, 0f));
			}
		}
		GUILayout.EndArea();
		updateFooter(position, memberCurves);
	}
	private void addKeyToCurve(object userData)
	{
		CurveContext curveContext = userData as CurveContext;
		if (curveContext != null)
		{
			curveContext.curveWrapper.AddKey(curveContext.time, curveContext.curveWrapper.Evaluate(curveContext.time), curveContext.state.DefaultTangentMode);
			if (CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			haveCurvesChanged = true;
		}
	}
	private void toggleMemberVisibility(object userData)
	{
		CinemaMemberCurveWrapper cinemaMemberCurveWrapper = userData as CinemaMemberCurveWrapper;
		if (cinemaMemberCurveWrapper != null)
		{
		    cinemaMemberCurveWrapper.IsVisible = !cinemaMemberCurveWrapper.IsVisible;
		}
	}
    private void toggleMemberVisibility2(object userData)
    {
        CurveMenuInfo cmi = userData as CurveMenuInfo;
        cmi.cinemaMemberCurveWrapper.onlySelf = !cmi.cinemaMemberCurveWrapper.onlySelf;
        CinemaMemberCurveWrapper expr_0B = cmi.cinemaMemberCurveWrapper;
        if (expr_0B.onlySelf)
        {
            for (int i = 0; i < cmi.memberCurves.Length; i++)
            {
                if (cmi.memberCurves[i] != expr_0B)
                {
                    cmi.memberCurves[i].onlySelf = false;
                    cmi.memberCurves[i].IsVisible = false;
                }
                else
                {
                    cmi.cinemaMemberCurveWrapper.IsVisible = true;
                    for (int j = 0; j < cmi.cinemaMemberCurveWrapper.AnimationCurves.Length; j++)
                    {
                        cmi.cinemaMemberCurveWrapper.AnimationCurves[j].IsVisible = true;
                        cmi.cinemaMemberCurveWrapper.AnimationCurves[j].onlySelf = false;
                    }
                }
            }
        }
        
    }

    private void toggleCurveVisibility(object userData)
    {
        CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = userData as CinemaAnimationCurveWrapper;
        if (cinemaAnimationCurveWrapper != null)
        {
            CinemaAnimationCurveWrapper expr_0B = cinemaAnimationCurveWrapper;
            expr_0B.IsVisible = !expr_0B.IsVisible;
        }
    }

    private void toggleCurveVisibility2(object userData)
    {
        CurveAnimationMemnInfo cami = userData as CurveAnimationMemnInfo;
        cami.cinemaAnimationCurveWrapper.onlySelf = !cami.cinemaAnimationCurveWrapper.onlySelf;
        if (cami.cinemaAnimationCurveWrapper.onlySelf)
        {
            for (int i = 0; i < cami.memberCurves.Length; i++)
            {
                if (cami.memberCurves[i] == cami.cinemaMemberCurveWrapper)
                {
                    cami.cinemaMemberCurveWrapper.onlySelf = true;
                    cami.cinemaMemberCurveWrapper.IsVisible = true;
                    for (int j = 0; j < cami.cinemaMemberCurveWrapper.AnimationCurves.Length; j++)
                    {
                        if (cami.cinemaMemberCurveWrapper.AnimationCurves[j] == cami.cinemaAnimationCurveWrapper)
                        {
                            cami.cinemaAnimationCurveWrapper.IsVisible = true;
                        }
                        else
                        {
                            cami.cinemaMemberCurveWrapper.AnimationCurves[j].onlySelf = false;
                            cami.cinemaMemberCurveWrapper.AnimationCurves[j].IsVisible = false;
                        }
                    }
                }
                else
                {
                    cami.memberCurves[i].onlySelf = false;
                    cami.memberCurves[i].IsVisible = false;
                }
            }
        }
    }

    private void ShowAllMemberCurve(CinemaMemberCurveWrapper[] memberCurves)
    {
        for (int i = 0; i < memberCurves.Length; i++)
        {
            memberCurves[i].onlySelf = false;
            memberCurves[i].IsVisible = true;
            for (int j = 0; j < memberCurves[i].AnimationCurves.Length; j++)
            {
                memberCurves[i].AnimationCurves[j].onlySelf = false;
                memberCurves[i].AnimationCurves[j].IsVisible = true;
            }
        }
    }
    private void updateOrAddKeyframe(CinemaAnimationCurveWrapper curveWrapper, DirectorControlState state, float newValue)
	{
		bool flag = false;
		for (int i = 0; i < curveWrapper.KeyframeCount; i++)
		{
			Keyframe keyframe = curveWrapper.GetKeyframe(i);
			if (keyframe.time == state.ScrubberPosition)
			{
				Keyframe kf = new Keyframe(keyframe.time, newValue, keyframe.inTangent, keyframe.outTangent);
				kf.tangentMode=(keyframe.tangentMode);
				curveWrapper.MoveKey(i, kf);
				flag = true;
			}
		}
		if (!flag)
		{
			curveWrapper.AddKey(state.ScrubberPosition, newValue, state.DefaultTangentMode);
		}
	}
	private void updateFooter(Rect position, CinemaMemberCurveWrapper[] memberCurveWrappers)
	{
		float num = position.width / 4f;
        Rect button = new Rect(0, position.y, num,position.height);
	    if (GUI.Button(button, "全部显示"))
	    {
            ShowAllMemberCurve(memberCurveWrappers);
	    }
		Rect rect = new Rect(position.x+num, position.y, num, position.height);
		Rect rect2 = new Rect(position.x + num*2f, position.y, num*2f, position.height);
		Rect arg_A6_0 = new Rect(position.x + num * 3f, position.y, num, position.height);
		isAutoResizeEnabled = GUI.Toggle(rect, isAutoResizeEnabled, "Auto Resize", EditorStyles.miniButton);
		float num2 = EditorGUI.FloatField(rect2, viewingSpace.y);
		float num3 = EditorGUI.FloatField(arg_A6_0, viewingSpace.height);
		if (num2 != viewingSpace.y || num3 != this.viewingSpace.height)
		{
			isAutoResizeEnabled = false;
			viewingSpace.y = num2;
			viewingSpace.height=(num3);
		}
	}
	private void showKeyframeContextMenu(CinemaAnimationCurveWrapper animationCurve, int i, bool isBookEnd)
	{
		GenericMenu genericMenu = new GenericMenu();
		CinemaKeyframeWrapper keyframeWrapper = animationCurve.GetKeyframeWrapper(i);
		KeyframeContext keyframeContext = new KeyframeContext(animationCurve, i, keyframeWrapper);
		Keyframe keyframe = animationCurve.GetKeyframe(i);
		if (!isBookEnd)
		{
			genericMenu.AddItem(new GUIContent("Delete Key"), false, deleteKey, keyframeContext);
			genericMenu.AddSeparator(string.Empty);
		}
		genericMenu.AddItem(new GUIContent("Auto"), animationCurve.IsAuto(i), (this.setKeyAuto), keyframeContext);
		genericMenu.AddItem(new GUIContent("Free Smooth"), animationCurve.IsFreeSmooth(i), new GenericMenu.MenuFunction2(this.setKeyFreeSmooth), keyframeContext);
		genericMenu.AddItem(new GUIContent("Flat"), animationCurve.IsFreeSmooth(i) && keyframe.inTangent == 0f && keyframe.outTangent == 0f, new GenericMenu.MenuFunction2(this.setKeyFlat), keyframeContext);
		genericMenu.AddItem(new GUIContent("Broken"), animationCurve.IsBroken(i), new GenericMenu.MenuFunction2(this.setKeyBroken), keyframeContext);
		genericMenu.AddSeparator(string.Empty);
		genericMenu.AddItem(new GUIContent("Left Tangent/Free"), animationCurve.IsLeftFree(i), new GenericMenu.MenuFunction2(this.setKeyLeftFree), keyframeContext);
		genericMenu.AddItem(new GUIContent("Left Tangent/Linear"), animationCurve.IsLeftLinear(i), new GenericMenu.MenuFunction2(this.setKeyLeftLinear), keyframeContext);
		genericMenu.AddItem(new GUIContent("Left Tangent/Constant"), animationCurve.IsLeftConstant(i), new GenericMenu.MenuFunction2(this.setKeyLeftConstant), keyframeContext);
		genericMenu.AddItem(new GUIContent("Right Tangent/Free"), animationCurve.IsRightFree(i), new GenericMenu.MenuFunction2(this.setKeyRightFree), keyframeContext);
		genericMenu.AddItem(new GUIContent("Right Tangent/Linear"), animationCurve.IsRightLinear(i), new GenericMenu.MenuFunction2(this.setKeyRightLinear), keyframeContext);
		genericMenu.AddItem(new GUIContent("Right Tangent/Constant"), animationCurve.IsRightConstant(i), new GenericMenu.MenuFunction2(this.setKeyRightConstant), keyframeContext);
		genericMenu.AddItem(new GUIContent("Both Tangents/Free"), animationCurve.IsLeftFree(i) && animationCurve.IsRightFree(i), new GenericMenu.MenuFunction2(this.setKeyBothFree), keyframeContext);
		genericMenu.AddItem(new GUIContent("Both Tangents/Linear"), animationCurve.IsLeftLinear(i) && animationCurve.IsRightLinear(i), new GenericMenu.MenuFunction2(this.setKeyBothLinear), keyframeContext);
		genericMenu.AddItem(new GUIContent("Both Tangents/Constant"), animationCurve.IsLeftConstant(i) && animationCurve.IsRightConstant(i), new GenericMenu.MenuFunction2(this.setKeyBothConstant), keyframeContext);
		genericMenu.ShowAsContext();
	}
	private void setKeyLeftConstant(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsLeftConstant(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyLeftConstant(keyframeContext.key);
			if (CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyRightConstant(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsRightConstant(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyRightConstant(keyframeContext.key);
			if (CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			haveCurvesChanged = true;
		}
	}
	private void setKeyBothConstant(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && (!keyframeContext.curveWrapper.IsRightConstant(keyframeContext.key) || !keyframeContext.curveWrapper.IsLeftConstant(keyframeContext.key)))
		{
			keyframeContext.curveWrapper.SetKeyLeftConstant(keyframeContext.key);
			keyframeContext.curveWrapper.SetKeyRightConstant(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyLeftLinear(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsLeftLinear(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyLeftLinear(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyLeftFree(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsLeftFree(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyLeftFree(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyBothLinear(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && (!keyframeContext.curveWrapper.IsRightLinear(keyframeContext.key) || !keyframeContext.curveWrapper.IsLeftLinear(keyframeContext.key)))
		{
			keyframeContext.curveWrapper.SetKeyLeftLinear(keyframeContext.key);
			keyframeContext.curveWrapper.SetKeyRightLinear(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyBothFree(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && (!keyframeContext.curveWrapper.IsRightFree(keyframeContext.key) || !keyframeContext.curveWrapper.IsLeftFree(keyframeContext.key)))
		{
			keyframeContext.curveWrapper.SetKeyLeftFree(keyframeContext.key);
			keyframeContext.curveWrapper.SetKeyRightFree(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyRightLinear(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsLeftLinear(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyRightLinear(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyRightFree(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsRightFree(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyRightFree(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyBroken(object userData)
	{
		KeyframeContext keyframeContext = userData as CinemaCurveClipItemControl.KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsRightFree(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyBroken(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyAuto(object userData)
	{
		CinemaCurveClipItemControl.KeyframeContext keyframeContext = userData as CinemaCurveClipItemControl.KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsAuto(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyAuto(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyFreeSmooth(object userData)
	{
		CinemaCurveClipItemControl.KeyframeContext keyframeContext = userData as CinemaCurveClipItemControl.KeyframeContext;
		if (keyframeContext != null && !keyframeContext.curveWrapper.IsFreeSmooth(keyframeContext.key))
		{
			keyframeContext.curveWrapper.SetKeyFreeSmooth(keyframeContext.key);
			if (this.CurvesChanged != null)
			{
				CinemaClipCurveWrapper cinemaClipCurveWrapper = base.Wrapper as CinemaClipCurveWrapper;
				if (cinemaClipCurveWrapper != null)
				{
					this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
				}
			}
			this.haveCurvesChanged = true;
		}
	}
	private void setKeyFlat(object userData)
	{
		KeyframeContext keyframeContext = userData as KeyframeContext;
		if (keyframeContext != null)
		{
			Keyframe keyframe = keyframeContext.curveWrapper.GetKeyframe(keyframeContext.key);
			if (keyframe.tangentMode != 0 || keyframe.inTangent != 0f || keyframe.outTangent != 0f)
			{
				keyframeContext.curveWrapper.FlattenKey(keyframeContext.key);
				if (this.CurvesChanged != null)
				{
					CinemaClipCurveWrapper cinemaClipCurveWrapper = Wrapper as CinemaClipCurveWrapper;
					if (cinemaClipCurveWrapper != null)
					{
						this.CurvesChanged(this, new CurveClipWrapperEventArgs(cinemaClipCurveWrapper));
					}
				}
				haveCurvesChanged = true;
			}
		}
	}
    private HashSet<int> selectedKeyHashes
    {
        get { return selection.selectedKeyHashs; }
        set { selection.selectedKeyHashs = value; }
    }

    private List<CinemaKeyframeWrapper> m_SelectedKeysCache;
    private Bounds? m_SelectionBoundsCache;
    public bool KeyIsSelected(int hash)
    {
        return selectedKeyHashes.Contains(hash);
    }
    public void SelectKey(CinemaKeyframeWrapper keyframe)
    {
        int hash = keyframe.GetHash();
        if (!selectedKeyHashes.Contains(hash))
            selectedKeyHashes.Add(hash);
    }
    public void SaveKeySelection(string undoLabel)
    {
        Undo.RegisterCompleteObjectUndo(selection, undoLabel);
    }
    
    public void UnselectKey(CinemaKeyframeWrapper keyframe)
    {
        int hash = keyframe.GetHash();
        if (selectedKeyHashes.Contains(hash))
            selectedKeyHashes.Remove(hash);
        m_SelectedKeysCache = null;
        m_SelectionBoundsCache = null;
    }
    public void ClearKeySelections()
    {
        selectedKeyHashes.Clear();
        m_SelectedKeysCache = null;
        m_SelectionBoundsCache = null;
    }
    public void OnDestroy()
    {
        Object.DestroyImmediate(selection);
    }

    public Rect GetCurrentPixelRect()
    {
        float height = controlPosition.height;
        Rect r = DirectorHelper.FromToRect(m_StartPoint, m_EndPoint);
        r.xMin = DirectorWindow.directorControl.TimeToPixel(DirectorWindow.directorControl.PixelToTime(r.xMin));
        r.xMax = DirectorWindow.directorControl.TimeToPixel(DirectorWindow.directorControl.PixelToTime(r.xMax));
        r.yMin = Mathf.Floor(r.yMin / height) * height;
        r.yMax = (Mathf.Floor(r.yMax / height) + 1) * height;
        return r;
    }
    public Rect GetCurrentTimeRect()
    {
        float height = controlPosition.height;
        Rect r = DirectorHelper.FromToRect(m_StartPoint, m_EndPoint);
        r.xMin = DirectorWindow.directorControl.PixelToTime(r.xMin);
        r.xMax = DirectorWindow.directorControl.PixelToTime(r.xMax);
        r.yMin = Mathf.Floor(r.yMin / height) * height;
        r.yMax = (Mathf.Floor(r.yMax / height) + 1) * height;
        return r;
    }
}