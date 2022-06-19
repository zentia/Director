using System;
using System.Collections.Generic;

namespace CinemaDirector
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TimelineTrackAttribute : Attribute
    {
        private string label;

        private List<TimelineTrackGenre> trackGenres = new List<TimelineTrackGenre>();

        private List<CutsceneItemGenre> itemGeners = new List<CutsceneItemGenre>();

        public TimelineTrackAttribute(string label, TimelineTrackGenre[] TrackGenres,
            params CutsceneItemGenre[] AllowedItemGenres)
        {
            this.label = label;
            this.trackGenres.AddRange(TrackGenres);
            this.itemGeners.AddRange(AllowedItemGenres);
        }

        public TimelineTrackAttribute(string label, TimelineTrackGenre TrackGenre,
            params CutsceneItemGenre[] AllowedItemGenres)
        {
            this.label = label;
            this.trackGenres.Add(TrackGenre);
            this.itemGeners.AddRange(AllowedItemGenres);
        }

        public string Label => label;

        public TimelineTrackGenre[] TrackGenres=>trackGenres.ToArray();

        public CutsceneItemGenre[] AllowedItemGenres=>itemGeners.ToArray();
    }
}