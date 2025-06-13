using Assets.Scripts.Common;
using PooledCollections;

namespace TimelineRuntime
{
    public class TimelineWrapper : PooledClassObject
    {
        private Timeline mTimeline;

        public TimelineWrapper(Timeline timeline)
        {
            mTimeline = timeline;
        }

        public override void OnReset()
        {
            base.OnReset();
            if (mTimeline != null)
            {
                TimelineService.GetInstance().StopTimeline(mTimeline);
                mTimeline = null;
            }
        }

        public Timeline Get()
        {
            return mTimeline;
        }
    }
}