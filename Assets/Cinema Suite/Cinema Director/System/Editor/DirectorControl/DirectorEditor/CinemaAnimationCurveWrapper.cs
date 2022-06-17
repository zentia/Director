using System;
using UnityEditor;
using UnityEngine;

public class CinemaAnimationCurveWrapper
{
    public int Id;
    public string Label;
    public UnityEngine.Color Color;
    public bool IsVisible = true;
    private AnimationCurve curve;
    private CinemaKeyframeWrapper[] KeyframeControls = new CinemaKeyframeWrapper[0];

    public void AddKey(float time, float value)
    {
        Keyframe key = new Keyframe();
        Keyframe keyframe2 = new Keyframe();
        for (int i = 0; i < (this.curve.length - 1); i++)
        {
            Keyframe keyframe4 = this.curve[i];
            Keyframe keyframe5 = this.curve[i + 1];
            if ((keyframe4.time < time) && (time < keyframe5.time))
            {
                key = keyframe4;
                keyframe2 = keyframe5;
            }
        }
        Keyframe keyframe3 = new Keyframe(time, value);
        int index = this.curve.AddKey(keyframe3);
        if (index > 0)
        {
            this.curve.MoveKey(index - 1, key);
            if (this.IsAuto(index - 1))
            {
                this.SmoothTangents(index - 1, 0f);
            }
            if (this.IsBroken(index - 1) && this.IsRightLinear(index - 1))
            {
                this.SetKeyRightLinear(index - 1);
            }
        }
        if (index < (this.curve.length - 1))
        {
            this.curve.MoveKey(index + 1, keyframe2);
            if (this.IsAuto(index + 1))
            {
                this.SmoothTangents(index + 1, 0f);
            }
            if (this.IsBroken(index + 1) && this.IsLeftLinear(index + 1))
            {
                this.SetKeyLeftLinear(index + 1);
            }
        }
        ArrayUtility.Insert<CinemaKeyframeWrapper>(ref this.KeyframeControls, index, new CinemaKeyframeWrapper());
    }

    internal void CollapseEnd(float oldEndTime, float newEndTime)
    {
        for (int i = this.curve.length - 2; i > 0; i--)
        {
            Keyframe keyframe = this.curve[i];
            if (keyframe.time >= newEndTime)
            {
                keyframe = this.curve[i];
                if (keyframe.time <= oldEndTime)
                {
                    this.RemoveKey(i);
                }
            }
        }
        if (newEndTime > this.GetKeyframe(0).time)
        {
            Keyframe keyframe = this.GetKeyframe(this.KeyframeCount - 1);
            Keyframe kf = new Keyframe(newEndTime, keyframe.value, keyframe.inTangent, keyframe.outTangent) {
                tangentMode = keyframe.tangentMode
            };
            this.MoveKey(this.KeyframeCount - 1, kf);
        }
    }

    internal void CollapseStart(float oldStartTime, float newStartTime)
    {
        for (int i = this.curve.length - 2; i > 0; i--)
        {
            Keyframe keyframe = this.curve[i];
            if (keyframe.time >= oldStartTime)
            {
                keyframe = this.curve[i];
                if (keyframe.time <= newStartTime)
                {
                    this.RemoveKey(i);
                }
            }
        }
        if (newStartTime < this.GetKeyframe(this.curve.length - 1).time)
        {
            Keyframe keyframe = this.GetKeyframe(0);
            Keyframe kf = new Keyframe(newStartTime, keyframe.value, keyframe.inTangent, keyframe.outTangent) {
                tangentMode = keyframe.tangentMode
            };
            this.MoveKey(0, kf);
        }
    }

    public float Evaluate(float time) => 
        this.curve.Evaluate(time);

    internal void FlattenKey(int index)
    {
        Keyframe keyframe2 = this.curve[index];
        keyframe2 = this.curve[index];
        Keyframe key = new Keyframe(keyframe2.time, keyframe2.value, 0f, 0f) {
            tangentMode = 0
        };
        this.curve.MoveKey(index, key);
    }

