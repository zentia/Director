using TimelineRuntime;

namespace TimelineEditor
{
    public class TimelineItemWrapper
    {
        public float fireTime;
        public short FireFrameNumber => TimelineUtility.TimeToFrame(fireTime);
        public TimelineItem timelineItem;

        public TimelineItemWrapper(TimelineItem timelineItem, float fireTime)
        {
            this.timelineItem = timelineItem;
            this.fireTime = fireTime;
        }
    }
}
