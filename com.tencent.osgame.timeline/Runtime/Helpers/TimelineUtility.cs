using UnityEngine;

namespace TimelineRuntime
{
    public static class TimelineUtility 
    {
        public static short TimeToFrame(float value)
        {
            var roundedValue = Mathf.Round(value * 30);
            return (short)roundedValue;
        }

        public static float FrameToTime(int frame)
        {
            return frame / 30.0f;
        }

        public static short GetFrame(this Keyframe keyframe)
        {
            return TimeToFrame(keyframe.time);
        }
    }
}
