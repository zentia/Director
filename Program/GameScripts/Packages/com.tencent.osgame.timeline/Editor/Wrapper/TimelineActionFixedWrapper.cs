using TimelineRuntime;

namespace TimelineEditor
{
    public class TimelineActionFixedWrapper : TimelineActionWrapper
    {
        private float inTime;
        private float outTime;
        private float itemLength;

        public TimelineActionFixedWrapper(TimelineItem timelineItem, float fireTime, float duration, float inTime, float outTime, float itemLength) : base(timelineItem, fireTime, duration)
        {
            this.inTime = inTime;
            this.outTime = outTime;
            this.itemLength = itemLength;
        }

        public float InTime
        {
            get
            {
                return inTime;
            }
            set
            {
                inTime = value;
                Duration = outTime - inTime;
            }
        }

        public float OutTime
        {
            get
            {
                return outTime;
            }
            set
            {
                outTime = value;
                Duration = outTime - inTime;
            }
        }

        public float ItemLength
        {
            get
            {
                return itemLength;
            }
            set
            {
                itemLength = value;
            }

        }
    }
}