    internal void Flip()
    {
        if (this.curve.length >= 2)
        {
            AnimationCurve curve = new AnimationCurve();
            Keyframe keyframe = this.curve[0];
            float time = keyframe.time;
            keyframe = this.curve[this.curve.length - 1];
            float num2 = keyframe.time;
            for (int i = 0; i < this.curve.length; i++)
            {
                Keyframe keyframe2 = this.GetKeyframe(i);
                float num4 = (num2 - keyframe2.time) + time;
                Keyframe key = new Keyframe(num4, keyframe2.value, keyframe2.inTangent, keyframe2.outTangent) {
                    tangentMode = keyframe2.tangentMode
                };
                curve.AddKey(key);
            }
            this.Curve = curve;
        }
    }

    public Vector2 GetInTangentScreenPosition(int index) => 
        this.GetKeyframeWrapper(index).InTangentControlPointPosition;

    public Keyframe GetKeyframe(int index) => 
        this.curve[index];

    public Vector2 GetKeyframeScreenPosition(int index) => 
        this.GetKeyframeWrapper(index).ScreenPosition;

    public CinemaKeyframeWrapper GetKeyframeWrapper(int i) => 
        this.KeyframeControls[i];

    public Vector2 GetOutTangentScreenPosition(int index) => 
        this.GetKeyframeWrapper(index).OutTangentControlPointPosition;

    private void initializeKeyframeWrappers()
    {
        this.KeyframeControls = new CinemaKeyframeWrapper[this.curve.length];
        for (int i = 0; i < this.curve.length; i++)
        {
            this.KeyframeControls[i] = new CinemaKeyframeWrapper();
        }
    }

    internal bool IsAuto(int index)
    {
        Keyframe keyframe = this.curve[index];
        return (keyframe.tangentMode == 10);
    }

    internal bool IsBroken(int index)
    {
        Keyframe keyframe = this.curve[index];
        return ((keyframe.tangentMode % 2) == 1);
    }

    internal bool IsFreeSmooth(int index)
    {
        Keyframe keyframe = this.curve[index];
        return (keyframe.tangentMode == 0);
    }

    internal bool IsLeftConstant(int index)
    {
        if (this.IsBroken(index))
        {
            Keyframe keyframe = this.curve[index];
            return ((keyframe.tangentMode % 8) == 7);
        }
        return false;
    }

    internal bool IsLeftFree(int index)
    {
        if (this.IsBroken(index))
        {
            Keyframe keyframe = this.curve[index];
            return ((keyframe.tangentMode % 8) == 1);
        }
        return false;
    }

    internal bool IsLeftLinear(int index)
    {
        if (this.IsBroken(index))
        {
            Keyframe keyframe = this.curve[index];
            return ((keyframe.tangentMode % 8) == 5);
        }
        return false;
    }

    internal bool IsRightConstant(int index)
    {
        Keyframe keyframe = this.curve[index];
        return ((keyframe.tangentMode / 8) == 3);
    }

    internal bool IsRightFree(int index)
    {
        if (this.IsBroken(index))
        {
            Keyframe keyframe = this.curve[index];
            return ((keyframe.tangentMode / 8) == 0);
        }
        return false;
    }

    internal bool IsRightLinear(int index)
    {
        Keyframe keyframe = this.curve[index];
        return ((keyframe.tangentMode / 8) == 2);
    }

