using TimelineRuntime;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineControlState
    {
        public bool IsInPreviewMode;
        public ResizeOption ResizeOption;
        public Vector2 Scale;
        public float position { get; set; }

        public short scrubberPositionFrame => TimelineUtility.TimeToFrame(position);
        public Vector2 Translation;
        public float TimeToPosition(float time)
        {
            return time * Scale.x + Translation.x;
        }
    }
}
