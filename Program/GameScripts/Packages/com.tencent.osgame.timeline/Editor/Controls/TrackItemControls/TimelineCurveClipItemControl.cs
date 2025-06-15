using System;
using System.Collections.Generic;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public abstract class TimelineCurveClipItemControl : ActionItemControl
    {
        private static float ms_MouseDownOffset = -1f;
        protected KeyframeContext cacheKey;
        protected Rect curveCanvasPosition;
        protected Rect curveTrackSafeArea;

        protected bool isCurveClipEmpty = true;
        protected bool isFolded;
        public SortedList<int, int> keyframeTimes = new ();
        private GenericMenu m_MultiMenu;
        private readonly List<TimelineCurveSelection> m_Selections = new ();
        private Rect m_MasterKeysPosition;
        private Rect m_TimelineItemPosition;
        private Rect m_ViewingSpace = new (0f, 0f, 1f, 1f);
        private int m_CacheMousePosition;
        private bool m_CurvesChanged;
        private const float QuaternionThreshold = 0.5f;
        private const float Threshold = 0.0001f;
        private bool _currentlyEditingRotation;
        public TimelineClipCurveWrapper ClipCurveWrapper=>Wrapper as TimelineClipCurveWrapper;

        private int MousePositionOffsetFrame
        {
            get
            {
                var mousePosition = Event.current.mousePosition.x;
                mousePosition /= ControlState.Scale.x;
                short mouseFrame = TimelineUtility.TimeToFrame(mousePosition);
                int offset = mouseFrame - m_CacheMousePosition;
                if (MathF.Abs(offset) > 0)
                {
                    m_CacheMousePosition = mouseFrame;
                }
                return offset;
            }
        }

        protected bool curvesChanged
        {
            get => m_CurvesChanged;
            set
            {
                m_CurvesChanged = value;
                if (value)
                {
                    var wrapper = Wrapper as TimelineClipCurveWrapper;
                    if (wrapper != null)
                        CurvesChanged(this, new CurveClipWrapperEventArgs(wrapper));
                }
            }
        }

        public event CurveClipWrapperEventHandler CurvesChanged;
        public event CurveClipScrubberEventHandler SnapScrubberEvent;
        public event CurveClipItemEventHandler TranslateCurveClipItem;

        public void AddKeyframe(object userdata)
        {
            var context = userdata as CurvesContext;
            var time = TimelineUtility.FrameToTime(context.frameNumber);
            var clip = Wrapper.timelineItem as TimelineActorCurveClip;
            var memberCurves = context.wrapper.MemberCurves;
            if (clip.timeline.state == Timeline.TimelineState.Paused && GUIUtility.hotControl == 0 && (clip.fireFrame <= context.frameNumber && context.frameNumber <= clip.endFrame) && clip.Actor != null)
            {
                bool hasDifferenceBeenFound = false;
                for (int i = 0; i < memberCurves.Count; i++)
                {
                    MemberCurveClipData data = clip.CurveData[i];
                    var curve = memberCurves[i];
                    if (data.Type == string.Empty || data.PropertyName == string.Empty)
                        continue;

                    _currentlyEditingRotation = data.PropertyName == "localEulerAngles";

                    var component = clip.Actor.GetComponent(data.Type);
                    var value = clip.GetCurrentValue(component, data, data.IsProperty);

                    var typeInfo = data.PropertyType;

                    if (typeInfo == PropertyTypeInfo.Int || typeInfo == PropertyTypeInfo.Long || typeInfo == PropertyTypeInfo.Bool || typeInfo == PropertyTypeInfo.Enum)
                    {
                        float curve1Value = data.Curve1.Evaluate(time);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(Convert.ToInt32(value), curve1Value, data.Curve1, context.frameNumber);
                    }
                    else if (typeInfo == PropertyTypeInfo.Float || typeInfo == PropertyTypeInfo.Double)
                    {
                        var curve1Value = data.Curve1.Evaluate(time);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(Convert.ToSingle(value), curve1Value, curve.AnimationCurves[0], context.frameNumber);
                    }
                    else if (typeInfo == PropertyTypeInfo.Vector2)
                    {
                        Vector2 vec2 = (Vector2)value;
                        float curve1Value = data.Curve1.Evaluate(time);
                        float curve2Value = data.Curve2.Evaluate(time);

                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec2.x, curve1Value, curve.AnimationCurves[0], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec2.y, curve2Value, curve.AnimationCurves[1], context.frameNumber);
                    }
                    else if (typeInfo == PropertyTypeInfo.Vector3)
                    {
                        var vec3 = (Vector3)value;
                        var curve1Value = data.Curve1.Evaluate(time);
                        var curve2Value = data.Curve2.Evaluate(time);
                        var curve3Value = data.Curve3.Evaluate(time);
                        if (_currentlyEditingRotation && clip.Actor.transform.hasChanged)
                        {
                            var test = Math.Abs(vec3.x - curve1Value) % 360;
                            if ((test) > 0.01)
                                hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec3.x, curve1Value, curve.AnimationCurves[0], context.frameNumber);
                            test = Math.Abs(vec3.y - curve2Value) % 360;
                            if ((test) > 0.01)
                                hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec3.y, curve2Value, curve.AnimationCurves[1], context.frameNumber);
                            test = Math.Abs(vec3.z - curve3Value) % 360;
                            if ((test) > 0.01)
                                hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec3.z, curve3Value, curve.AnimationCurves[2], context.frameNumber);
                        }
                        else
                        {
                            hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec3.x, curve1Value, curve.AnimationCurves[0], context.frameNumber);
                            hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec3.y, curve2Value, curve.AnimationCurves[1], context.frameNumber);
                            hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec3.z, curve3Value, curve.AnimationCurves[2], context.frameNumber);
                        }
                    }
                    else if (typeInfo == PropertyTypeInfo.Vector4)
                    {
                        Vector4 vec4 = (Vector4)value;
                        float curve1Value = data.Curve1.Evaluate(time);
                        float curve2Value = data.Curve2.Evaluate(time);
                        float curve3Value = data.Curve3.Evaluate(time);
                        float curve4Value = data.Curve4.Evaluate(time);

                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec4.x, curve1Value, curve.AnimationCurves[0], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec4.y, curve2Value, curve.AnimationCurves[1], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec4.z, curve3Value, curve.AnimationCurves[2], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(vec4.w, curve4Value, curve.AnimationCurves[3], context.frameNumber);

                    }
                    else if (typeInfo == PropertyTypeInfo.Quaternion)
                    {
                        Quaternion quaternion = (Quaternion)value;
                        float curve1Value = data.Curve1.Evaluate(time);
                        float curve2Value = data.Curve2.Evaluate(time);
                        float curve3Value = data.Curve3.Evaluate(time);
                        float curve4Value = data.Curve4.Evaluate(time);

                        for (int j = 0; j < data.Curve1.length; j++)
                        {
                            Keyframe k = data.Curve1[j];
                            if (k.GetFrame() == context.frameNumber)
                            {
                                Keyframe newKeyframe = new Keyframe(k.time, quaternion.x, k.inTangent, k.outTangent);
                                data.Curve1.MoveKey(j, newKeyframe);
                            }
                        }

                        for (int j = 0; j < data.Curve2.length; j++)
                        {
                            Keyframe k = data.Curve2[j];
                            if (k.GetFrame() == context.frameNumber)
                            {
                                Keyframe newKeyframe = new Keyframe(k.time, quaternion.y, k.inTangent, k.outTangent);
                                data.Curve2.MoveKey(j, newKeyframe);
                            }
                        }

                        for (int j = 0; j < data.Curve3.length; j++)
                        {
                            Keyframe k = data.Curve3[j];
                            if (k.GetFrame() == context.frameNumber)
                            {
                                Keyframe newKeyframe = new Keyframe(k.time, quaternion.z, k.inTangent, k.outTangent);
                                data.Curve3.MoveKey(j, newKeyframe);
                            }
                        }

                        for (int j = 0; j < data.Curve4.length; j++)
                        {
                            Keyframe k = data.Curve4[j];
                            if (k.GetFrame() == context.frameNumber)
                            {
                                Keyframe newKeyframe = new Keyframe(k.time, quaternion.w, k.inTangent, k.outTangent);
                                data.Curve4.MoveKey(j, newKeyframe);
                            }
                        }

                        Quaternion curveValue = new Quaternion(curve1Value, curve2Value, curve3Value, curve4Value);
                        float angle = Vector3.Angle(quaternion.eulerAngles, curveValue.eulerAngles);
                        hasDifferenceBeenFound = hasDifferenceBeenFound || angle > QuaternionThreshold;
                        if (angle > QuaternionThreshold)
                        {
                            data.Curve1.AddKey(time, quaternion.x);
                            data.Curve2.AddKey(time, quaternion.y);
                            data.Curve3.AddKey(time, quaternion.z);
                            data.Curve4.AddKey(time, quaternion.w);
                        }
                    }
                    else if (typeInfo == PropertyTypeInfo.Color)
                    {
                        Color color = (Color)value;
                        var curve1Value = data.Curve1.Evaluate(time);
                        var curve2Value = data.Curve2.Evaluate(time);
                        var curve3Value = data.Curve3.Evaluate(time);
                        var curve4Value = data.Curve4.Evaluate(time);

                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(color.r, curve1Value, curve.AnimationCurves[0], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(color.g, curve2Value, curve.AnimationCurves[1], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(color.b, curve3Value, curve.AnimationCurves[2], context.frameNumber);
                        hasDifferenceBeenFound |= AddKeyOnUserInteraction(color.a, curve4Value, curve.AnimationCurves[3], context.frameNumber);
                    }
                }
                if (hasDifferenceBeenFound)
                {
                    Undo.RecordObject(clip, "Auto Key Created");
                    EditorUtility.SetDirty(clip);
                    return;
                }
            }
            for (var i = 0; i < memberCurves.Count; i++)
            {
                foreach (var wrapper in memberCurves[i].AnimationCurves)
                {
                    wrapper.AddKey(context.frameNumber, wrapper.Evaluate(time));
                }
            }
            curvesChanged = true;
            ClearSelectedCurves();
        }

        private bool AddKeyOnUserInteraction(int value, float curveValue, AnimationCurve curve, int scrubberPosition)
        {
            int curveVal = Mathf.RoundToInt(curveValue);
            float floatValue = value;

            bool differenceFound = false;
            if (Math.Abs(value - curveVal) != 0)
            {
                differenceFound = true;
                bool doesKeyExist = false;
                for (int j = 0; j < curve.length; j++)
                {
                    Keyframe k = curve[j];
                    if (k.GetFrame() == scrubberPosition)
                    {
                        Keyframe newKeyframe = new Keyframe(k.time, floatValue, k.inTangent, k.outTangent);
                        curve.MoveKey(j, newKeyframe);
                        doesKeyExist = true;
                    }
                }
                if (!doesKeyExist)
                {
                    var kf = new Keyframe(TimelineUtility.FrameToTime(scrubberPosition), floatValue);
                    curve.AddKey(kf);
                }
            }
            return differenceFound;
        }

        private bool AddKeyOnUserInteraction(float value, float curveValue, TimelineAnimationCurveWrapper curve, int scrubberPosition)
        {
            bool differenceFound = false;
            if (!(Math.Abs(value - curveValue) < Threshold))
            {
                differenceFound = true;
                bool doesKeyExist = false;
                for (int j = 0; j < curve.KeyframeCount; j++)
                {
                    Keyframe k = curve.GetKeyframe(j);
                    if (k.GetFrame() == scrubberPosition)
                    {
                        Keyframe newKeyframe = new Keyframe(k.time, value, k.inTangent, k.outTangent);

                        curve.MoveKey(j, newKeyframe);
                        doesKeyExist = true;
                    }
                }
                if (!doesKeyExist)
                {
                    curve.AddKey(scrubberPosition, value);
                }
            }
            return differenceFound;
        }

        private void AddKeyToCurve(object userData)
        {
            var context = userData as CurveContext;
            if (context != null)
            {
                Undo.RecordObject(Wrapper.timelineItem, "Added Key");
                context.curveWrapper.AddKey(context.frameNumber, context.curveWrapper.Evaluate(TimelineUtility.FrameToTime(context.frameNumber)));
            }
        }

        internal override void ConfirmTranslate()
        {
            var wrapper = Wrapper as TimelineClipCurveWrapper;
            if (wrapper != null)
            {
                if (TranslateCurveClipItem != null)
                    TranslateCurveClipItem(this, new CurveClipItemEventArgs(wrapper.timelineItem, wrapper.fireTime, wrapper.Duration));
                curvesChanged = true;
            }
        }

        private void DeleteKey(object userData)
        {
            var context = userData as KeyframeContext;
            if (context != null)
            {
                context.curveWrapper.RemoveKey(context.key);
                curvesChanged = true;
                ClearSelectedCurves();
            }
        }

        public void LoadFromAnimation(AnimationClip clip)
        {
            ReadDccAnimationData(clip, "Dummy001");
        }

        private void ReadUnityAnimationData(List<AnimationCurve> curves, AnimationClip animationClip, List<EditorCurveBinding> curveBindings, bool convert)
        {
            var clipCurveWrapper = Wrapper as TimelineClipCurveWrapper;
            if (clipCurveWrapper != null)
            {
                var curveClip = clipCurveWrapper.timelineItem as TimelineCurveClip;
                if (curveClip != null)
                {
                    curveClip.CurveData.Clear();
                    Component trans = clipCurveWrapper.timelineItem.transform;
                    curveClip.AddClipCurveData(trans, "localPosition", true, typeof(Vector3));
                    curveClip.AddClipCurveData(trans, "localEulerAngles", true, typeof(Vector3));

                    for (int i = 0; i < curveBindings.Count; ++i)
                    {
                        var binding = curveBindings[i];
                        var curve = curves[i];

                        MemberCurveClipData curveData = null;
                        if (binding.propertyName.StartsWith("m_LocalPosition"))
                        {
                            curveData = curveClip.CurveData[0];
                        }
                        else if (binding.propertyName.StartsWith("m_LocalScale"))
                        {
                            continue;
                        }
                        else if (binding.propertyName.StartsWith("m_LocalRotation") || binding.propertyName.StartsWith("localEulerAnglesRaw"))
                        {
                            curveData = curveClip.CurveData[1];
                        }

                        if (binding.propertyName.EndsWith(".x"))
                        {
                            curveData.SetCurve(0, curve);
                        }
                        else if (binding.propertyName.EndsWith(".y"))
                        {
                            curveData.SetCurve(1, curve);
                        }
                        else if (binding.propertyName.EndsWith(".z"))
                        {
                            curveData.SetCurve(2, curve);
                        }
                        else if (binding.propertyName.EndsWith(".w"))
                        {
                            curveData.SetCurve(3, curve);
                        }
                    }
                }

                if (convert)
                {
                    var animationCurves = QuaternionToEuler(curveClip.CurveData[1].Curve1,curveClip.CurveData[1].Curve2,curveClip.CurveData[1].Curve3,curveClip.CurveData[1].Curve4);
                    curveClip.CurveData[1].Curve1 = animationCurves[0];
                    curveClip.CurveData[1].Curve2 = animationCurves[1];
                    curveClip.CurveData[1].Curve3 = animationCurves[2];
                }

                clipCurveWrapper.Duration = animationClip.length;
                UpdateCurveWrappers(clipCurveWrapper);
            }
        }

        private void ReadDccAnimationData(AnimationClip animationClip, string boneName)
        {
            if (animationClip == null || string.IsNullOrEmpty(boneName))
            {
                Debug.LogError("Please specify an AnimationClip and a bone name.");
                return;
            }

            var curveBindings = new List<EditorCurveBinding>();
            var  curves = new List<AnimationCurve>();

            EditorCurveBinding[] allCurveBindings = AnimationUtility.GetCurveBindings(animationClip);

            foreach (EditorCurveBinding binding in allCurveBindings)
            {
                curveBindings.Add(binding);
                curves.Add(AnimationUtility.GetEditorCurve(animationClip, binding));
            }

            var compressedClip = new AnimationClip();
            compressedClip.name = "_compressed";

            AnimationCurve qx = null;
            AnimationCurve qy = null;
            AnimationCurve qz = null;
            AnimationCurve qw = null;
            for (int i = 0; i < curveBindings.Count; i++)
            {
                var binding = curveBindings[i];
                var curve = curves[i];
                if (!binding.propertyName.StartsWith("m_LocalRotation"))
                {
                    continue;
                }

                if (binding.propertyName.EndsWith(".x"))
                {
                    qx = curve;
                }
                else if (binding.propertyName.EndsWith(".y"))
                {
                    qy = curve;
                }
                else if (binding.propertyName.EndsWith(".z"))
                {
                    qz = curve;
                }
                else if (binding.propertyName.EndsWith(".w"))
                {
                    qw = curve;
                }
            }

            if (qx == null)
            {
                ReadUnityAnimationData(curves, animationClip, curveBindings, false);
                return;
            }

            var newCurve = Rotate180AroundY(qx, qy, qz, qw);

            if (newCurve != null)
            {
                for (int i = 0; i < curveBindings.Count; i++)
                {
                    var binding = curveBindings[i];
                    var curve = curves[i];
                    AnimationUtility.SetEditorCurve(compressedClip, curveBindings[i], curve);
                    if (!binding.propertyName.StartsWith("m_LocalRotation"))
                    {
                        continue;
                    }

                    if (binding.propertyName.EndsWith(".x"))
                    {
                        AnimationUtility.SetEditorCurve(compressedClip, curveBindings[i], newCurve[0]);
                    }
                    else if (binding.propertyName.EndsWith(".y"))
                    {
                        AnimationUtility.SetEditorCurve(compressedClip, curveBindings[i], newCurve[1]);
                    }
                    else if (binding.propertyName.EndsWith(".z"))
                    {
                        AnimationUtility.SetEditorCurve(compressedClip, curveBindings[i], newCurve[2]);
                    }
                    else if (binding.propertyName.EndsWith(".w"))
                    {
                        AnimationUtility.SetEditorCurve(compressedClip, curveBindings[i], newCurve[3]);
                    }
                }
            }

            compressedClip.EnsureQuaternionContinuity();
            curveBindings.Clear();
            curves.Clear();
            // 获取AnimationClip中所有的EditorCurveBindings
            allCurveBindings = AnimationUtility.GetCurveBindings(compressedClip);
            float tolerance = 0.0001f;
            // 筛选出指定骨骼的EditorCurveBindings
            foreach (EditorCurveBinding binding in allCurveBindings)
            {
                curveBindings.Add(binding);
                var originalCurve = AnimationUtility.GetEditorCurve(compressedClip, binding);
                curves.Add(originalCurve);
            }

            ReadUnityAnimationData(curves, animationClip, curveBindings, true);
        }

        private AnimationCurve[] Rotate180AroundY(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            if (curveX == null)
            {
                return null;
            }
            var newCurveX = new AnimationCurve();
            AnimationCurve newCurveY = new AnimationCurve();
            AnimationCurve newCurveZ = new AnimationCurve();
            AnimationCurve newCurveW = new AnimationCurve();

            Quaternion rotation180 = Quaternion.Euler(0, 180, 0);

            int keyframeCount = curveX.length;
            float ani_length = curveX.keys[keyframeCount - 1].time;
            for (int i = 0; i < keyframeCount; i++)
            {
                float time = curveX[i].time;

                float preTime = time - 0.03333333f;
                if (preTime < 0)
                {
                    preTime = 0;
                }

                float nextTime = time + 0.03333333f;
                if (nextTime > ani_length)
                {
                    nextTime = ani_length;
                }

                float preValueX = curveX.Evaluate(preTime);
                float nextValueX = curveX.Evaluate(nextTime);
                float preValueY = curveY.Evaluate(preTime);
                float nextValueY = curveY.Evaluate(nextTime);
                float preValueZ = curveZ.Evaluate(preTime);
                float nextValueZ = curveZ.Evaluate(nextTime);
                float preValueW = curveW.Evaluate(preTime);
                float nextValueW = curveW.Evaluate(nextTime);

                Quaternion originalRotation = new Quaternion(curveX[i].value, curveY[i].value, curveZ[i].value, curveW[i].value);
                Quaternion newRotation = originalRotation * rotation180;

                Quaternion preOriginalRotation = new Quaternion(preValueX, preValueY, preValueZ, preValueW);
                Quaternion newPreRotation = preOriginalRotation * rotation180;

                Quaternion nextOriginalRotation = new Quaternion(nextValueX, nextValueY, nextValueZ, nextValueW);
                Quaternion newNextRotation = nextOriginalRotation * rotation180;

                float deltatimePre = time - preTime;
                float delattimeNext = nextTime - time;

                float intanx = 0;
                float intany = 0;
                float intanz = 0;
                float intanw = 0;

                float outtanx = 0;
                float outtany = 0;
                float outtanz = 0;
                float outtanw = 0;

                if (deltatimePre > 0.001f)
                {
                    intanx = (newRotation.x - newPreRotation.x) / deltatimePre;
                    intany = (newRotation.y - newPreRotation.y) / deltatimePre;
                    intanz = (newRotation.z - newPreRotation.z) / deltatimePre;
                    intanw = (newRotation.w - newPreRotation.w) / deltatimePre;

                }

                if (delattimeNext > 0.001f)
                {
                    outtanx = (newNextRotation.x - newRotation.x) / delattimeNext;
                    outtany = (newNextRotation.y - newRotation.y) / delattimeNext;
                    outtanz = (newNextRotation.z - newRotation.z) / delattimeNext;
                    outtanw = (newNextRotation.w - newRotation.w) / delattimeNext;

                }
                if (Mathf.Abs(intanx) < 0.0001f)
                {
                    intanx = 0;
                }

                if (Mathf.Abs(intany) < 0.0001f)
                {
                    intany = 0;
                }

                if (Mathf.Abs(intanz) < 0.0001f)
                {
                    intanz = 0;
                }

                if (Mathf.Abs(intanw) < 0.0001f)
                {
                    intanw = 0;
                }

                if (Mathf.Abs(outtanx) < 0.0001f)
                {
                    intanx = 0;
                }

                if (Mathf.Abs(outtany) < 0.0001f)
                {
                    intany = 0;
                }

                if (Mathf.Abs(outtanz) < 0.0001f)
                {
                    intanz = 0;
                }

                if (Mathf.Abs(outtanw) < 0.0001f)
                {
                    intanw = 0;
                }

                Keyframe newXKey = new Keyframe(time, newRotation.x, intanx, outtanx);
                Keyframe newYKey = new Keyframe(time, newRotation.y, intany, outtany);
                Keyframe newZKey = new Keyframe(time, newRotation.z, intanz, outtanz);
                Keyframe newWKey = new Keyframe(time, newRotation.w, intanw, outtanw);

                newCurveX.AddKey(newXKey);
                newCurveY.AddKey(newYKey);
                newCurveZ.AddKey(newZKey);
                newCurveW.AddKey(newWKey);
            }

            return new [] { newCurveX, newCurveY, newCurveZ, newCurveW };
        }

        private static AnimationCurve[] QuaternionToEuler(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            if (curveX == null)
            {
                return null;
            }
            var newCurveX = new AnimationCurve();
            AnimationCurve newCurveY = new AnimationCurve();
            AnimationCurve newCurveZ = new AnimationCurve();

            int keyframeCount = curveX.length;
            float ani_length = curveX.keys[keyframeCount - 1].time;
            for (int i = 0; i < keyframeCount; i++)
            {
                float time = curveX[i].time;

                float preTime = time - 0.03333333f;
                if (preTime < 0)
                {
                    preTime = 0;
                }

                float nextTime = time + 0.03333333f;
                if (nextTime > ani_length)
                {
                    nextTime = ani_length;
                }

                float preValueX = curveX.Evaluate(preTime);
                float nextValueX = curveX.Evaluate(nextTime);
                float preValueY = curveY.Evaluate(preTime);
                float nextValueY = curveY.Evaluate(nextTime);
                float preValueZ = curveZ.Evaluate(preTime);
                float nextValueZ = curveZ.Evaluate(nextTime);
                float preValueW = curveW.Evaluate(preTime);
                float nextValueW = curveW.Evaluate(nextTime);

                var originalRotation = new Quaternion(curveX[i].value, curveY[i].value, curveZ[i].value, curveW[i].value).eulerAngles;
                var preOriginalRotation = new Quaternion(preValueX, preValueY, preValueZ, preValueW).eulerAngles;
                var nextOriginalRotation = new Quaternion(nextValueX, nextValueY, nextValueZ, nextValueW).eulerAngles;

                float deltatimePre = time - preTime;
                float delattimeNext = nextTime - time;

                float intanx = 0;
                float intany = 0;
                float intanz = 0;
                float intanw = 0;

                float outtanx = 0;
                float outtany = 0;
                float outtanz = 0;
                float outtanw = 0;

                if (deltatimePre > 0.001f)
                {
                    intanx = (originalRotation.x - preOriginalRotation.x) / deltatimePre;
                    intany = (originalRotation.y - preOriginalRotation.y) / deltatimePre;
                    intanz = (originalRotation.z - preOriginalRotation.z) / deltatimePre;

                }

                if (delattimeNext > 0.001f)
                {
                    outtanx = (nextOriginalRotation.x - originalRotation.x) / delattimeNext;
                    outtany = (nextOriginalRotation.y - originalRotation.y) / delattimeNext;
                    outtanz = (nextOriginalRotation.z - originalRotation.z) / delattimeNext;

                }
                if (Mathf.Abs(intanx) < 0.0001f)
                {
                    intanx = 0;
                }

                if (Mathf.Abs(intany) < 0.0001f)
                {
                    intany = 0;
                }

                if (Mathf.Abs(intanz) < 0.0001f)
                {
                    intanz = 0;
                }

                if (Mathf.Abs(intanw) < 0.0001f)
                {
                    intanw = 0;
                }

                if (Mathf.Abs(outtanx) < 0.0001f)
                {
                    intanx = 0;
                }

                if (Mathf.Abs(outtany) < 0.0001f)
                {
                    intany = 0;
                }

                if (Mathf.Abs(outtanz) < 0.0001f)
                {
                    intanz = 0;
                }

                if (Mathf.Abs(outtanw) < 0.0001f)
                {
                    intanw = 0;
                }

                Keyframe newXKey = new Keyframe(time, originalRotation.x, intanx, outtanx);
                Keyframe newYKey = new Keyframe(time, originalRotation.y, intany, outtany);
                Keyframe newZKey = new Keyframe(time, originalRotation.z, intanz, outtanz);

                newCurveX.AddKey(newXKey);
                newCurveY.AddKey(newYKey);
                newCurveZ.AddKey(newZKey);
            }

            return new [] { newCurveX, newCurveY, newCurveZ };
        }

        private void CopyKey(object userData)
        {
            var context = userData as KeyframeContext;
            if (context != null)
                cacheKey = context;
            ClearSelectedCurves();
        }

        private void PasteKeyFrame(object userData)
        {
            var curvesContext = userData as CurvesContext;
            if (curvesContext != null && cacheKey != null)
            {
                var memberCurves = curvesContext.wrapper.MemberCurves;
                foreach (var memberCurve in memberCurves)
                {
                    foreach (var wrapper in memberCurve.AnimationCurves)
                    {
                        var data = cacheKey.curveWrapper.GetKeyframe(cacheKey.key);
                        if (cacheKey.curveWrapper == wrapper)
                            wrapper.AddKey(curvesContext.frameNumber, data.value);
                    }
                }
            }
            curvesChanged = true;
            ClearSelectedCurves();
        }

        private void PasteKeyframes(object userData)
        {
            var curvesContext = userData as CurvesContext;
            if (curvesContext != null)
            {
                Paste(curvesContext);
            }

            ClearSelectedCurves();
        }

        private void CopyKeyframes(object userData)
        {
            var curvesContext = userData as CurvesContext;
            if (curvesContext != null)
            {
                TimelineCopyPaste.copyFrameContext = curvesContext;
                TimelineCopyPaste.copyKeyFrames = true;
            }

            ClearSelectedCurves();
        }

        private void DeleteKeyframes(object userData)
        {
            var context = userData as CurvesContext;
            if (context != null)
            {
                var memberCurves = context.wrapper.MemberCurves;
                for (var i = 0; i < memberCurves.Count; i++)
                {
                    var animationCurves = memberCurves[i].AnimationCurves;
                    for (var j = 0; j < animationCurves.Count; j++)
                        animationCurves[j].RemoveAtTime(context.frameNumber);
                }
                curvesChanged = true;
            }

            ClearSelectedCurves();
        }

        public override void Draw(TimelineControlState state)
        {
            var clipCurveWrapper = Wrapper as TimelineClipCurveWrapper;
            if (clipCurveWrapper != null)
            {
                DrawCurveItem();
                if (!isCurveClipEmpty && !isFolded)
                {
                    DrawCurveCanvas(clipCurveWrapper);
                    DrawMasterKeys(state);
                    HandleCurveCanvasInput(clipCurveWrapper, state);
                }
            }
        }

        public override void PostUpdate(TimelineControlState state)
        {
            var clipCurveWrapper = Wrapper as TimelineClipCurveWrapper;
            if (clipCurveWrapper != null && Parent == TimelineCopyPaste.TimelineTrackControl)
                HandlePasteKeyFrames(clipCurveWrapper, state);

            if (clipCurveWrapper != null)
                if (TimelineCopyPaste.focusedControl == this)
                {
                    if (TimelineCopyPaste.LastKey)
                    {
                        var leftTime = float.MinValue;
                        foreach (var keyframeTime in keyframeTimes)
                            if (keyframeTime.Key < TimelineCopyPaste.CurrentRunningTime && keyframeTime.Key > leftTime)
                                leftTime = keyframeTime.Key;

                        leftTime = leftTime == float.MinValue ? TimelineCopyPaste.CurrentRunningTime : leftTime;
                        TimelineCopyPaste.LastKey = false;

                        var context = new CurvesContext(clipCurveWrapper, TimelineUtility.TimeToFrame(leftTime), state);
                        if (SnapScrubberEvent != null)
                        {
                            SnapScrubberEvent(this, new CurveClipScrubberEventArgs(Wrapper.timelineItem, context.frameNumber));
                            context.state.IsInPreviewMode = true;
                            context.state.position = context.frameNumber/30.0f;
                        }
                    }

                    if (TimelineCopyPaste.NextKey)
                    {
                        var rightTime = float.MaxValue;
                        foreach (var keyframeTime in keyframeTimes)
                        {
                            if (keyframeTime.Key > TimelineCopyPaste.CurrentRunningTime && keyframeTime.Key < rightTime)
                                rightTime = keyframeTime.Key;
                        }
                        rightTime = rightTime == float.MaxValue ? TimelineCopyPaste.CurrentRunningTime : rightTime;
                        TimelineCopyPaste.NextKey = false;

                        var context = new CurvesContext(clipCurveWrapper, TimelineUtility.TimeToFrame(rightTime), state);
                        if (SnapScrubberEvent != null)
                        {
                            SnapScrubberEvent(this, new CurveClipScrubberEventArgs(Wrapper.timelineItem, context.frameNumber));
                            context.state.IsInPreviewMode = true;
                            context.state.position = context.frameNumber/30.0f;
                        }
                    }
                }
        }

        private void DrawCurve(TimelineAnimationCurveWrapper wrapper)
        {
            for (var i = 0; i < wrapper.KeyframeCount - 1; i++)
            {
                var cur = wrapper.GetKeyframeWrapper(i);
                var next = wrapper.GetKeyframeWrapper(i + 1);
                var curPos = cur.ScreenPosition;
                var nextPos = next.ScreenPosition;
                var curOutTangent = cur.OutTangentControlPointPosition;
                var nextInTangent = next.InTangentControlPointPosition;
                if (float.IsPositiveInfinity(wrapper.GetKeyframe(i).outTangent) || float.IsPositiveInfinity(wrapper.GetKeyframe(i + 1).inTangent))
                {
                    Handles.DrawBezier(new Vector3(curPos.x, curPos.y), new Vector3(nextPos.x, curPos.y), new Vector3(nextPos.x, cur.ScreenPosition.y), new Vector3(cur.ScreenPosition.x, curPos.y), wrapper.Color, null, 2f);
                    Handles.DrawBezier(new Vector3(nextPos.x, curPos.y), new Vector3(nextPos.x, nextPos.y), new Vector3(next.ScreenPosition.x, next.ScreenPosition.y), new Vector3(next.ScreenPosition.x, curPos.y), wrapper.Color, null, 2f);
                }
                else
                {
                    Handles.DrawBezier(new Vector3(curPos.x, curPos.y), new Vector3(nextPos.x, nextPos.y), new Vector3(curOutTangent.x, curOutTangent.y), new Vector3(nextInTangent.x, nextInTangent.y), wrapper.Color, null, 2f);
                }
            }
        }

        private void DrawCurveCanvas(TimelineClipCurveWrapper clipCurveWrapper)
        {
            GUI.Box(curveCanvasPosition, GUIContent.none, TimelineTrackControl.styles.curveCanvasStyle);
            foreach (var wrapper in clipCurveWrapper.MemberCurves)
            {
                foreach (var wrapper2 in wrapper.AnimationCurves)
                {
                    if (wrapper.IsVisible && wrapper2.IsVisible)
                    {
                        if (timelineControl.ShowCurves)
                        {
                            DrawCurve(wrapper2);
                        }
                        DrawKeyframes(wrapper, wrapper2);
                    }
                }
            }
        }

        private void DrawCurveItem()
        {
            if (Wrapper.timelineItem != null)
            {
                if (Parent.isExpanded)
                {
                    if (IsSelected)
                    {
                        GUI.Box(m_TimelineItemPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemSelectedStyle);
                    }
                    else
                    {
                        GUI.Box(m_TimelineItemPosition, GUIContent.none, TimelineTrackControl.styles.TrackItemStyle);
                    }
                }
                else if (IsSelected)
                {
                    GUI.Box(m_TimelineItemPosition, GUIContent.none, TimelineTrackControl.styles.CurveTrackItemSelectedStyle);
                }
                else
                {
                    GUI.Box(m_TimelineItemPosition, GUIContent.none, TimelineTrackControl.styles.CurveTrackItemStyle);
                }
                var tempColor = GUI.color;
                GUI.color = new Color(0.71f, 0.14f, 0.8f);
                var timelineItemPosition = this.m_TimelineItemPosition;
                timelineItemPosition.x += 4f;
                timelineItemPosition.width = 16f;
                timelineItemPosition.height = 16f;
                GUI.Box(timelineItemPosition, actionIcon, GUIStyle.none);
                GUI.color = tempColor;
            }
        }

        private bool FindSelection(TimelineMemberCurveWrapper memberWrapper, int i, int id, int frame)
        {
            return m_Selections.Find(e => e.MemberCurveWrapper == memberWrapper && e.KeyId == i && e.CurveId == id) != null;
        }

        private bool FindMasterSelection(int frame)
        {
            return m_Selections.Find(e => e.Keyframe.GetFrame() == frame) != null;
        }

        private void AddSelection(TimelineMemberCurveWrapper memberWrapper, int i, int id, int frame)
        {
            var selection = new TimelineCurveSelection(id, i, memberWrapper);
            m_Selections.Add(selection);
        }

        private void AddMasterSelection(int frame)
        {
            var clipCurveWrapper = Wrapper as TimelineClipCurveWrapper;
            foreach (var timelineMemberCurveWrapper in clipCurveWrapper.MemberCurves)
            {
                foreach (var timelineAnimationCurveWrapper in timelineMemberCurveWrapper.AnimationCurves)
                {
                    for (int i = 0; i < timelineAnimationCurveWrapper.KeyframeCount; i++)
                    {
                        var keyframe = timelineAnimationCurveWrapper.GetKeyframe(i);
                        if (frame == keyframe.GetFrame())
                        {
                            m_Selections.Add(new TimelineCurveSelection(timelineAnimationCurveWrapper.Id, i, timelineMemberCurveWrapper));
                            break;
                        }
                    }
                }
            }
        }

        private void ClearAndAddSelection(TimelineMemberCurveWrapper memberCurveWrapper, int i, int id, int frame)
        {
            m_Selections.Clear();
            AddSelection(memberCurveWrapper,i,id, frame);
        }

        private void ClearAndAddMasterSelection(int frame)
        {
            m_Selections.Clear();
            AddMasterSelection(frame);
        }

        private void DrawKeyframes(TimelineMemberCurveWrapper memberWrapper, TimelineAnimationCurveWrapper wrapper)
        {
            for (var i = 0; i < wrapper.KeyframeCount; i++)
            {
                var keyframeWrapper = wrapper.GetKeyframeWrapper(i);
                var color = GUI.color;
                var found = FindSelection(memberWrapper, i, wrapper.Id, wrapper.GetKeyframe(i).GetFrame());
                if (timelineControl.ShowCurves && found)
                {
                    if (i > 0 && !wrapper.IsAuto(i) && !wrapper.IsLeftLinear(i) && !wrapper.IsLeftConstant(i))
                    {
                        var vector = new Vector2(keyframeWrapper.InTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.InTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
                        vector.Normalize();
                        vector *= 30f;
                        Handles.color = Color.gray;
                        Handles.DrawLine(new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y, 0f), new Vector3(keyframeWrapper.ScreenPosition.x + vector.x, keyframeWrapper.ScreenPosition.y + vector.y, 0f));
                        GUI.Label(new Rect(keyframeWrapper.ScreenPosition.x + vector.x - 4f, keyframeWrapper.ScreenPosition.y + vector.y - 4f, 8f, 8f), string.Empty, TimelineTrackControl.styles.tangentStyle);
                    }
                    if (i < wrapper.KeyframeCount - 1 && !wrapper.IsAuto(i) && !wrapper.IsRightLinear(i) && !wrapper.IsRightConstant(i))
                    {
                        var vector2 = new Vector2(keyframeWrapper.OutTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.OutTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
                        vector2.Normalize();
                        vector2 *= 30f;
                        Handles.color = Color.gray;
                        Handles.DrawLine(new Vector3(keyframeWrapper.ScreenPosition.x, keyframeWrapper.ScreenPosition.y, 0f), new Vector3(keyframeWrapper.ScreenPosition.x + vector2.x, keyframeWrapper.ScreenPosition.y + vector2.y, 0f));
                        GUI.Label(new Rect(keyframeWrapper.ScreenPosition.x + vector2.x - 4f, keyframeWrapper.ScreenPosition.y + vector2.y - 4f, 8f, 8f), string.Empty, TimelineTrackControl.styles.tangentStyle);
                    }
                    GUI.color = wrapper.Color;
                }
                if (found)
                    GUI.color = new Color(0.5f, 0.6f, 0.905f, 1f);
                GUI.Label(new Rect(keyframeWrapper.ScreenPosition.x - 4f, keyframeWrapper.ScreenPosition.y - 4f, 8f, 8f), string.Empty, TimelineTrackControl.styles.keyframeStyle);
                GUI.color = color;
            }
        }

        private void DrawMasterKeys(TimelineControlState state)
        {
            for (var i = 0; i < keyframeTimes.Count; i++)
            {
                var distance = TimelineUtility.FrameToTime(keyframeTimes.Keys[i]) * state.Scale.x + state.Translation.x;
                var color = GUI.color;
                GUI.color = FindMasterSelection(keyframeTimes.Keys[i]) ? new Color(0.5f, 0.6f, 0.905f, 1f) : color;
                GUI.Label(new Rect(distance - 4f, m_MasterKeysPosition.y, 8f, 8f), string.Empty, TimelineTrackControl.styles.keyframeStyle);
                GUI.color = color;
            }
        }

        private Rect GetViewingArea(bool isResizeEnabled, TimelineClipCurveWrapper clipCurveWrapper)
        {
            var rect = new Rect(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
            foreach (var wrapper in clipCurveWrapper.MemberCurves)
            {
                if (wrapper.IsVisible)
                {
                    foreach (var wrapper2 in wrapper.AnimationCurves)
                    {
                        if (wrapper2.IsVisible)
                        {
                            for (var i = 0; i < wrapper2.KeyframeCount; i++)
                            {
                                var keyframe = wrapper2.GetKeyframe(i);
                                rect.x = Mathf.Min(rect.x, keyframe.time);
                                rect.width = Mathf.Max(rect.width, keyframe.time);
                                rect.y = Mathf.Min(rect.y, keyframe.value);
                                rect.height = Mathf.Max(rect.height, keyframe.value);
                                if (i > 0)
                                {
                                    var keyframe2 = wrapper2.GetKeyframe(i - 1);
                                    var num4 = Mathf.Abs(keyframe.time - keyframe2.time) * 0.333333f;
                                    var num5 = wrapper2.Evaluate(keyframe2.time + num4);
                                    var num6 = wrapper2.Evaluate(keyframe.time - num4);
                                    var num7 = wrapper2.Evaluate(keyframe2.time + Mathf.Abs(keyframe.time - keyframe2.time) * 0.5f);
                                    float[] values = { rect.y, num5, num6, num7 };
                                    rect.y = Mathf.Min(values);
                                    float[] singleArray2 = { rect.height, num5, num6, num7 };
                                    rect.height = Mathf.Max(singleArray2);
                                }
                            }
                        }
                    }
                }
            }

            if (rect.height - rect.y == 0f)
            {
                rect.y += 0f;
                rect.height = rect.y + 1f;
            }

            if (!isResizeEnabled)
            {
                rect.y = m_ViewingSpace.y;
                rect.height = m_ViewingSpace.height;
            }

            return rect;
        }

        private void HandleCurveCanvasInput(TimelineClipCurveWrapper clipCurveWrapper, TimelineControlState state)
        {
            var id = GUIUtility.GetControlID("CurveCanvas".GetHashCode(), FocusType.Passive);
            if (Event.current.GetTypeForControl(id) == EventType.MouseDown && curveCanvasPosition.Contains(Event.current.mousePosition) && Event.current.button == 1)
            {
                var time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                var context = new CurvesContext(clipCurveWrapper, TimelineUtility.TimeToFrame(time), state);
                m_MultiMenu = ShowKeyFramesAndCurveCanvasContextMenu(clipCurveWrapper);
                ShowCurveCanvasContextMenu(context);
                m_MultiMenu.ShowAsContext();
                Event.current.Use();
            }

            switch (Event.current.type)
            {
                case EventType.ValidateCommand:
                {
                    if (GUIUtility.hotControl == id && Event.current.commandName == "Copy" || Event.current.commandName == "Paste")
                    {
                        Event.current.Use();
                    }
                    break;
                }
                case EventType.ExecuteCommand:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        if (Event.current.commandName == "Copy")
                        {
                            CopyKeyframes(new CurvesContext(clipCurveWrapper, state.scrubberPositionFrame, state));
                            Event.current.Use();
                        }
                        else if (Event.current.commandName == "Copy")
                        {
                            PasteKeyframes(new CurvesContext(clipCurveWrapper, state.scrubberPositionFrame, state));
                            Event.current.Use();
                        }
                    }
                    break;
                }
            }
        }

        private void Paste(CurvesContext context)
        {
            var memberCurves = context.wrapper.MemberCurves;
            var targetMemberCurves = TimelineCopyPaste.copyFrameContext.wrapper.MemberCurves;
            if (targetMemberCurves.Count == memberCurves.Count)
            {
                for (var i = 0; i < memberCurves.Count; i++)
                {
                    var memberCurve = memberCurves[i];
                    var targetCurve = targetMemberCurves[i];
                    if (memberCurve.AnimationCurves.Count != targetCurve.AnimationCurves.Count)
                        break;
                    for (var j = 0; j < memberCurve.AnimationCurves.Count; j++)
                    {
                        var wrapper = memberCurve.AnimationCurves[j];
                        var targetWrapper = targetCurve.AnimationCurves[j];
                        wrapper.AddKey(context.frameNumber, targetWrapper.Evaluate(TimelineUtility.FrameToTime(TimelineCopyPaste.copyFrameContext.frameNumber)));
                    }
                }
            }
            curvesChanged = true;
        }

        private void HandlePasteKeyFrames(TimelineClipCurveWrapper clipCurveWrapper, TimelineControlState state)
        {
            if (!TimelineCopyPaste.pasteKeyFrames)
            {
                return;
            }

            var context = new CurvesContext(clipCurveWrapper, TimelineCopyPaste.pasteFrame, state);
            Paste(context);

            TimelineCopyPaste.pasteKeyFrames = false;
        }

        public override void ZoomSelectKey(Rect selectionBox)
        {
            var clipCurveWrapper = Wrapper as TimelineClipCurveWrapper;
            if (clipCurveWrapper != null)
            {
                m_Selections.Clear();
                var found = false;
                for (var i = 0; i < keyframeTimes.Count; i++)
                {
                    var distance = TimelineUtility.FrameToTime(keyframeTimes.Keys[i]) * ControlState.Scale.x + ControlState.Translation.x;
                    if (selectionBox.Contains(new Vector2(distance, m_MasterKeysPosition.y)))
                    {
                        found = true;
                        AddMasterSelection(keyframeTimes.Keys[i]);
                    }
                }

                if (found)
                {
                    foreach (var memberCurveWrapper in clipCurveWrapper.MemberCurves)
                    {
                        if (memberCurveWrapper.IsVisible)
                        {
                            foreach (var animationCurveWrapper in memberCurveWrapper.AnimationCurves)
                            {
                                if (animationCurveWrapper.IsVisible)
                                {
                                    for (var i = 0; i < animationCurveWrapper.KeyframeCount; i++)
                                    {
                                        var keyframeScreenPosition = animationCurveWrapper.GetKeyframeScreenPosition(i);
                                        if (selectionBox.Contains(keyframeScreenPosition))
                                        {
                                            AddSelection(memberCurveWrapper,animationCurveWrapper.Id, i, animationCurveWrapper.GetKeyframe(i).GetFrame());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void HandleInput(TimelineControlState state, Rect trackPosition)
        {
            var clipCurveWrapper = Wrapper as TimelineClipCurveWrapper;
            if (clipCurveWrapper != null)
            {
                HandleItemInput(clipCurveWrapper);
                if (!isCurveClipEmpty && !isFolded)
                {
                    HandleKeyframeInput(clipCurveWrapper);
                    UpdateMasterKeys(clipCurveWrapper);
                    HandleMasterKeysInput(clipCurveWrapper, state);
                }
            }
        }

        private void HandleItemInput(TimelineClipCurveWrapper clipCurveWrapper)
        {
            if (isRenaming)
                return;
            var x = clipCurveWrapper.fireTime * ControlState.Scale.x + ControlState.Translation.x;
            var num2 = (clipCurveWrapper.fireTime + clipCurveWrapper.Duration) * ControlState.Scale.x + ControlState.Translation.x;
            var position = new Rect(x, 0f, 5f, m_TimelineItemPosition.height);
            var rect2 = new Rect(x + 5f, 0f, num2 - x - 10f, m_TimelineItemPosition.height);
            var rect3 = new Rect(num2 - 5f, 0f, 5f, m_TimelineItemPosition.height);
            EditorGUIUtility.AddCursorRect(position, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rect2, MouseCursor.SlideArrow);
            EditorGUIUtility.AddCursorRect(rect3, MouseCursor.ResizeHorizontal);
            this.controlID = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, m_TimelineItemPosition);
            var controlID = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, position);
            var num4 = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, rect2);
            var num5 = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, rect3);
            if (Event.current.GetTypeForControl(this.controlID) == EventType.MouseDown && rect2.Contains(Event.current.mousePosition) && Event.current.button == 1)
            {
                if (!IsSelected)
                {
                    var gameObjects = Selection.gameObjects;
                    ArrayUtility.Add(ref gameObjects, Wrapper.timelineItem.gameObject);
                    Selection.objects = gameObjects;
                    hasSelectionChanged = true;
                }
                ShowContextMenu(Wrapper.timelineItem);
                Event.current.Use();

            }
            switch (Event.current.GetTypeForControl(num4))
            {
                case EventType.MouseDown:
                {
                    if (!rect2.Contains(Event.current.mousePosition) || Event.current.button != 0) goto Label_0497;
                    GUIUtility.hotControl = num4;
                    if (!Event.current.control)
                    {
                        if (!IsSelected)
                        {
                            Selection.activeInstanceID = Wrapper.timelineItem.GetInstanceID();
                        }
                        break;
                    }

                    if (!IsSelected)
                    {
                        var array = Selection.gameObjects;
                        ArrayUtility.Add(ref array, Wrapper.timelineItem.gameObject);
                        Selection.objects = array;
                        hasSelectionChanged = true;
                        break;
                    }

                    var gameObjects = Selection.gameObjects;
                    ArrayUtility.Remove(ref gameObjects, Wrapper.timelineItem.gameObject);
                    Selection.objects = gameObjects;
                    hasSelectionChanged = true;
                    break;
                }
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == num4)
                    {
                        ms_MouseDownOffset = -1f;
                        GUIUtility.hotControl = 0;
                        if (mouseDragActivity)
                        {
                            TriggerTrackItemUpdateEvent();
                        }
                        else if (!Event.current.control)
                        {
                            Selection.activeInstanceID = Wrapper.timelineItem.GetInstanceID();
                        }
                        else if (!hasSelectionChanged)
                        {
                            if (!IsSelected)
                            {
                                var gameObjects = Selection.gameObjects;
                                ArrayUtility.Add(ref gameObjects, Wrapper.timelineItem.gameObject);
                                Selection.objects = gameObjects;
                            }
                            else
                            {
                                var gameObjects = Selection.gameObjects;
                                ArrayUtility.Remove(ref gameObjects, Wrapper.timelineItem.gameObject);
                                Selection.objects = gameObjects;
                            }
                        }

                        hasSelectionChanged = false;
                    }

                    goto Label_0497;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == num4 && !hasSelectionChanged)
                    {
                        Undo.RecordObject(Wrapper.timelineItem, $"Changed {Wrapper.timelineItem.name}");
                        var fireTime = (Event.current.mousePosition.x - ControlState.Translation.x) / ControlState.Scale.x;
                        fireTime -= ms_MouseDownOffset;
                        if (!mouseDragActivity)
                            mouseDragActivity = !(Wrapper.fireTime == fireTime);
                        TriggerRequestTrackItemTranslate(fireTime);
                    }
                    goto Label_0497;
                default:
                    goto Label_0497;
            }

            mouseDragActivity = false;
            ms_MouseDownOffset = (Event.current.mousePosition.x - ControlState.Translation.x) / ControlState.Scale.x - clipCurveWrapper.fireTime;
            Event.current.Use();

        Label_0497:
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;
                        ms_MouseDownOffset = (Event.current.mousePosition.x - ControlState.Translation.x) / ControlState.Scale.x - clipCurveWrapper.fireTime;
                        Event.current.Use();

                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        ms_MouseDownOffset = -1f;
                        GUIUtility.hotControl = 0;
                        curvesChanged = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        var time = (Event.current.mousePosition.x - ControlState.Translation.x) / ControlState.Scale.x;
                        var a = 0f;
                        var num9 = clipCurveWrapper.fireTime + clipCurveWrapper.Duration;
                        foreach (TimelineActionWrapper wrapper3 in Parent.Wrapper.Children)
                            if (wrapper3 != null && wrapper3.timelineItem != Wrapper.timelineItem)
                            {
                                var b = wrapper3.fireTime + wrapper3.Duration;
                                if (b <= Wrapper.fireTime) a = Mathf.Max(a, b);
                            }
                        time = Mathf.Max(a, time);
                        time = Mathf.Min(num9, time);
                        if (ControlState.ResizeOption == ResizeOption.Crop)
                            clipCurveWrapper.CropFireTime(time);
                        else if (ControlState.ResizeOption == ResizeOption.Scale) clipCurveWrapper.ScaleFiretime(time);
                    }
                    break;
            }

            switch (Event.current.GetTypeForControl(num5))
            {
                case EventType.MouseDown:
                    if (rect3.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = num5;
                        ms_MouseDownOffset = (Event.current.mousePosition.x - ControlState.Translation.x) / ControlState.Scale.x - Wrapper.fireTime;
                        Event.current.Use();

                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == num5)
                    {
                        ms_MouseDownOffset = -1f;
                        GUIUtility.hotControl = 0;
                        curvesChanged = true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == num5)
                    {
                        var time = (Event.current.mousePosition.x - ControlState.Translation.x) / ControlState.Scale.x;
                        var positiveInfinity = float.PositiveInfinity;
                        foreach (TimelineActionWrapper wrapper5 in Parent.Wrapper.Children)
                            if (wrapper5 != null && wrapper5.timelineItem != Wrapper.timelineItem)
                            {
                                var num13 = clipCurveWrapper.fireTime + clipCurveWrapper.Duration;
                                if (wrapper5.fireTime >= num13)
                                    positiveInfinity = Mathf.Min(positiveInfinity, wrapper5.fireTime);
                            }

                        time = Mathf.Clamp(time, Wrapper.fireTime, positiveInfinity);
                        if (ControlState.ResizeOption == ResizeOption.Crop)
                            clipCurveWrapper.CropDuration(time - Wrapper.fireTime);
                        else if (ControlState.ResizeOption == ResizeOption.Scale)
                            clipCurveWrapper.ScaleDuration(time - Wrapper.fireTime);
                    }

                    break;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && Selection.activeGameObject == Wrapper.timelineItem.gameObject)
            {
                DeleteItem(Wrapper.timelineItem);
                Event.current.Use();
            }
        }

        public void ClearSelectedCurves()
        {
            m_Selections.Clear();
        }

        private void HandleKeyframeInput(TimelineClipCurveWrapper clipCurveWrapper)
        {
            var current = Event.current;
            var verticalRange = m_ViewingSpace.height - m_ViewingSpace.y;
            var control = GUIUtility.GetControlID("KeyframeControl".GetHashCode(), FocusType.Passive);
            foreach (var memberCurve in clipCurveWrapper.MemberCurves)
            {
                foreach (var animationCurve in memberCurve.AnimationCurves)
                {
                    if (animationCurve.IsVisible)
                    {
                        for (var i = 0; i < animationCurve.KeyframeCount; i++)
                        {
                            var keyframe = animationCurve.GetKeyframe(i);
                            var frame = keyframe.GetFrame();
                            var found = FindSelection(memberCurve, i, animationCurve.Id, frame);
                            var isBookEnd = frame == clipCurveWrapper.FireFrameNumber || frame == clipCurveWrapper.endFrame;
                            var keyframeScreenPosition = animationCurve.GetKeyframeScreenPosition(i);
                            var rect = new Rect(keyframeScreenPosition.x - 4f, keyframeScreenPosition.y - 4f, 8f, 8f);
                            switch (current.GetTypeForControl(control))
                            {
                                case EventType.MouseDown:
                                {
                                    if (rect.Contains(current.mousePosition))
                                    {
                                        GUIUtility.hotControl = control;
                                        Selection.activeInstanceID = Wrapper.timelineItem.GetInstanceID();
                                        if (!found)
                                        {
                                            if (current.control && !isBookEnd)
                                                AddSelection(memberCurve, i, animationCurve.Id, frame);
                                            else
                                            {
                                                ClearAndAddSelection(memberCurve, i, animationCurve.Id, frame);
                                            }
                                        }
                                        current.Use();
                                        m_CacheMousePosition = TimelineUtility.TimeToFrame(current.mousePosition.x / ControlState.Scale.x);
                                    }
                                    continue;
                                }
                                case EventType.MouseUp:
                                {
                                    if (GUIUtility.hotControl != control)
                                        break;
                                    if (found)
                                    {
                                        if (current.button == 1)
                                        {
                                            if (m_Selections.Count > 0)
                                            {
                                                var menu = ShowKeyFramesAndCurveCanvasContextMenu(clipCurveWrapper);
                                                menu.ShowAsContext();
                                            }
                                            else
                                            {
                                                ShowKeyframeContextMenu(animationCurve, i, isBookEnd);
                                            }
                                            GUIUtility.hotControl = 0;
                                            current.Use();
                                        }
                                        else if (current.button == 0)
                                        {
                                            HandleKeyframeInputUp(clipCurveWrapper);
                                        }
                                    }
                                    break;
                                }
                                case EventType.MouseDrag:
                                    {
                                        if (GUIUtility.hotControl == control && current.button == 0 && found)
                                        {
                                            MoveKeyframe(clipCurveWrapper);
                                            return;
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            HandleKeyframeTangentInput(clipCurveWrapper, verticalRange);
        }

        private void MoveKeyframe(TimelineClipCurveWrapper clipCurveWrapper)
        {
            var offset = MousePositionOffsetFrame;
            var verticalRange = m_ViewingSpace.height - m_ViewingSpace.y;
            var offsetY = timelineControl.ShowCurves ? Event.current.delta.y / curveTrackSafeArea.height * verticalRange : 0;
            foreach (var selection in m_Selections)
            {
                int frame;
                var keyframe = selection.Keyframe;
                var isBookEnd = keyframe.GetFrame() == clipCurveWrapper.FireFrameNumber || keyframe.GetFrame() == clipCurveWrapper.endFrame;
                if (isBookEnd)
                    frame = keyframe.GetFrame();
                else
                {
                    frame = keyframe.GetFrame() + offset;
                }
                var kf = new Keyframe(TimelineUtility.FrameToTime(frame), keyframe.value - offsetY, keyframe.inTangent, keyframe.outTangent);
                if ((frame > clipCurveWrapper.FireFrameNumber && frame < clipCurveWrapper.endFrame) || isBookEnd)
                {
                    selection.KeyId = selection.AnimationCurveWrapper.MoveKey(selection.KeyId, kf);
                    curvesChanged = true;
                }
            }
        }

        private void HandleKeyframeInputUp(TimelineClipCurveWrapper clipCurveWrapper)
        {
            var offset = MousePositionOffsetFrame;
            var verticalRange = m_ViewingSpace.height - m_ViewingSpace.y;
            var offsetY = Event.current.delta.y / curveTrackSafeArea.height * verticalRange;
            foreach (var selection in m_Selections)
            {
                int f;
                var keyframe = selection.Keyframe;
                int frame = keyframe.GetFrame();
                var isBookEnd = frame == clipCurveWrapper.FireFrameNumber || frame == clipCurveWrapper.endFrame;
                if (isBookEnd)
                    f = frame;
                else
                {
                    f = frame + offset;
                }

                var kf = new Keyframe(TimelineUtility.FrameToTime(f), keyframe.value - offsetY, keyframe.inTangent, keyframe.outTangent);
                if ((f > clipCurveWrapper.FireFrameNumber && f < clipCurveWrapper.endFrame) || isBookEnd)
                {
                    selection.KeyId = selection.AnimationCurveWrapper.MoveKey(selection.KeyId, kf);
                    curvesChanged = true;
                }
            }
        }

        private void HandleKeyframeTangentInput(TimelineClipCurveWrapper clipCurveWrapper, float verticalRange)
        {
            foreach (var memberWrapper in clipCurveWrapper.MemberCurves)
            {
                foreach (var curveWrapper in memberWrapper.AnimationCurves)
                {
                    for (var i = 0; i < curveWrapper.KeyframeCount; i++)
                    {
                        var keyframe = curveWrapper.GetKeyframe(i);
                        var keyframeWrapper = curveWrapper.GetKeyframeWrapper(i);
                        if (!FindSelection(memberWrapper, i, curveWrapper.Id, keyframe.GetFrame()) || curveWrapper.IsAuto(i))
                            continue;
                        if (i > 0 && !curveWrapper.IsLeftLinear(i) && !curveWrapper.IsLeftConstant(i))
                        {
                            var vector = new Vector2(keyframeWrapper.InTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.InTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
                            vector.Normalize();
                            vector *= 30f;
                            var rect = new Rect(keyframeWrapper.ScreenPosition.x + vector.x - 4f, keyframeWrapper.ScreenPosition.y + vector.y - 4f, 8f, 8f);
                            controlID = GUIUtility.GetControlID("TangentIn".GetHashCode(), FocusType.Passive);
                            var current = Event.current;
                            switch (current.GetTypeForControl(controlID))
                            {
                                case EventType.MouseDown:
                                    if (rect.Contains(current.mousePosition))
                                    {
                                        m_CacheMousePosition = TimelineUtility.TimeToFrame(current.mousePosition.x / ControlState.Scale.x);
                                        GUIUtility.hotControl = controlID;
                                            current.Use();
                                    }
                                    break;
                                case EventType.MouseUp:
                                    HandleKeyframeTangentInput5(i, curveWrapper,keyframeWrapper, verticalRange,keyframe);
                                    continue;
                                case EventType.MouseDrag:
                                    if (HandleKeyframeTangentInputDrag(i, verticalRange, keyframe, curveWrapper, keyframeWrapper, current.mousePosition))
                                    {
                                        return;
                                    }
                                    continue;
                            }
                        }
                        if (HandleKeyframeTangentInput3(i, curveWrapper, keyframeWrapper, verticalRange, keyframe))
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void HandleKeyframeTangentInput5(int i,TimelineAnimationCurveWrapper wrapper2,TimelineKeyframeWrapper keyframeWrapper, float verticalRange,Keyframe keyframe)
        {
            HandleKeyframeTangentInput2();
            HandleKeyframeTangentInput3(i, wrapper2,keyframeWrapper, verticalRange,keyframe);
        }

        private bool HandleKeyframeTangentInputDrag(int i,float verticalRange,Keyframe keyframe,TimelineAnimationCurveWrapper curveWrapper,TimelineKeyframeWrapper keyframeWrapper, Vector2 mousePosition)
        {
            if (GUIUtility.hotControl == controlID)
            {
                var inTangent = GetTangentInValue(keyframeWrapper.ScreenPosition, mousePosition, curveWrapper, i);
                var kf = new Keyframe(keyframe.time, keyframe.value, inTangent, inTangent)
                {
                    tangentMode = keyframe.tangentMode
                };
                curveWrapper.MoveKey(i, kf);

                curvesChanged = true;
                return true;
            }
            HandleKeyframeTangentInput3(i, curveWrapper,keyframeWrapper, verticalRange,keyframe);
            return false;
        }

        private bool HandleKeyframeTangentInput3(int i,TimelineAnimationCurveWrapper animationCurveWrapper,TimelineKeyframeWrapper keyframeWrapper, float verticalRange,Keyframe keyframe)
        {
            if (i < animationCurveWrapper.KeyframeCount - 1 && !animationCurveWrapper.IsRightLinear(i) && !animationCurveWrapper.IsRightConstant(i))
            {
                var vector2 = new Vector2(keyframeWrapper.OutTangentControlPointPosition.x - keyframeWrapper.ScreenPosition.x, keyframeWrapper.OutTangentControlPointPosition.y - keyframeWrapper.ScreenPosition.y);
                vector2.Normalize();
                vector2 *= 30f;
                var rect2 = new Rect(keyframeWrapper.ScreenPosition.x + vector2.x - 4f, keyframeWrapper.ScreenPosition.y + vector2.y - 4f, 8f, 8f);
                controlID = GUIUtility.GetControlID("TangentOut".GetHashCode(), FocusType.Passive);
                var current = Event.current;
                switch (current.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        if (rect2.Contains(current.mousePosition))
                        {
                            GUIUtility.hotControl = controlID;
                                current.Use();
                            m_CacheMousePosition = TimelineUtility.TimeToFrame(current.mousePosition.x / ControlState.Scale.x);
                        }
                        break;
                    case EventType.MouseUp:
                        HandleKeyframeTangentInput2();
                        break;
                    case EventType.MouseDrag:
                        return HandleKeyframeTangentInput1(keyframeWrapper, current.mousePosition, keyframe, animationCurveWrapper, i);
                }
            }

            return false;
        }

        private void HandleKeyframeTangentInput2()
        {
            if (GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                curvesChanged = true;
            }
        }

        private bool HandleKeyframeTangentInput1(TimelineKeyframeWrapper keyframeWrapper, Vector2 mousePosition, Keyframe keyframe, TimelineAnimationCurveWrapper animationCurveWrapper, int index)
        {
            if (GUIUtility.hotControl == controlID)
            {
                var inTangent = GetTangentOutValue(keyframeWrapper.ScreenPosition, mousePosition, animationCurveWrapper, index);
                var kf = new Keyframe(keyframe.time, keyframe.value, inTangent, inTangent)
                {
                    tangentMode = keyframe.tangentMode
                };
                animationCurveWrapper.MoveKey(index, kf);
                curvesChanged = true;
                return true;
            }

            return false;
        }

        private void HandleMasterKeysInput(TimelineClipCurveWrapper clipCurveWrapper, TimelineControlState state)
        {
            for (var i = 0; i < keyframeTimes.Count; i++)
            {
                var frameNumber = keyframeTimes.Keys[i];
                var num3 = TimelineUtility.FrameToTime(keyframeTimes.Keys[i]) * state.Scale.x + state.Translation.x;
                var rect = new Rect(num3 - 4f, m_MasterKeysPosition.y, 8f, 8f);
                var control = keyframeTimes.Values[i];
                var flag2 = frameNumber == clipCurveWrapper.FireFrameNumber || frameNumber == clipCurveWrapper.endFrame;
                var isDeletable = !flag2 && keyframeTimes.Count > 2;
                switch (Event.current.GetTypeForControl(control))
                {
                    case EventType.MouseDown:
                    {
                        if (!rect.Contains(Event.current.mousePosition))
                            break;
                        if (Event.current.button == 0)
                        {
                            if (!FindMasterSelection(frameNumber))
                            {
                                ClearAndAddMasterSelection(frameNumber);
                            }
                            timelineControl.Repaint();
                            Event.current.Use();
                            GUIUtility.hotControl = control;
                            m_CacheMousePosition = TimelineUtility.TimeToFrame(Event.current.mousePosition.x / ControlState.Scale.x);
                        }
                        else if (Event.current.button == 1)
                        {
                            var context = new CurvesContext(clipCurveWrapper, frameNumber, state);
                            ShowMasterKeyContextMenu(context, isDeletable);
                            Event.current.Use();
                        }
                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == control)
                        {
                            GUIUtility.hotControl = 0;
                            curvesChanged = true;
                        }
                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl != control || Event.current.button != 0 || flag2)
                            continue;
                        MoveKeyframe(clipCurveWrapper);
                        break;
                    }
                }
            }
        }

        public override void PreUpdate(TimelineControlState state, Rect trackPosition)
        {
            var clipWrapper = Wrapper as TimelineClipCurveWrapper;
            if (clipWrapper != null)
            {
                isCurveClipEmpty = clipWrapper.IsEmpty;
                isFolded = trackPosition.height.Equals(17f);
                if (GUIUtility.hotControl == 0)
                {
                    UpdateCurveWrappers(clipWrapper);
                    m_ViewingSpace = GetViewingArea(true, clipWrapper);
                }
                var x = clipWrapper.fireTime * state.Scale.x + state.Translation.x;
                var clipWrapperDuration = (clipWrapper.fireTime + clipWrapper.Duration) * state.Scale.x + state.Translation.x;
                ControlPosition = new Rect(x, 0f, clipWrapperDuration - x, trackPosition.height);
                if (isCurveClipEmpty || isFolded)
                    m_TimelineItemPosition = ControlPosition;
                else
                    m_TimelineItemPosition = new Rect(ControlPosition.x, ControlPosition.y, ControlPosition.width, 17f);
                m_MasterKeysPosition = new Rect(ControlPosition.x, m_TimelineItemPosition.y + m_TimelineItemPosition.height + 4, ControlPosition.width, 17f);
                curveCanvasPosition = new Rect(ControlPosition.x, m_MasterKeysPosition.y + m_MasterKeysPosition.height - 4, ControlPosition.width, trackPosition.height - m_TimelineItemPosition.height - m_MasterKeysPosition.height);
                curveTrackSafeArea = new Rect(curveCanvasPosition.x, curveCanvasPosition.y + 8f, curveCanvasPosition.width, curveCanvasPosition.height - 16f);
                if (!isCurveClipEmpty)
                    UpdateKeyframes(clipWrapper, state);
            }
        }

        private void DeleteKeysBySelected(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                contexts.Sort((x, y) =>
                {
                    if (x.key < y.key)
                        return 1;
                    if (x.key > y.key)
                        return -1;
                    return 0;
                });

                foreach (var context in contexts)
                    context.curveWrapper.RemoveKey(context.key);
                curvesChanged = true;
                ClearSelectedCurves();
            }
        }

        private void SetKeyClampedAuto(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChanged = false;
                foreach (var tempContext in contexts)
                {
                    if (tempContext != null && !tempContext.curveWrapper.IsClampedAuto(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyClampedAuto(tempContext.key);
                        isChanged = true;
                    }
                }
                if (isChanged)
                    curvesChanged = true;
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsClampedAuto(context.key))
            {
                context.curveWrapper.SetKeyClampedAuto(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyAuto(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                {
                    if (tempContext != null && !tempContext.curveWrapper.IsAuto(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyAuto(tempContext.key);
                        isChange = true;
                    }
                }
                if (isChange)
                    curvesChanged = true;
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsAuto(context.key))
            {
                context.curveWrapper.SetKeyAuto(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyBothConstant(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && (!tempContext.curveWrapper.IsRightConstant(tempContext.key) || !tempContext.curveWrapper.IsLeftConstant(tempContext.key)))
                    {
                        tempContext.curveWrapper.SetKeyLeftConstant(tempContext.key);
                        tempContext.curveWrapper.SetKeyRightConstant(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                    curvesChanged = true;
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && (!context.curveWrapper.IsRightConstant(context.key) || !context.curveWrapper.IsLeftConstant(context.key)))
            {
                context.curveWrapper.SetKeyLeftConstant(context.key);
                context.curveWrapper.SetKeyRightConstant(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyBothFree(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && (!tempContext.curveWrapper.IsRightFree(tempContext.key) || !tempContext.curveWrapper.IsLeftFree(tempContext.key)))
                    {
                        tempContext.curveWrapper.SetKeyLeftLinear(tempContext.key);
                        tempContext.curveWrapper.SetKeyRightLinear(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                    curvesChanged = true;
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && (!context.curveWrapper.IsRightFree(context.key) || !context.curveWrapper.IsLeftFree(context.key)))
            {
                context.curveWrapper.SetKeyLeftFree(context.key);
                context.curveWrapper.SetKeyRightFree(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyBothLinear(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && (!tempContext.curveWrapper.IsRightLinear(tempContext.key) ||
                                                !tempContext.curveWrapper.IsLeftLinear(tempContext.key)))
                    {
                        tempContext.curveWrapper.SetKeyLeftLinear(tempContext.key);
                        tempContext.curveWrapper.SetKeyRightLinear(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                    curvesChanged = true;
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && (!context.curveWrapper.IsRightLinear(context.key) || !context.curveWrapper.IsLeftLinear(context.key)))
            {
                context.curveWrapper.SetKeyLeftLinear(context.key);
                context.curveWrapper.SetKeyRightLinear(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyBroken(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsRightFree(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyBroken(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsRightFree(context.key))
            {
                context.curveWrapper.SetKeyBroken(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyLeftConstant(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsLeftConstant(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyLeftConstant(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsLeftConstant(context.key))
            {
                context.curveWrapper.SetKeyLeftConstant(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyLeftFree(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsLeftFree(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyLeftFree(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }

            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsLeftFree(context.key))
            {
                context.curveWrapper.SetKeyLeftFree(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyLeftLinear(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsLeftLinear(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyLeftLinear(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }
            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsLeftLinear(context.key))
            {
                context.curveWrapper.SetKeyLeftLinear(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyRightConstant(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsRightConstant(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyRightConstant(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }
            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsRightConstant(context.key))
            {
                context.curveWrapper.SetKeyRightConstant(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyRightFree(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var IsChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsRightFree(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyRightFree(tempContext.key);
                        IsChange = true;
                    }

                if (IsChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }
            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsRightFree(context.key))
            {
                context.curveWrapper.SetKeyRightFree(context.key);
                curvesChanged = true;
            }
        }

        private void SetKeyRightLinear(object userData)
        {
            var contexts = userData as List<KeyframeContext>;
            if (contexts != null)
            {
                var isChange = false;
                foreach (var tempContext in contexts)
                    if (tempContext != null && !tempContext.curveWrapper.IsLeftLinear(tempContext.key))
                    {
                        tempContext.curveWrapper.SetKeyRightLinear(tempContext.key);
                        isChange = true;
                    }
                if (isChange)
                {
                    curvesChanged = true;
                }
                ClearSelectedCurves();
                return;
            }
            var context = userData as KeyframeContext;
            if (context != null && !context.curveWrapper.IsLeftLinear(context.key))
            {
                context.curveWrapper.SetKeyRightLinear(context.key);
                curvesChanged = true;
            }
        }

        private GenericMenu ShowKeyFramesAndCurveCanvasContextMenu(TimelineClipCurveWrapper clipCurveWrapper)
        {
            var menu = new GenericMenu();
            var isClampedAuto = true;
            var isAuto = true;
            var isBroken = true;
            var isLeftFree = true;
            var isLeftLinear = true;
            var isLeftConstant = true;
            var isRightFree = true;
            var isRightLinear = true;
            var isRightConstant = true;
            var bothTangentsFree = true;
            var bothTangentsLinear = true;
            var bothTangentsConstant = true;
            var keyframeContexts = new List<KeyframeContext>();
            foreach (var selection in m_Selections)
            {
                foreach (var memberCurve in clipCurveWrapper.MemberCurves)
                {
                    if (memberCurve == selection.MemberCurveWrapper)
                    {
                        isClampedAuto = isClampedAuto && memberCurve.AnimationCurves[selection.CurveId]
                            .IsClampedAuto(selection.KeyId);
                        isAuto = isAuto && memberCurve.AnimationCurves[selection.CurveId]
                            .IsAuto(selection.KeyId);
                        isBroken = isBroken && memberCurve.AnimationCurves[selection.CurveId]
                            .IsBroken(selection.KeyId);
                        isLeftFree = isLeftFree && memberCurve.AnimationCurves[selection.CurveId]
                            .IsLeftFree(selection.KeyId);
                        isLeftLinear = isLeftLinear && memberCurve.AnimationCurves[selection.CurveId]
                            .IsLeftLinear(selection.KeyId);
                        isLeftConstant = isLeftConstant && memberCurve.AnimationCurves[selection.CurveId]
                            .IsLeftConstant(selection.KeyId);
                        isRightFree = isRightFree && memberCurve.AnimationCurves[selection.CurveId]
                            .IsRightFree(selection.KeyId);
                        isRightLinear = isRightLinear && memberCurve.AnimationCurves[selection.CurveId]
                            .IsRightLinear(selection.KeyId);
                        isRightConstant = isRightConstant && memberCurve.AnimationCurves[selection.CurveId]
                            .IsRightConstant(selection.KeyId);
                        bothTangentsFree = bothTangentsFree &&
                                           memberCurve.AnimationCurves[selection.CurveId]
                                               .IsLeftFree(selection.KeyId) && memberCurve
                                               .AnimationCurves[selection.CurveId]
                                               .IsRightFree(selection.KeyId);
                        bothTangentsLinear = bothTangentsLinear &&
                                             memberCurve.AnimationCurves[selection.CurveId]
                                                 .IsLeftLinear(selection.KeyId) &&
                                             memberCurve.AnimationCurves[selection.CurveId]
                                                 .IsRightLinear(selection.KeyId);
                        bothTangentsConstant = bothTangentsConstant &&
                                               memberCurve.AnimationCurves[selection.CurveId]
                                                   .IsLeftConstant(selection.KeyId) &&
                                               memberCurve.AnimationCurves[selection.CurveId]
                                                   .IsRightConstant(selection.KeyId);
                        keyframeContexts.Add(new KeyframeContext(
                            memberCurve.AnimationCurves[selection.CurveId], selection.KeyId,
                            memberCurve.AnimationCurves[selection.CurveId]
                                .GetKeyframeWrapper(selection.KeyId)));
                    }
                }
            }
            if (keyframeContexts.Count > 0)
            {
                menu.AddItem(new GUIContent(keyframeContexts.Count > 1 ? "Delete Keys" : "Delete Key"), false, DeleteKeysBySelected, keyframeContexts);
                if (timelineControl.ShowCurves)
                {
                    menu.AddItem(new GUIContent("Clamped Auto"), isClampedAuto, SetKeyClampedAuto, keyframeContexts);
                    menu.AddItem(new GUIContent("Auto"), isAuto, SetKeyAuto, keyframeContexts);
                    menu.AddItem(new GUIContent("Broken"), isBroken, SetKeyBroken, keyframeContexts);
                    menu.AddItem(new GUIContent("Left Tangent/Free"), isLeftFree, SetKeyLeftFree, keyframeContexts);
                    menu.AddItem(new GUIContent("Left Tangent/Linear"), isLeftLinear, SetKeyLeftLinear, keyframeContexts);
                    menu.AddItem(new GUIContent("Left Tangent/Constant"), isLeftConstant, SetKeyLeftConstant, keyframeContexts);
                    menu.AddItem(new GUIContent("Right Tangent/Free"), isRightFree, SetKeyRightFree, keyframeContexts);
                    menu.AddItem(new GUIContent("Right Tangent/Linear"), isRightLinear, SetKeyRightLinear, keyframeContexts);
                    menu.AddItem(new GUIContent("Right Tangent/Constant"), isRightConstant, SetKeyRightConstant, keyframeContexts);
                    menu.AddItem(new GUIContent("Both Tangents/Free"), bothTangentsFree, SetKeyBothFree, keyframeContexts);
                    menu.AddItem(new GUIContent("Both Tangents/Linear"), bothTangentsLinear, SetKeyBothLinear, keyframeContexts);
                    menu.AddItem(new GUIContent("Both Tangents/Constant"), bothTangentsConstant, SetKeyBothConstant, keyframeContexts);
                }
            }
            return menu;
        }

        private void ShowCurveCanvasContextMenu(CurvesContext context)
        {
            m_MultiMenu.AddItem(new GUIContent("Add Keyframe"), false, AddKeyframe, context);
            m_MultiMenu.AddItem(new GUIContent("Paste Key"), false, PasteKeyFrame, context);
            m_MultiMenu.AddSeparator(string.Empty);
        }

        private void ShowKeyframeContextMenu(TimelineAnimationCurveWrapper animationCurve, int i, bool isBookEnd)
        {
            var menu = new GenericMenu();
            var keyframeWrapper = animationCurve.GetKeyframeWrapper(i);
            var userData = new KeyframeContext(animationCurve, i, keyframeWrapper);
            if (!isBookEnd)
            {
                menu.AddItem(new GUIContent("Delete Key"), false, DeleteKey, userData);
            }
            menu.AddItem(new GUIContent("Copy Key"), false, CopyKey, userData);
            if (timelineControl.ShowCurves)
            {
                menu.AddItem(new GUIContent("Auto"), animationCurve.IsAuto(i), SetKeyAuto, userData);
                menu.AddItem(new GUIContent("Broken"), animationCurve.IsBroken(i), SetKeyBroken, userData);
                menu.AddItem(new GUIContent("Left Tangent/Free"), animationCurve.IsLeftFree(i), SetKeyLeftFree, userData);
                menu.AddItem(new GUIContent("Left Tangent/Linear"), animationCurve.IsLeftLinear(i), SetKeyLeftLinear, userData);
                menu.AddItem(new GUIContent("Left Tangent/Constant"), animationCurve.IsLeftConstant(i), SetKeyLeftConstant, userData);
                menu.AddItem(new GUIContent("Right Tangent/Free"), animationCurve.IsRightFree(i), SetKeyRightFree, userData);
                menu.AddItem(new GUIContent("Right Tangent/Linear"), animationCurve.IsRightLinear(i), SetKeyRightLinear, userData);
                menu.AddItem(new GUIContent("Right Tangent/Constant"), animationCurve.IsRightConstant(i), SetKeyRightConstant, userData);
                menu.AddItem(new GUIContent("Both Tangents/Free"), animationCurve.IsLeftFree(i) && animationCurve.IsRightFree(i), SetKeyBothFree, userData);
                menu.AddItem(new GUIContent("Both Tangents/Linear"), animationCurve.IsLeftLinear(i) && animationCurve.IsRightLinear(i), SetKeyBothLinear, userData);
                menu.AddItem(new GUIContent("Both Tangents/Constant"), animationCurve.IsLeftConstant(i) && animationCurve.IsRightConstant(i), SetKeyBothConstant, userData);
            }
            menu.ShowAsContext();
        }

        private void ShowMasterKeyContextMenu(CurvesContext context, bool isDeletable)
        {
            var menu = new GenericMenu();
            if (isDeletable)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Delete KeyFrames"), false, DeleteKeyframes, context);
            }
            menu.AddItem(new GUIContent("Copy KeyFrames"), false, CopyKeyframes, context);
            menu.AddItem(new GUIContent("Paste KeyFrames"), false, PasteKeyframes, context);
            menu.ShowAsContext();
        }

        private void DeleteCurve(object userData)
        {
            var wrapper = userData as TimelineMemberCurveWrapper;
            var timelineActorCurveClip = Wrapper.timelineItem as TimelineActorCurveClip;
            timelineActorCurveClip.DeleteClipCurveData(wrapper.Type, wrapper.PropertyName);
        }

        internal override void Translate(float amount)
        {
            var wrapper = Wrapper as TimelineClipCurveWrapper;
            if (wrapper != null)
            {
                wrapper.TranslateCurves(amount);
                curvesChanged = true;
            }
        }

        private static void UpdateCurveWrappers(TimelineClipCurveWrapper clipWrapper)
        {
            var curveClip = clipWrapper.timelineItem as TimelineCurveClip;
            for (int i = 0; i < curveClip.CurveData.Count; i++)
            {
                MemberCurveClipData memberCurve = curveClip.CurveData[i];

                TimelineMemberCurveWrapper memberWrapper;
                if (!clipWrapper.TryGetValue(memberCurve.Type, memberCurve.PropertyName, out memberWrapper))
                {
                    memberWrapper = new TimelineMemberCurveWrapper
                    {
                        Type = memberCurve.Type,
                        PropertyName = memberCurve.PropertyName,
                        Texture = EditorGUIUtility.ObjectContent(null, UnityPropertyTypeInfo.GetUnityType(memberCurve.Type)).image
                    };
                    clipWrapper.MemberCurves.Add(memberWrapper);
                    int showingCurves = UnityPropertyTypeInfo.GetCurveCount(memberCurve.PropertyType);

                    for (int j = 0; j < showingCurves; j++)
                    {
                        memberWrapper.AnimationCurves.Add(new TimelineAnimationCurveWrapper
                        {
                            Id = j,
                            Curve = new AnimationCurve(memberCurve.GetCurve(j).keys),
                            Label = UnityPropertyTypeInfo.GetCurveName(memberCurve.PropertyType, j)
                        });
                        memberWrapper.AnimationCurves[j].Color = UnityPropertyTypeInfo.GetCurveColor(memberCurve.Type, memberCurve.PropertyName, memberWrapper.AnimationCurves[j].Label, j);
                    }
                }
                else
                {
                    int showingCurves = UnityPropertyTypeInfo.GetCurveCount(memberCurve.PropertyType);
                    for (int j = 0; j < showingCurves; j++)
                    {
                        memberWrapper.AnimationCurves[j].Curve = memberCurve.GetCurve(j);
                    }
                }
            }
            List<TimelineMemberCurveWrapper> itemRemovals = new List<TimelineMemberCurveWrapper>();
            for (int i = 0; i < clipWrapper.MemberCurves.Count; i++)
            {
                TimelineMemberCurveWrapper cw = clipWrapper.MemberCurves[i];
                bool found = false;
                for (int j = 0; j < curveClip.CurveData.Count; j++)
                {
                    MemberCurveClipData memberCurve = curveClip.CurveData[j];
                    if (memberCurve.Type == cw.Type && memberCurve.PropertyName == cw.PropertyName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemRemovals.Add(cw);
                }
            }
            for (int i = 0; i < itemRemovals.Count; i++)
            {
                clipWrapper.MemberCurves.Remove(itemRemovals[i]);
            }
        }

        internal void UpdateHeaderArea(TimelineControlState state, Rect controlHeaderArea)
        {
            var wrapper = Wrapper as TimelineClipCurveWrapper;
            if (wrapper != null)
            {
                GUILayout.BeginArea(controlHeaderArea);
                var rect3 = new Rect(controlHeaderArea.width - 15f, 0f, 15f, controlHeaderArea.height);
                var num = controlHeaderArea.width - 12f - rect3.width;
                var num2 = 0;
                foreach (var timelineMemberCurveWrapper in wrapper.MemberCurves)
                {
                    var rect4 = new Rect(0f, 17f * num2, num * 0.66f, 17f);
                    var rect5 = new Rect(num * 0.8f + 4f, 17f * num2, 32f, 16f);
                    var userFriendlyName = TimelineControlHelper.GetUserFriendlyName(timelineMemberCurveWrapper.Type, timelineMemberCurveWrapper.PropertyName);
                    var text = userFriendlyName == string.Empty ? timelineMemberCurveWrapper.Type : $"{timelineMemberCurveWrapper.Type}.{userFriendlyName}";
                    timelineMemberCurveWrapper.IsFoldedOut = EditorGUI.Foldout(rect4, timelineMemberCurveWrapper.IsFoldedOut, new GUIContent(text, timelineMemberCurveWrapper.Texture));
                    GUI.Box(rect5, string.Empty, TimelineTrackControl.styles.keyframeContextStyle);
                    var controlID = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, rect5);
                    if (Event.current.GetTypeForControl(controlID) == EventType.MouseDown && rect5.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Delete"), false, DeleteCurve, timelineMemberCurveWrapper);
                        menu.DropDown(new Rect(rect5.x, rect5.y + rect5.height, 0f, 0f));
                    }
                    num2++;
                    if (timelineMemberCurveWrapper.IsFoldedOut)
                    {
                        foreach (var animationCurve in timelineMemberCurveWrapper.AnimationCurves)
                        {
                            var rect6 = new Rect(12f, 17f * num2, num * 0.5f, 17f);
                            var rect7 = new Rect(rect6.x + rect6.width, 17f * num2, num * 0.3f, 17f);
                            var rect8 = new Rect(rect7.x + rect7.width + 4f, 17f * num2, 32f, 16f);
                            var label = userFriendlyName == string.Empty ? animationCurve.Label : $"{userFriendlyName}.{animationCurve.Label}";
                            EditorGUI.LabelField(rect6, label);
                            var scrubberPositionFrame = state.scrubberPositionFrame;
                            var endFrame = wrapper.endFrame;
                            if (scrubberPositionFrame < endFrame + 1 && scrubberPositionFrame > endFrame)
                            {
                                scrubberPositionFrame = endFrame;
                            }
                            var num6 = animationCurve.Evaluate(TimelineUtility.FrameToTime(scrubberPositionFrame));
                            var toolbarTextField = EditorStyles.toolbarTextField;
                            var newValue = EditorGUI.FloatField(rect7, num6, toolbarTextField);
                            if (!Mathf.Approximately(newValue, num6) && scrubberPositionFrame >= wrapper.fireTime && scrubberPositionFrame <= endFrame)
                            {
                                UpdateOrAddKeyframe(animationCurve, scrubberPositionFrame, newValue);
                                curvesChanged = true;
                            }
                            var color = GUI.color;
                            GUI.color = animationCurve.Color;
                            GUI.Box(rect8, string.Empty, TimelineTrackControl.styles.keyframeContextStyle);
                            var num8 = GUIUtility.GetControlID(Wrapper.timelineItem.GetInstanceID(), FocusType.Passive, rect8);
                            if (Event.current.GetTypeForControl(num8) == EventType.MouseDown && rect8.Contains(Event.current.mousePosition) && Event.current.button == 0)
                            {
                                var menu = new GenericMenu();
                                if (scrubberPositionFrame >= wrapper.fireTime && scrubberPositionFrame <= endFrame)
                                {
                                    menu.AddSeparator(string.Empty);
                                    var userData = new CurveContext(animationCurve, scrubberPositionFrame);
                                    menu.AddItem(new GUIContent("Add Key"), false, AddKeyToCurve, userData);
                                }

                                menu.DropDown(new Rect(rect8.x, rect8.y + rect8.height, 0f, 0f));
                            }
                            GUI.color = color;
                            num2++;
                        }
                    }
                }
                GUILayout.EndArea();
            }
        }

        private float GetTangentOutValue(Vector2 screenPoint, Vector2 mousePoint, TimelineAnimationCurveWrapper animationCurve, int index)
        {
            var current = animationCurve.GetKeyframe(index);
            var next = animationCurve.GetKeyframe(index + 1);
            var offset = GetOffset(current, next);
            var originX = current.time + offset;
            var finalX = originX * ControlState.Scale.x + ControlState.Translation.x;
            var slope = (mousePoint.y - screenPoint.y) / (mousePoint.x - screenPoint.x);
            var finalY = (finalX - mousePoint.x) * slope + mousePoint.y;
            var num2 = curveTrackSafeArea.y + curveTrackSafeArea.height;
            var num = m_ViewingSpace.height - m_ViewingSpace.y;
            var originY = (num2 - finalY)*num/curveTrackSafeArea.height+m_ViewingSpace.y;
            return (originY - current.value) / offset;
        }

        private float GetTangentInValue(Vector2 screenPoint, Vector2 mousePoint, TimelineAnimationCurveWrapper animationCurve, int index)
        {
            var current = animationCurve.GetKeyframe(index);
            var next = animationCurve.GetKeyframe(index - 1);
            var offset = GetOffset(current, next);
            var originX = current.time - offset;
            var finalX = originX * ControlState.Scale.x + ControlState.Translation.x;
            var slope = (screenPoint.y - mousePoint.y) / (screenPoint.x - mousePoint.x);
            var finalY = (finalX - mousePoint.y) * slope + mousePoint.y;
            var num2 = curveTrackSafeArea.y + curveTrackSafeArea.height;
            var num = m_ViewingSpace.height - m_ViewingSpace.y;
            var originY = (num2 - finalY) * num / curveTrackSafeArea.height + m_ViewingSpace.y;
            return -(current.value - originY) / offset;
        }

        private float GetOffset(Keyframe first, Keyframe second)
        {
            return Mathf.Abs(second.time - first.time) * 0.3333333f;
        }

        private void UpdateKeyframes(TimelineClipCurveWrapper clipWrapper, TimelineControlState state)
        {
            var num = m_ViewingSpace.height - m_ViewingSpace.y;
            var num2 = curveTrackSafeArea.y + curveTrackSafeArea.height;
            var index = 2;
            for (var i = 0; i < clipWrapper.MemberCurves.Count; i++)
            {
                var wrapper = clipWrapper.MemberCurves[i];
                index++;
                for (var j = 0; j < wrapper.AnimationCurves.Count; j++)
                {
                    var animationCurve = wrapper.AnimationCurves[j];
                    var y = m_TimelineItemPosition.y + m_TimelineItemPosition.height * index++ + 8;
                    for (var k = 0; k < animationCurve.KeyframeCount; k++)
                    {
                        var current = animationCurve.GetKeyframe(k);
                        var keyframeWrapper = animationCurve.GetKeyframeWrapper(k);
                        keyframeWrapper.ScreenPosition = timelineControl.ShowCurves ? new Vector2(state.TimeToPosition(current.time), num2 - (current.value - m_ViewingSpace.y) / num * curveTrackSafeArea.height) : new Vector2(state.TimeToPosition(current.time), y);
                        if (k < animationCurve.KeyframeCount - 1)
                        {
                            var offset = GetOffset(animationCurve.GetKeyframe(k + 1), current);
                            var outTangent = current.outTangent;
                            if (float.IsPositiveInfinity(current.outTangent))
                                outTangent = 0f;
                            var origin = new Vector2(current.time + offset, current.value + offset * outTangent);
                            var final = new Vector2(origin.x * state.Scale.x + state.Translation.x, num2 - (origin.y - m_ViewingSpace.y) / num * curveTrackSafeArea.height);
                            keyframeWrapper.OutTangentControlPointPosition = final;
                        }

                        if (k > 0)
                        {
                            var offset = GetOffset(animationCurve.GetKeyframe(k - 1), current);
                            var inTangent = current.inTangent;
                            if (float.IsPositiveInfinity(current.inTangent))
                                inTangent = 0f;
                            var origin = new Vector2(current.time - offset, current.value - offset * inTangent);
                            var final = new Vector2(origin.x * state.Scale.x + state.Translation.x, num2 - (origin.y - m_ViewingSpace.y) / num * curveTrackSafeArea.height);
                            keyframeWrapper.InTangentControlPointPosition = final;
                        }
                    }
                }
            }
        }

        private void UpdateMasterKeys(TimelineClipCurveWrapper clipWrapper)
        {
            keyframeTimes = new SortedList<int, int>();
            foreach (var wrapper in clipWrapper.MemberCurves)
            {
                if (wrapper.IsVisible)
                {
                    foreach (var wrapper2 in wrapper.AnimationCurves)
                    {
                        if (wrapper2.IsVisible)
                        {
                            for (var j = 0; j < wrapper2.KeyframeCount; j++)
                            {
                                var keyframe = wrapper2.GetKeyframe(j);
                                if (!keyframeTimes.ContainsKey(keyframe.GetFrame()))
                                    keyframeTimes.Add(keyframe.GetFrame(), 0);
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < keyframeTimes.Count; i++)
                keyframeTimes[keyframeTimes.Keys[i]] = GUIUtility.GetControlID("MasterKeyframe".GetHashCode(), FocusType.Passive);
        }

        private void UpdateOrAddKeyframe(TimelineAnimationCurveWrapper curveWrapper, short frameNumber, float newValue)
        {
            var flag = false;
            for (var i = 0; i < curveWrapper.KeyframeCount; i++)
            {
                var keyframe = curveWrapper.GetKeyframe(i);
                if ((ushort)(keyframe.time * 30) == frameNumber)
                {
                    var kf = new Keyframe(keyframe.time, newValue, keyframe.inTangent, keyframe.outTangent);
                    curveWrapper.MoveKey(i, kf);
                    flag = true;
                }
            }

            if (!flag)
                curveWrapper.AddKey(frameNumber, newValue);
        }

        private class CurveContext
        {
            public readonly TimelineAnimationCurveWrapper curveWrapper;
            public readonly short frameNumber;

            public CurveContext(TimelineAnimationCurveWrapper curveWrapper, short frameNumber)
            {
                this.curveWrapper = curveWrapper;
                this.frameNumber = frameNumber;
            }
        }

        protected class KeyframeContext
        {
            public TimelineKeyframeWrapper ckw;
            public readonly TimelineAnimationCurveWrapper curveWrapper;
            public readonly int key;

            public KeyframeContext(TimelineAnimationCurveWrapper curveWrapper, int key, TimelineKeyframeWrapper ckw)
            {
                this.curveWrapper = curveWrapper;
                this.key = key;
                this.ckw = ckw;
            }
        }
    }
}
