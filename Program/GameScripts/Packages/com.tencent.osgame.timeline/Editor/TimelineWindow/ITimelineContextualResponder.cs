using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    // Required information for animation recording.
    internal interface ITimelineContextualResponder
    {
        bool IsAnimatable(PropertyModification[] modifications);
        bool IsEditable(Object targetObject);

        bool KeyExists(PropertyModification[] modifications);
        bool CandidateExists(PropertyModification[] modifications);

        bool CurveExists(PropertyModification[] modifications);

        bool HasAnyCandidates();
        bool HasAnyCurves();

        void AddKey(PropertyModification[] modifications);
        void RemoveKey(PropertyModification[] modifications);

        void RemoveCurve(PropertyModification[] modifications);

        void AddCandidateKeys();
        void AddAnimatedKeys();

        void GoToNextKeyframe(PropertyModification[] modifications);
        void GoToPreviousKeyframe(PropertyModification[] modifications);
    }
}