    public int MoveKey(int index, Keyframe kf)
    {
        int num = this.curve.MoveKey(index, kf);
        CinemaKeyframeWrapper wrapper = this.KeyframeControls[index];
        this.KeyframeControls[index] = this.KeyframeControls[num];
        this.KeyframeControls[num] = wrapper;
        if (this.IsAuto(num))
        {
            this.SmoothTangents(num, 0f);
        }
        if (this.IsBroken(num))
        {
            if (this.IsLeftLinear(num))
            {
                this.SetKeyLeftLinear(num);
            }
            if (this.IsRightLinear(num))
            {
                this.SetKeyRightLinear(num);
            }
        }
        if (index > 0)
        {
            if (this.IsAuto(index - 1))
            {
                this.SmoothTangents(index - 1, 0f);
            }
            if (this.IsBroken(index - 1) && this.IsRightLinear(index - 1))
            {
                this.SetKeyRightLinear(index - 1);
            }
        }
        if (index < (this.curve.length - 1))
        {
            if (this.IsAuto(index + 1))
            {
                this.SmoothTangents(index + 1, 0f);
            }
            if (this.IsBroken(index + 1) && this.IsLeftLinear(index + 1))
            {
                this.SetKeyLeftLinear(index + 1);
            }
        }
        return num;
    }

    internal void RemoveAtTime(float time)
    {
        int index = -1;
        for (int i = 0; i < this.curve.length; i++)
        {
            if (this.curve.keys[i].time == time)
            {
                index = i;
            }
        }
        if (index >= 0)
        {
            ArrayUtility.RemoveAt<CinemaKeyframeWrapper>(ref this.KeyframeControls, index);
            this.curve.RemoveKey(index);
            if (index > 0)
            {
                if (this.IsAuto(index - 1))
                {
                    this.SmoothTangents(index - 1, 0f);
                }
                if (this.IsBroken(index - 1) && this.IsRightLinear(index - 1))
                {
                    this.SetKeyRightLinear(index - 1);
                }
            }
            if (index < this.curve.length)
            {
                if (this.IsAuto(index))
                {
                    this.SmoothTangents(index, 0f);
                }
                if (this.IsBroken(index) && this.IsLeftLinear(index))
                {
                    this.SetKeyLeftLinear(index);
                }
            }
        }
    }

    public void RemoveKey(int id)
    {
        ArrayUtility.RemoveAt<CinemaKeyframeWrapper>(ref this.KeyframeControls, id);
        this.curve.RemoveKey(id);
        if (id > 0)
        {
            if (this.IsAuto(id - 1))
            {
                this.SmoothTangents(id - 1, 0f);
            }
            if (this.IsBroken(id - 1) && this.IsRightLinear(id - 1))
            {
                this.SetKeyRightLinear(id - 1);
            }
        }
        if (id < this.curve.length)
        {
            if (this.IsAuto(id))
            {
                this.SmoothTangents(id, 0f);
            }
            if (this.IsBroken(id) && this.IsLeftLinear(id))
            {
                this.SetKeyLeftLinear(id);
            }
        }
    }

    internal void ScaleEnd(float oldDuration, float newDuration)
    {
        float num = newDuration / oldDuration;
        Keyframe keyframe = this.curve[0];
        float time = keyframe.time;
        for (int i = 1; i < this.curve.length; i++)
        {
            Keyframe keyframe2 = this.GetKeyframe(i);
            float num4 = ((keyframe2.time - time) * num) + time;
            Keyframe kf = new Keyframe(num4, keyframe2.value, keyframe2.inTangent, keyframe2.outTangent) {
                tangentMode = keyframe2.tangentMode
            };
            this.MoveKey(i, kf);
        }
    }

    internal void ScaleStart(float oldFiretime, float oldDuration, float newFiretime)
    {
        float num = ((oldFiretime + oldDuration) - newFiretime) / oldDuration;
        for (int i = this.curve.length - 1; i >= 0; i--)
        {
            Keyframe keyframe = this.GetKeyframe(i);
            float time = ((keyframe.time - oldFiretime) * num) + newFiretime;
            Keyframe kf = new Keyframe(time, keyframe.value, keyframe.inTangent, keyframe.outTangent) {
                tangentMode = keyframe.tangentMode
            };
            this.MoveKey(i, kf);
        }
    }

    public void SetInTangentScreenPosition(int index, Vector2 screenPosition)
    {
        this.GetKeyframeWrapper(index).InTangentControlPointPosition = screenPosition;
    }

