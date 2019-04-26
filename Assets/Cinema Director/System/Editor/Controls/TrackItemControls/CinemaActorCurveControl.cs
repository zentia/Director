using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CinemaDirector;

[CutsceneItemControlAttribute(typeof(CinemaActorClipCurve))]
public class CinemaActorCurveControl : CinemaCurveControl
{
    private const float QUATERNION_THRESHOLD = 0.05f;
    private bool hasUserInteracted;
    
    public override void PostUpdate(DirectorControlState state, bool inArea, EventType type)
    {
        CinemaActorClipCurve clipCurve = Wrapper.Behaviour as CinemaActorClipCurve;
        if (clipCurve == null) return;

        hasUndoRedoBeenPerformed = (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed");
        if ((HaveCurvesChanged || hasUndoRedoBeenPerformed) && state.IsInPreviewMode)
        {
            clipCurve.SampleTime(state.ScrubberPosition);
            HaveCurvesChanged = false;
            hasUserInteracted = false;
        }
        else
        {
            if (state.IsInPreviewMode && IsEditing && GUIUtility.hotControl == 0 &&(clipCurve.Firetime <= state.ScrubberPosition &&state.ScrubberPosition <= clipCurve.Firetime + clipCurve.Duration) && clipCurve.Actor != null)
            {
                inArea = !(Event.current.shift && Event.current.keyCode == KeyCode.F && Event.current.control);
                checkToAddNewKeyframes(clipCurve, state, EditorWindow.focusedWindow == DirectorWindow.Instance && inArea);
            }
        }
    }

    protected override void showContextMenu(Behaviour behaviour)
    {
        CinemaActorClipCurve clipCurve = behaviour as CinemaActorClipCurve;
        if (clipCurve == null) return;

        List<KeyValuePair<string, string>> currentCurves = new List<KeyValuePair<string, string>>();
        foreach (MemberClipCurveData data in clipCurve.CurveData)
        {
            KeyValuePair<string, string> curveStrings = new KeyValuePair<string, string>(data.Type, data.PropertyName);
            currentCurves.Add(curveStrings);
        }

        GenericMenu createMenu = new GenericMenu();

        createMenu.AddItem(new GUIContent("Rename"), false, renameItem, behaviour);
        createMenu.AddItem(new GUIContent("Copy"), false, copyItem, behaviour);
        createMenu.AddItem(new GUIContent("Delete"), false, deleteItem, behaviour);
        createMenu.AddSeparator(string.Empty);
        if (clipCurve.Actor != null)
        {
            Component[] components = DirectorHelper.getValidComponents(clipCurve.Actor.gameObject);

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                MemberInfo[] members = DirectorHelper.getValidMembers(component);
                for (int j = 0; j < members.Length; j++)
                {
                    AddCurveContext context = new AddCurveContext();
                    context.clipCurve = clipCurve;
                    context.component = component;
                    context.memberInfo = members[j];
                    if (!currentCurves.Contains(new KeyValuePair<string, string>(component.GetType().Name, members[j].Name)))
                    {
                        createMenu.AddItem(new GUIContent(string.Format("Add Curve/{0}/{1}", component.GetType().Name, DirectorHelper.GetUserFriendlyName(component, members[j]))), false, addCurve, context);
                    }
                }
            }
            createMenu.AddItem(new GUIContent("Load Curve"), false, loadItem, clipCurve);
        }
        createMenu.ShowAsContext();
    }

    private void addCurve(object userData)
    {
        AddCurveContext arg = userData as AddCurveContext;
        if (arg != null)
        {
            Type t = null;
            PropertyInfo property = arg.memberInfo as PropertyInfo;
            FieldInfo field = arg.memberInfo as FieldInfo;
            bool isProperty = false;
            if (property != null)
            {
                t = property.PropertyType;
                isProperty = true;
            }
            else if (field != null)
            {
                t = field.FieldType;
                isProperty = false;
            }
            Undo.RecordObject(arg.clipCurve, "Added Curve");
            arg.clipCurve.AddClipCurveData(arg.component, arg.memberInfo.Name, isProperty, t);
            EditorUtility.SetDirty(arg.clipCurve);
        }
    }

