using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace TimelineEditor
{
    internal interface CurveRenderer
    {
        void DrawCurve(float minTime, float maxTime, Color color, Matrix4x4 transform, Color wrapColor);
        AnimationCurve GetCurve();
        float RangeStart();
        float RangeEnd();
        void SetWrap(WrapMode wrap);
        void SetWrap(WrapMode preWrap, WrapMode postWrap);
        void SetCustomRange(float start, float end);
        float EvaluateCurveSlow(float time);
        float EvaluateCurveDeltaSlow(float time);
        Bounds GetBounds();
        Bounds GetBounds(float minTime, float maxTime);
        float ClampedValue(float value);
        void FlushCache();
    }
} // namespace
