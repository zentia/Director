using System;
using System.Collections.Generic;
using Assets.Plugins.Common;
using Highlight;
using UnityEngine;
using UnityService.Xml.Serialization;

namespace TimelineRuntime
{
    [TimelineItem("Curve Clip", "Actor Curve Clip", TimelineItemGenre.CurveClipItem)]
    public class TimelineActorCurveClip : TimelineCurveClip, IRecoverableObject
    {
        [SerializeField] private RevertMode runtimeRevertMode = RevertMode.Finalize;
        [ObjectSpace]
        public ObjectSpace ObjectSpaceId = new();
        public bool ObjectSpaceUnAvailable = false;
        private Transform m_ObjectSpace;
        private bool m_logErrored;

        internal void Reset()
        {
            foreach (MemberCurveClipData memberData in CurveData)
            {
                memberData.Reset(Actor);
            }
        }

        public RevertInfo[] CacheState()
        {
            var reverts = new List<RevertInfo>();
            var actors = GetActors();
            if (actors != null)
            {
                for (var i = 0; i < CurveData.Count; i++)
                {
                    foreach (var actor in GetActors())
                    {
                        if (actor != null)
                        {
                            var component = actor.GetComponent(CurveData[i].Type);
                            if (component != null)
                            {
                                var info = new RevertInfo(this, component, CurveData[i].PropertyName, CurveData[i].GetCurrentValue(component));
                                reverts.Add(info);
                            }
                        }
                    }

                }
            }
            return reverts.ToArray();
        }

        public RevertMode RuntimeRevertMode
        {
            get => runtimeRevertMode;
            set => runtimeRevertMode = value;
        }

        public override void Initialize()
        {
            if (timeline.sceneRoot && !ObjectSpaceUnAvailable)
            {
                if (string.IsNullOrEmpty(ObjectSpaceId.path))
                {
                    m_ObjectSpace = timeline.sceneRoot.transform;
                }
                else
                {
                    var go = timeline.sceneRoot.FindChildBFS(ObjectSpaceId.path);
                    if (go != null)
                    {
                        m_ObjectSpace = go.transform;
                    }
                }
            }

            m_logErrored = false;
        }

