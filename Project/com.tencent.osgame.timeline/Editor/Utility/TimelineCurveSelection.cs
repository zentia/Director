using UnityEngine;

namespace TimelineEditor
{
    public class TimelineCurveSelection
    {
        public readonly int CurveId;
        public int KeyId;
        public readonly TimelineMemberCurveWrapper MemberCurveWrapper;

        public TimelineCurveSelection(int curveId, int keyId, TimelineMemberCurveWrapper memberCurveWrapper)
        {
            CurveId = curveId;
            KeyId = keyId;
            MemberCurveWrapper = memberCurveWrapper;
        }

        public Keyframe Keyframe => MemberCurveWrapper.AnimationCurves[CurveId].GetKeyframe(KeyId);

        public TimelineAnimationCurveWrapper AnimationCurveWrapper => MemberCurveWrapper.AnimationCurves[CurveId];
    }
}
