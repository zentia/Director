using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [Serializable]
    public class StartValue<T>
    {
        [LabelText("使用？")]
        public bool use;

        [LabelText("值")]
        [ShowIf("use")]
        public T value;
    }

    [Serializable]
    public class CurveValue<T> : StartValue<T>
    {
        [LabelText("运动曲线")]
        [ShowIf("use")]
        public AnimationCurve curve;
    }

    public static class CameraMotionHelper
    {
        public static float Calculate(this CurveValue<float> curve, float startValue, float proportion)
        {
            if (!curve.use)
                return startValue;

            return curve.use ? startValue + (curve.value - startValue) * curve.curve.Evaluate(proportion) : startValue;
        }

        public static Vector3 Calculate(this CurveValue<Vector3> curve, Vector3 startValue, float proportion)
        {
            return curve.use ? Vector3.Lerp(startValue, curve.value, curve.curve.Evaluate(proportion)) : startValue;
        }
    }
}
