using System;

namespace TimelineEditor
{
    public class TimelineTrackAttribute : Attribute
    {
        private Type trackType;

        public TimelineTrackAttribute(Type type)
        {
            trackType = type;
        }

        public Type TrackType => trackType;
    }
}
