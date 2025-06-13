using System;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineAnimationCurveWrapper
    {
        private AnimationCurve m_Curve;
        private TimelineKeyframeWrapper[] m_KeyframeControls;
        public Color Color = Color.white;
        public int Id;
        public bool IsVisible = true;

        public string Label;

        public AnimationCurve Curve
        {
            get => m_Curve;
            set
            {
                m_Curve = value;
                InitializeKeyframeWrappers();
            }
        }

        public int KeyframeCount => m_Curve.length;

        public void AddKey(int frameNumber, float value)
        {
            var keyframe = new Keyframe();
            var keyframe2 = new Keyframe();
            var time = TimelineUtility.FrameToTime(frameNumber);
            for (var i = 0; i < m_Curve.length - 1; i++)
            {
                var keyframe4 = m_Curve[i];
                var keyframe5 = m_Curve[i + 1];
                if (keyframe4.GetFrame() == frameNumber)
                {
                    m_Curve.RemoveKey(i);
                    m_Curve.AddKey(time, value);
                    return;
                }

                if (keyframe5.GetFrame() == frameNumber)
                {
                    m_Curve.RemoveKey(i + 1);
                    m_Curve.AddKey(time, value);
                    return;
                }
                if (keyframe4.time < time && time < keyframe5.time)
                {
                    keyframe = keyframe4;
                    keyframe2 = keyframe5;
                    break;
                }
            }
            var keyframe3 = new Keyframe(time, value);

            var index = m_Curve.AddKey(keyframe3);
            if (index < 0)
                return;
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.Linear);
        }

        public void CollapseEnd(float oldEndTime, float newEndTime)
        {
            Keyframe keyframe;
            for (var i = m_Curve.length - 2; i > 0; i--)
            {
                keyframe = m_Curve[i];
                if (keyframe.time >= newEndTime && keyframe.time <= oldEndTime)
                {
                    RemoveKey(i);
                }
            }

            if (newEndTime <= GetKeyframe(0).time)
            {
                return;
            }
            keyframe = GetKeyframe(KeyframeCount - 1);
            var kf = new Keyframe(newEndTime, keyframe.value, keyframe.inTangent, keyframe.outTangent);
            MoveKey(KeyframeCount - 1, kf);
        }

        public void CollapseStart(float oldStartTime, float newStartTime)
        {
            Keyframe keyframe;
            for (var i = m_Curve.length - 2; i > 0; i--)
            {
                keyframe = m_Curve[i];
                if (keyframe.time >= oldStartTime && keyframe.time <= newStartTime)
                {
                    RemoveKey(i);
                }
            }

            if (newStartTime >= GetKeyframe(m_Curve.length - 1).time)
            {
                return;
            }
            keyframe = GetKeyframe(0);
            var kf = new Keyframe(newStartTime, keyframe.value, keyframe.inTangent, keyframe.outTangent);
            MoveKey(0, kf);
        }

        public float Evaluate(float time)
        {
            return m_Curve.Evaluate(time);
        }

        public Keyframe GetKeyframe(int index)
        {
            return m_Curve[Math.Clamp(index, 0, m_Curve.length)];
        }

        public Vector2 GetKeyframeScreenPosition(int index)
        {
            return GetKeyframeWrapper(index).ScreenPosition;
        }

        public TimelineKeyframeWrapper GetKeyframeWrapper(int i)
        {
            return m_KeyframeControls[i];
        }

        private void InitializeKeyframeWrappers()
        {
            m_KeyframeControls = new TimelineKeyframeWrapper[m_Curve.length];
            for (var i = 0; i < m_Curve.length; i++)
                m_KeyframeControls[i] = new TimelineKeyframeWrapper();
        }

        public bool IsClampedAuto(int index)
        {
            return AnimationUtility.GetKeyLeftTangentMode(m_Curve, index) == AnimationUtility.TangentMode.ClampedAuto &&
                   AnimationUtility.GetKeyRightTangentMode(m_Curve, index) == AnimationUtility.TangentMode.ClampedAuto;
        }

        public bool IsAuto(int index)
        {
            return AnimationUtility.GetKeyLeftTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Auto &&
                   AnimationUtility.GetKeyRightTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Auto;
        }

        public bool IsLeftConstant(int index)
        {
            return AnimationUtility.GetKeyLeftTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Constant;
        }

        public bool IsLeftFree(int index)
        {
            return AnimationUtility.GetKeyLeftTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Free;
        }

        public bool IsLeftLinear(int index)
        {
            return AnimationUtility.GetKeyLeftTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Linear;
        }

        public bool IsRightConstant(int index)
        {
            return AnimationUtility.GetKeyRightTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Constant;
        }

        public bool IsRightFree(int index)
        {
            return AnimationUtility.GetKeyRightTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Free;
        }

        public bool IsRightLinear(int index)
        {
            return AnimationUtility.GetKeyRightTangentMode(m_Curve, index) == AnimationUtility.TangentMode.Linear;
        }

        public bool IsBroken(int index)
        {
            return AnimationUtility.GetKeyBroken(m_Curve, index);
        }

        public int MoveKey(int index, Keyframe kf)
        {
            var num = m_Curve.MoveKey(index, kf);
            if (num < 0 || num >= m_KeyframeControls.Length)
                return num;
            (m_KeyframeControls[index], m_KeyframeControls[num]) = (m_KeyframeControls[num], m_KeyframeControls[index]);
            if (IsAuto(num))
                SmoothTangents(num, 0f);
            if (IsBroken(num))
            {
                if (IsLeftLinear(num)) SetKeyLeftLinear(num);
                if (IsRightLinear(num)) SetKeyRightLinear(num);
            }

            if (index > 0)
            {
                if (IsAuto(index - 1))
                    SmoothTangents(index - 1, 0f);
                if (IsBroken(index - 1) && IsRightLinear(index - 1))
                    SetKeyRightLinear(index - 1);
            }

            if (index < m_Curve.length - 1)
            {
                if (IsAuto(index + 1))
                    SmoothTangents(index + 1, 0f);
                if (IsBroken(index + 1) && IsLeftLinear(index + 1))
                    SetKeyLeftLinear(index + 1);
            }

            return num;
        }

        public void RemoveAtTime(int frame)
        {
            var index = -1;
            for (var i = 0; i < m_Curve.length; i++)
            {
                if (m_Curve.keys[i].GetFrame() == frame)
                    index = i;
            }
            if (index >= 0)
            {
                ArrayUtility.RemoveAt(ref m_KeyframeControls, index);
                m_Curve.RemoveKey(index);
                if (index > 0)
                {
                    if (IsAuto(index - 1)) SmoothTangents(index - 1, 0f);
                    if (IsBroken(index - 1) && IsRightLinear(index - 1)) SetKeyRightLinear(index - 1);
                }

                if (index < m_Curve.length)
                {
                    if (IsAuto(index)) SmoothTangents(index, 0f);
                    if (IsBroken(index) && IsLeftLinear(index)) SetKeyLeftLinear(index);
                }
            }
        }

        public void RemoveKey(int id)
        {
            ArrayUtility.RemoveAt(ref m_KeyframeControls, id);
            m_Curve.RemoveKey(id);
            if (id > 0)
            {
                if (IsAuto(id - 1))
                    SmoothTangents(id - 1, 0f);
                if (IsBroken(id - 1) && IsRightLinear(id - 1))
                    SetKeyRightLinear(id - 1);
            }

            if (id < m_Curve.length)
            {
                if (IsAuto(id))
                    SmoothTangents(id, 0f);
                if (IsBroken(id) && IsLeftLinear(id))
                    SetKeyLeftLinear(id);
            }
        }

        public void ScaleEnd(float oldDuration, float newDuration)
        {
            var num = newDuration / oldDuration;
            {
                var keyframe = m_Curve[0];
                var time = keyframe.time;
                for (var i = 1; i < m_Curve.length; i++)
                {
                    var keyframe2 = GetKeyframe(i);
                    var num4 = (keyframe2.time - time) * num + time;
                    var kf = new Keyframe(num4, keyframe2.value, keyframe2.inTangent, keyframe2.outTangent);
                    MoveKey(i, kf);
                }
            }
        }

        public void ScaleStart(float oldTime, float oldDuration, float newTime)
        {
            var num = (oldTime + oldDuration - newTime) / oldDuration;

            for (var i = m_Curve.length - 1; i >= 0; i--)
            {
                var keyframe = GetKeyframe(i);
                var time = (keyframe.time - oldTime) * num + newTime;
                var kf = new Keyframe(time, keyframe.value, keyframe.inTangent, keyframe.outTangent);
                MoveKey(i, kf);
            }
        }

        public void SetKeyClampedAuto(int index)
        {
            var keyframe = m_Curve[index];
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.ClampedAuto);
            m_Curve.MoveKey(index, keyframe);
        }

        public void SetKeyAuto(int index)
        {
            var keyframe = m_Curve[index];
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.Auto);
            m_Curve.MoveKey(index, keyframe);
        }

        public void SetKeyBroken(int index)
        {
            var keyframe = m_Curve[index];
            var key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent);
            m_Curve.MoveKey(index, key);
        }

        public void SetKeyLeftConstant(int index)
        {
            if (m_Curve == null)
            {
                return;
            }
            var keyframe = m_Curve[index];
            var key = new Keyframe(keyframe.time, keyframe.value, float.PositiveInfinity, keyframe.outTangent);
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.Constant);
            m_Curve.MoveKey(index, key);
        }

        public void SetKeyLeftFree(int index)
        {
            if (m_Curve == null)
            {
                return;
            }
            var keyframe = m_Curve[index];
            var key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent);
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.Free);
            m_Curve.MoveKey(index, key);
        }

        public void SetKeyLeftLinear(int index)
        {
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.Linear);
        }

        public void SetKeyRightConstant(int index)
        {
            if (m_Curve == null)
            {
                return;
            }
            var keyframe = m_Curve[index];
            var key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, float.PositiveInfinity);
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.Constant);
            m_Curve.MoveKey(index, key);
        }

        public void SetKeyRightFree(int index)
        {
            var keyframe = m_Curve[index];
            var key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent);
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.Free);
            m_Curve.MoveKey(index, key);
        }

        public void SetKeyRightLinear(int index)
        {
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.Linear);
        }

        private void SmoothTangents(int index, float weight)
        {
            m_Curve.SmoothTangents(index, weight);
        }
    }
}
