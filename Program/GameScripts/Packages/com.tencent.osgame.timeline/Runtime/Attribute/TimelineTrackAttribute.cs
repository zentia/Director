using System;
using System.Collections.Generic;

namespace TimelineRuntime
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TimelineTrackAttribute : Attribute
    {
        private string label;

        private List<TimelineTrackGenre> trackGenres = new();

        private List<TimelineItemGenre> itemGenres = new();

        public TimelineTrackAttribute(string label, TimelineTrackGenre[] TrackGenres, params TimelineItemGenre[] AllowedItemGenres)
        {
            this.label = label;
            this.trackGenres.AddRange(TrackGenres);
            this.itemGenres.AddRange(AllowedItemGenres);
        }

        public TimelineTrackAttribute(string label, TimelineTrackGenre TrackGenre, params TimelineItemGenre[] AllowedItemGenres)
        {
            this.label = label;
            this.trackGenres.Add(TrackGenre);
            this.itemGenres.AddRange(AllowedItemGenres);
        }

        /// <summary>
        /// The label of this track.
        /// </summary>
        public string Label
        {
            get
            {
                return label;
            }
        }

        /// <summary>
        /// The genres of this Track.
        /// </summary>
        public TimelineTrackGenre[] TrackGenres
        {
            get
            {
                return trackGenres.ToArray();
            }
        }

        /// <summary>
        /// The allowed item genres for this track.
        /// </summary>
        public TimelineItemGenre[] AllowedItemGenres
        {
            get
            {
                return itemGenres.ToArray();
            }
        }
    }
}
