using System;

namespace TimelineEditor
{
    public class TimelineTrackGroupControlAttribute : Attribute
    {
        private readonly Type trackGroupType;
        public TimelineTrackGroupControlAttribute(Type type) 
        {
            trackGroupType = type;
        }

        public Type TrackGroupType => trackGroupType;
    }
}
