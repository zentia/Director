using System.Collections.Generic;

namespace TimelineEditor
{
    public class TimelineMemberCurveWrapper
    {
        public string Type;
        public string PropertyName;
        public UnityEngine.Texture Texture;
        public bool IsVisible = true;
        public bool IsFoldedOut = true;
        public List<TimelineAnimationCurveWrapper> AnimationCurves = new ();
    }
}