    private void checkToAddNewKeyframes(CinemaActorClipCurve clipCurve, DirectorControlState state, bool inArea)
    {
        Undo.RecordObject(clipCurve, "Auto Key Created");
        bool hasDifferenceBeenFound = false;
        foreach (MemberClipCurveData data in clipCurve.CurveData)
        {
            if (data.Type == string.Empty || data.PropertyName == string.Empty) continue;

            Component component = clipCurve.Actor.GetComponent(data.Type);
            object value = CinemaActorClipCurve.GetCurrentValue(component, data.PropertyName, data.IsProperty);

            PropertyTypeInfo typeInfo = data.PropertyType;
            if (typeInfo == PropertyTypeInfo.Int || typeInfo == PropertyTypeInfo.Long || typeInfo == PropertyTypeInfo.Float || typeInfo == PropertyTypeInfo.Double)
            {
                float x = (float)value;
                float curve1Value = data.Curve1.Evaluate(state.ScrubberPosition);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(x, curve1Value, data.Curve1, state.ScrubberPosition, inArea);
            }
            else if (typeInfo == PropertyTypeInfo.Vector2)
            {
                Vector2 vec2 = (Vector2)value;
                float curve1Value = data.Curve1.Evaluate(state.ScrubberPosition);
                float curve2Value = data.Curve2.Evaluate(state.ScrubberPosition);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(vec2.x, curve1Value, data.Curve1, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(vec2.y, curve2Value, data.Curve2, state.ScrubberPosition, inArea);
            }
            else if (typeInfo == PropertyTypeInfo.Vector3)
            {
                Vector3 vec3 = (Vector3)value;
                float curve1Value = data.Curve1.Evaluate(state.ScrubberPosition);
                float curve2Value = data.Curve2.Evaluate(state.ScrubberPosition);
                float curve3Value = data.Curve3.Evaluate(state.ScrubberPosition);

                hasDifferenceBeenFound |= addKeyOnUserInteraction(vec3.x, curve1Value, data.Curve1, state.ScrubberPosition, inArea);
                hasDifferenceBeenFound |= addKeyOnUserInteraction(vec3.y, curve2Value, data.Curve2, state.ScrubberPosition, inArea);
                hasDifferenceBeenFound |= addKeyOnUserInteraction(vec3.z, curve3Value, data.Curve3, state.ScrubberPosition, inArea);
                
            }
            else if (typeInfo == PropertyTypeInfo.Vector4)
            {
                Vector4 vec4 = (Vector4)value;
                float curve1Value = data.Curve1.Evaluate(state.ScrubberPosition);
                float curve2Value = data.Curve2.Evaluate(state.ScrubberPosition);
                float curve3Value = data.Curve3.Evaluate(state.ScrubberPosition);
                float curve4Value = data.Curve4.Evaluate(state.ScrubberPosition);

				hasDifferenceBeenFound |= addKeyOnUserInteraction(vec4.x, curve1Value, data.Curve1, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(vec4.y, curve2Value, data.Curve2, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(vec4.z, curve3Value, data.Curve3, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(vec4.w, curve4Value, data.Curve4, state.ScrubberPosition, inArea);

            }
            else if (typeInfo == PropertyTypeInfo.Quaternion)
            {
                Quaternion quaternion = (Quaternion)value;
                float curve1Value = data.Curve1.Evaluate(state.ScrubberPosition);
                float curve2Value = data.Curve2.Evaluate(state.ScrubberPosition);
                float curve3Value = data.Curve3.Evaluate(state.ScrubberPosition);
                float curve4Value = data.Curve4.Evaluate(state.ScrubberPosition);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(quaternion.x, curve1Value, data.Curve1, state.ScrubberPosition, inArea,QUATERNION_THRESHOLD);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(quaternion.y, curve2Value, data.Curve2, state.ScrubberPosition, inArea,QUATERNION_THRESHOLD);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(quaternion.z, curve3Value, data.Curve3, state.ScrubberPosition, inArea,QUATERNION_THRESHOLD);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(quaternion.w, curve4Value, data.Curve4, state.ScrubberPosition, inArea,QUATERNION_THRESHOLD);
            }
            else if (typeInfo == PropertyTypeInfo.Color)
            {
                Color color = (Color)value;
                float curve1Value = data.Curve1.Evaluate(state.ScrubberPosition);
                float curve2Value = data.Curve2.Evaluate(state.ScrubberPosition);
                float curve3Value = data.Curve3.Evaluate(state.ScrubberPosition);
                float curve4Value = data.Curve4.Evaluate(state.ScrubberPosition);

				hasDifferenceBeenFound |= addKeyOnUserInteraction(color.r, curve1Value, data.Curve1, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(color.g, curve2Value, data.Curve2, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(color.b, curve3Value, data.Curve3, state.ScrubberPosition, inArea);
				hasDifferenceBeenFound |= addKeyOnUserInteraction(color.a, curve4Value, data.Curve4, state.ScrubberPosition, inArea);
            }
        }
        if (hasDifferenceBeenFound)
        {
            hasUserInteracted = true;
            FillReverseKeyframes();
            EditorUtility.SetDirty(clipCurve);
        }
        else
        {
            m_RecordList.Clear();
        }
    }
    private void FillReverseKeyframes()
    {
        for (int i = 0; i < m_RecordList.Count; i++)
        {
            AddOrMoveKey(m_RecordList[i].m_Time, m_RecordList[i].m_Curve, m_RecordList[i].m_Value);
        }
        m_RecordList.Clear();
    }
    private void AddOrMoveKey(float time, AnimationCurve curve, float value)
    {
        
        bool doesKeyExist = false;
        AnimationKeyTime akt = AnimationKeyTime.Time(time, DirectorWindow.directorControl.frameRate);
        for (int j = 0; j < curve.length; j++)
        {
            Keyframe k = curve[j];
            if (akt.ContainsTime(k.time))
            {
                Keyframe newKeyframe = new Keyframe(k.time, value, k.inTangent, k.outTangent);
                newKeyframe.tangentMode = k.tangentMode;
                AnimationCurveHelper.MoveKey(curve, j, newKeyframe);
                doesKeyExist = true;
            }
        }
        if (!doesKeyExist)
        {
            Keyframe kf = new Keyframe(time, value);
            AnimationCurveHelper.AddKey(curve, kf, akt);
        }
    }
    class RecordKeyframe
    {
        public AnimationCurve m_Curve;
        public float m_Time;
        public float m_Value;
    }
    static List<RecordKeyframe> m_RecordList = new List<RecordKeyframe>();
    private void RecordNeedKeyframeCurve(float time, AnimationCurve curve, float value)
    {
        var record = new RecordKeyframe
        {
            m_Curve = curve,
            m_Time = time,
            m_Value = value,
        };
        m_RecordList.Add(record);
    }
    private bool addKeyOnUserInteraction(float value, float curveValue, AnimationCurve curve, float scrubberPosition, bool inArea = true, float theshold = 0.000001f)
    {
        var time = (float)Mathf.RoundToInt(scrubberPosition * DirectorWindow.directorControl.frameRate) / DirectorWindow.directorControl.frameRate;
        bool differenceFound = false;
		var v = Math.Abs(value - curveValue);
		if (v > theshold && !inArea)
        {
            differenceFound = true;
            if (hasUserInteracted)
            {
                AddOrMoveKey(time, curve, value);
            }
        }
        else if (Event.current.keyCode == KeyCode.K)
        {
            AnimationKeyTime akt = AnimationKeyTime.Time(time, DirectorWindow.directorControl.frameRate);
            for (int j = 0; j < curve.length; j++)
            {
                Keyframe k = curve[j];
                if (akt.ContainsTime(k.time))
                {
                    return false;
                }
            }
            Keyframe kf = new Keyframe(time, value);
            AnimationCurveHelper.AddKey(curve, kf, akt);
            differenceFound = true;
        }
        else
        {
            RecordNeedKeyframeCurve(time, curve, value);
        }
        return differenceFound;
    }

    private class AddCurveContext
    {
        public CinemaActorClipCurve clipCurve;
        public Component component;
        public MemberInfo memberInfo;
    }
}
