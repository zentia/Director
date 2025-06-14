// Author zentia
// Desc collect all types and cache
// Url https://zentia.github.io/2022/08/22/Engine/Unity/Timeline/
using System;
using System.Collections.Generic;

namespace TimelineRuntime
{
    /// <summary>
    /// The Attribute for track groups.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TimelineTrackGroupAttribute : Attribute
    {
        // The user friendly name for this Track Group.
        private string label;

        // The list of Track Genres that this Track Group allows.
        private List<TimelineTrackGenre> trackGenres = new();

        /// <summary>
        /// Attribute for Track Groups
        /// </summary>
        /// <param name="label">The name of this track group.</param>
        /// <param name="TrackGenres">The Track Genres that this Track Group is allowed to contain.</param>
        public TimelineTrackGroupAttribute(string label, params TimelineTrackGenre[] TrackGenres)
        {
            this.label = label;
            this.trackGenres.AddRange(TrackGenres);
        }

        /// <summary>
        /// The label of this track group.
        /// </summary>
        public string Label
        {
            get
            {
                return label;
            }
        }

        /// <summary>
        /// The Track Genres that this Track Group can contain.
        /// </summary>
        public TimelineTrackGenre[] AllowedTrackGenres
        {
            get
            {
                return trackGenres.ToArray();
            }
        }
    }
}