    internal void SetKeyAuto(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent) {
            tangentMode = 10
        };
        this.curve.MoveKey(index, key);
        this.curve.SmoothTangents(index, 0f);
    }

    internal void SetKeyBroken(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent) {
            tangentMode = 1
        };
        this.curve.MoveKey(index, key);
    }

    public void SetKeyframeScreenPosition(int index, Vector2 screenPosition)
    {
        this.GetKeyframeWrapper(index).ScreenPosition = screenPosition;
    }

    internal void SetKeyFreeSmooth(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent) {
            tangentMode = 0
        };
        this.curve.MoveKey(index, key);
    }

    internal void SetKeyLeftConstant(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, float.PositiveInfinity, keyframe.outTangent);
        int num = (keyframe.tangentMode > 0x10) ? ((keyframe.tangentMode / 8) * 8) : 0;
        key.tangentMode = (6 + num) + 1;
        this.curve.MoveKey(index, key);
    }

    internal void SetKeyLeftFree(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent);
        int num = (keyframe.tangentMode > 0x10) ? ((keyframe.tangentMode / 8) * 8) : 0;
        key.tangentMode = num + 1;
        this.curve.MoveKey(index, key);
    }

    internal void SetKeyLeftLinear(int index)
    {
        Keyframe keyframe = this.curve[index];
        float inTangent = keyframe.inTangent;
        if (index > 0)
        {
            Keyframe keyframe3 = this.curve[index - 1];
            inTangent = (keyframe.value - keyframe3.value) / (keyframe.time - keyframe3.time);
        }
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, inTangent, keyframe.outTangent);
        int num2 = (keyframe.tangentMode > 0x10) ? ((keyframe.tangentMode / 8) * 8) : 0;
        key.tangentMode = (num2 + 1) + 4;
        this.curve.MoveKey(index, key);
    }

    internal void SetKeyRightConstant(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, float.PositiveInfinity);
        int num = ((keyframe.tangentMode == 10) || (keyframe.tangentMode == 0)) ? 0 : ((keyframe.tangentMode % 8) - 1);
        key.tangentMode = (0x18 + num) + 1;
        this.curve.MoveKey(index, key);
    }

    internal void SetKeyRightFree(int index)
    {
        Keyframe keyframe = this.curve[index];
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, keyframe.outTangent);
        int num = ((keyframe.tangentMode == 10) || (keyframe.tangentMode == 0)) ? 0 : ((keyframe.tangentMode % 8) - 1);
        key.tangentMode = num + 1;
        this.curve.MoveKey(index, key);
    }

    internal void SetKeyRightLinear(int index)
    {
        Keyframe keyframe = this.curve[index];
        float outTangent = keyframe.outTangent;
        if (index < (this.curve.length - 1))
        {
            Keyframe keyframe3 = this.curve[index + 1];
            outTangent = (keyframe3.value - keyframe.value) / (keyframe3.time - keyframe.time);
        }
        Keyframe key = new Keyframe(keyframe.time, keyframe.value, keyframe.inTangent, outTangent);
        int num2 = ((keyframe.tangentMode == 10) || (keyframe.tangentMode == 0)) ? 0 : ((keyframe.tangentMode % 8) - 1);
        key.tangentMode = (num2 + 0x10) + 1;
        this.curve.MoveKey(index, key);
    }

    public void SetOutTangentScreenPosition(int index, Vector2 screenPosition)
    {
        this.GetKeyframeWrapper(index).OutTangentControlPointPosition = screenPosition;
    }

    public void SmoothTangents(int index, float weight)
    {
        this.curve.SmoothTangents(index, weight);
    }

    public AnimationCurve Curve
    {
        get => 
            this.curve;
        set
        {
            this.curve = value;
            this.initializeKeyframeWrappers();
        }
    }

    public int KeyframeCount =>
        this.curve.length;
}