        public override void SampleTime(Transform actor, float time)
        {
            if (Firetime <= time && time <= Firetime + Duration)
            {
                CurveData.ForEach(memberCurveClipData =>
                {
                    if (timeline.SampleDelegate != null)
                    {
                        if (!timeline.SampleDelegate(memberCurveClipData, actor, time) && !m_logErrored)
                        {
                            m_logErrored = true;
                            Log.LogE(LogTag.Timeline, "SampleDelegate return false, timeline: {0}, propertyName: {1}", timeline.name, memberCurveClipData.PropertyName);
                        }
                        return;
                    }

                    if (memberCurveClipData.IsProperty)
                    {
                        if (m_ObjectSpace != null)
                        {
                            switch (memberCurveClipData.PropertyName)
                            {
                                case "position":
                                    actor.transform.position = m_ObjectSpace.localToWorldMatrix.MultiplyPoint(EvaluateVector3(memberCurveClipData, time));
                                    return;
                                case "rotation":
                                    actor.transform.rotation = m_ObjectSpace.rotation * EvaluateQuaternion(memberCurveClipData, time);
                                    return;
                                case "localPosition":
                                {
                                    var relatePosition = m_ObjectSpace.TransformPoint(EvaluateVector3(memberCurveClipData, time));
                                    var parent = actor.parent;
                                    actor.transform.localPosition = parent != null ? parent.InverseTransformPoint(relatePosition) : relatePosition;
                                }
                                    return;
                                case "localEulerAngles":
                                    actor.transform.rotation = m_ObjectSpace.rotation * Quaternion.Euler(EvaluateVector3(memberCurveClipData, time));
                                    return;
                                case "localRotation":
                                {
                                    var rot = m_ObjectSpace.rotation * EvaluateQuaternion(memberCurveClipData, time);
                                    var parent = actor.parent;
                                    if (parent != null)
                                    {
                                        rot *= parent.rotation;
                                    }
                                    actor.transform.rotation = rot;
                                }
                                    return;
                            }
                        }
                        switch (memberCurveClipData.PropertyName)
                        {
                            case "position":
                                actor.transform.position = EvaluateVector3(memberCurveClipData, time);
                                return;
                            case "rotation":
                                actor.transform.rotation = EvaluateQuaternion(memberCurveClipData, time);
                                return;
                            case "localPosition":
                                actor.transform.localPosition = EvaluateVector3(memberCurveClipData, time);
                                return;
                            case "localEulerAngles":
                                actor.transform.rotation = Quaternion.Euler(EvaluateVector3(memberCurveClipData, time));
                                return;
                            case "localRotation":
                                actor.transform.rotation = EvaluateQuaternion(memberCurveClipData, time);
                                return;
                            case "color":
                            {
                                var l = actor.GetComponent<Light>();
                                if (l == null)
                                    return;
                                l.color = EvaluateColor(memberCurveClipData, time);
                            }
                                return;
                            case "intensity":
                            {
                                var l = actor.GetComponent<Light>();
                                if (l == null)
                                    return;
                                l.intensity = EvaluateFloat(memberCurveClipData, time);
                            }
                                return;
                            case "fov":
                            case "fieldOfView":
                            {
                                var c = actor.GetComponent<Camera>();
                                if (c == null)
                                    return;
                                c.fieldOfView = EvaluateFloat(memberCurveClipData, time);
                            }
                                return;
                            case "vPosition":
                            {
                                var v = actor.GetComponent<VirtualCamera>();
                                if (v != null)
                                {
                                    v.Position = EvaluateVector3(memberCurveClipData, time);
                                }
                            }
                                return;
                            case "vAngles":
                            {
                                var v = actor.GetComponent<VirtualCamera>();
                                if (v != null)
                                {
                                    v.Angles = EvaluateVector3(memberCurveClipData, time);
                                }
                            }
                                return;
                            case "vFov":
                            {
                                var v = actor.GetComponent<VirtualCamera>();
                                if (v != null)
                                {
                                    v.FieldOfView = EvaluateFloat(memberCurveClipData, time);
                                }
                            }
                                return;
                            default:
                            {
                                var component = actor.GetComponent(memberCurveClipData.Type);
                                if (component == null)
                                    return;
                                var componentType = component.GetType();
                                var propertyInfo = ReflectionHelper.GetProperty(componentType, memberCurveClipData.PropertyName);
                                if (propertyInfo == null)
                                {
                                    return;
                                }
                                var value = Evaluate(memberCurveClipData, time, propertyInfo.PropertyType);
                                if (propertyInfo.PropertyType == value.GetType())
                                {
                                    propertyInfo.SetValue(component, value, null);
                                }
                                else
                                {
                                    Debug.LogErrorFormat("Actor={0},Component={1},Property={2},OriginType={3},TargetType={4}", actor.name, componentType, memberCurveClipData.PropertyName, propertyInfo.GetValueType(), value.GetType());
                                }
                            }
                                return;
                        }
                    }
                    {
                        switch (memberCurveClipData.PropertyName)
                        {
                            case "color":
                            {
                                var l = actor.GetComponent<Yarp.YALight>();
                                if (l == null)
                                    return;
                                l.color = EvaluateColor(memberCurveClipData, time);
                            }
                                return;
                            case "intensity":
                            {
                                var l = actor.GetComponent<Yarp.YALight>();
                                if (l == null)
                                    return;
                                l.intensity = EvaluateFloat(memberCurveClipData, time);
                            }
                                return;
                            case "Intensity":
                            {
                                var l = actor.GetComponent<Yarp.EnvironmentLight>();
                                if (l == null)
                                    return;
                                l.Intensity = EvaluateColor(memberCurveClipData, time);
                            }
                                return;
                            default:
                            {
                                var component = actor.GetComponent(memberCurveClipData.Type);
                                if (component == null)
                                    return;
                                var componentType = component.GetType();
                                var fieldInfo = ReflectionHelper.GetField(componentType, memberCurveClipData.PropertyName);
                                if (fieldInfo != null)
                                {
                                    fieldInfo.SetValue(component, Evaluate(memberCurveClipData, time, fieldInfo.FieldType));
                                }
                            }
                                return;
                        }
                    }
                });
            }
        }

        public override void SetTime(GameObject actor, float time, float deltaTime)
        {
            SampleTime(actor.transform, time);
        }
    }
}
