using System.Collections.Generic;
using TimelineRuntime;

namespace TimelineEditor
{
    public class TimelineTrackGroupWrapper
    {
        public readonly TrackGroup Data;
        public TimelineTrackGroupControl Control;
        public bool HasChanged = true;
        public readonly List<TimelineTrackWrapper> Children = new ();

        public TimelineTrackGroupWrapper(TrackGroup group)
        {
            Data = group;
        }

        public void AddTrack(TimelineTrackWrapper wrapper)
        {
            Children.Add(wrapper);
        }

        public bool ContainsTrack(TimelineTrack behaviour, out TimelineTrackWrapper trackWrapper)
        {
            trackWrapper = Children.Find((wrapper => { return wrapper.Track == behaviour; }));
            return trackWrapper != null;
        }

        public void RemoveTrack(TimelineTrackControl timelineTrackWrapper)
        {
            Children.Remove(timelineTrackWrapper.Wrapper);
            Control.Children.Remove(timelineTrackWrapper);
        }
    }
}
