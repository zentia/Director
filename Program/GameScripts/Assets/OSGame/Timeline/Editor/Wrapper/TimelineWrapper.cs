using System.Collections.Generic;
using TimelineRuntime;

namespace TimelineEditor
{
    public class TimelineWrapper : UnityBehaviourWrapper
    {
        private float runningTime;
        private bool isPlaying;
        public Timeline timeline;
        public TimelineWrapper(TimelineControl control)
        {
            control.Wrapper = this;
        }

        public void AddTrackGroup(TimelineTrackGroupWrapper wrapper)
        {
            TimelineTrackGroupWrappers.Add(wrapper);
        }

        public bool ContainsTrackGroup(TrackGroup behaviour, out TimelineTrackGroupWrapper trackGroupWrapper)
        {
            trackGroupWrapper = TimelineTrackGroupWrappers.Find((wrapper => wrapper.Data == behaviour));
            return trackGroupWrapper != null;
        }

        public void RemoveTrackGroup(TimelineTrackGroupWrapper behaviour)
        {
            TimelineTrackGroupWrappers.Remove(behaviour);
            timeline.OnValidate();
        }

        public float Duration
        {
            get
            {
                return timeline.Duration;
            }
            set
            {
                timeline.Duration = value;
            }
        }

        public float RunningTime
        {
            get
            {
                return runningTime;
            }
            set
            {
                runningTime = value;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            set
            {
                isPlaying = value;
            }
        }

        public readonly List<TimelineTrackGroupWrapper> TimelineTrackGroupWrappers = new();
    }
}
