using TimelineRuntime;

namespace TimelineEditor
{
    public class TimelineActionWrapper : TimelineItemWrapper
    {
        private float m_Duration;

        public TimelineActionWrapper(TimelineItem timelineItem, float fireTime, float duration) : base(timelineItem, fireTime)
        {
            m_Duration = duration;
        }

        public float Duration
        {
            get
            {
                if (timelineItem as TimelineAction)
                {
                    return (timelineItem as TimelineAction).Duration;
                }
                return m_Duration;
            }
            set
            {
                if (timelineItem as TimelineAction)
                {
                    (timelineItem as TimelineAction).Duration = value;
                }
                m_Duration = value;
            }
        }

        public short endFrame => TimelineUtility.TimeToFrame(endTime);
        public float endTime => fireTime + m_Duration;
    }
}
