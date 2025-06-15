using System.Collections.Generic;
using TimelineRuntime;

namespace TimelineEditor
{
    public class TimelineTrackWrapper : UnityBehaviourWrapper
    {
        public TimelineTrack Track;
        public TimelineTrackGroupWrapper GroupWrapper;

        public readonly List<TimelineItemWrapper> Children = new ();

        public TimelineTrackWrapper(TimelineTrack track, TimelineTrackGroupWrapper groupWrapperWrapper)
        {
            Track = track;
            GroupWrapper = groupWrapperWrapper;
        }

        public void AddItem(TimelineItemWrapper wrapper)
        {
            Children.Add(wrapper);
        }

        public bool ContainsItem(TimelineItem behaviour, out TimelineItemWrapper itemWrapper)
        {
            itemWrapper = Children.Find((wrapper => wrapper.timelineItem == behaviour));
            return itemWrapper != null;
        }
    }
}
