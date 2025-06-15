using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace TimelineEditorInternal
{
    // Required information for animation recording.
    internal interface ITimelineRecordingState
    {
        GameObject activeGameObject { get; }
        GameObject activeRootGameObject { get; }
        AnimationClip activeAnimationClip { get; }
        int currentFrame { get; }

        bool addZeroFrame { get; }

        bool DiscardModification(PropertyModification modification);
        void SaveCurve(TimelineWindowCurve curve);
        void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride);
    }
}
