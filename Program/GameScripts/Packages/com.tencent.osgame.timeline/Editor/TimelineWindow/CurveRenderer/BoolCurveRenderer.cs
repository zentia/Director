using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace TimelineEditor
{
    internal class BoolCurveRenderer : NormalCurveRenderer
    {
        public BoolCurveRenderer(AnimationCurve curve)
            : base(curve)
        {
        }

        public override float ClampedValue(float value)
        {
            return value != 0.0f ? 1.0f : 0.0f;
        }

        public override float EvaluateCurveSlow(float time)
        {
            return ClampedValue(GetCurve().Evaluate(time));
        }
    }
} // namespace
