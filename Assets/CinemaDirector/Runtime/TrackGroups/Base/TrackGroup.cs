using System;
using System.Collections.Generic;
using System.Xml;

namespace CinemaDirector
{
    [TrackGroup("Track Group", TimelineTrackGenre.GenericTrack, TimelineTrackGenre.CurveTrack)]
    public class TrackGroup : DirectorObject
    {
        protected int id = -1;

        protected List<Type> allowedTrackTypes;

        public override int CompareTo(DirectorObject directorObject)
        {
            var trackGroup = directorObject as TrackGroup;
            return id - trackGroup.id;
        }

        /// <summary>
        /// Initialize all Tracks before beginning a fresh playback.
        /// </summary>
        public virtual void Initialize()
        {
            foreach (TimelineTrack track in GetTracks())
            {
                track.Initialize();
            }
        }

        /// <summary>
        /// Update the track group to the current running time of the cutscene.
        /// </summary>
        /// <param name="time">The current running time</param>
        /// <param name="deltaTime">The deltaTime since the last update call</param>
        public virtual void UpdateTrackGroup(float time, float deltaTime)
        {
            foreach (TimelineTrack track in GetTracks())
            {
                track.UpdateTrack(time, deltaTime);
            }
        }

        /// <summary>
        /// Pause all Track Items that this TrackGroup contains.
        /// </summary>
        public virtual void Pause()
        {
            foreach (TimelineTrack track in GetTracks())
            {
                track.Pause();
            }
        }

        /// <summary>
        /// Stop all Track Items that this TrackGroup contains.
        /// </summary>
        public virtual void Stop()
        {
            foreach (TimelineTrack track in GetTracks())
            {
                track.Stop();
            }
        }

        /// <summary>
        /// Resume all Track Items that this TrackGroup contains.
        /// </summary>
        public virtual void Resume()
        {
            foreach (TimelineTrack track in GetTracks())
            {
                track.Resume();
            }
        }

        /// <summary>
        /// Set this TrackGroup to the state of a given new running time.
        /// </summary>
        /// <param name="time">The new running time</param>
        public virtual void SetRunningTime(float time)
        {
            foreach (TimelineTrack track in GetTracks())
            {
                track.SetTime(time);
            }
        }

        /// <summary>
        /// Retrieve a list of important times for this track group within the given range.
        /// </summary>
        /// <param name="from">the starting time</param>
        /// <param name="to">the ending time</param>
        /// <returns>A list of ordered milestone times within the given range.</returns>
        public virtual List<float> GetMilestones(float from, float to)
        {
            List<float> times = new List<float>();
            foreach (TimelineTrack track in GetTracks())
            {
                List<float> trackTimes = track.GetMilestones(from, to);
                foreach(float f in trackTimes)
                {
                    if(!times.Contains(f))
                    {
                        times.Add(f);
                    }
                }
            }
            times.Sort();
            return times;
        }

        public Cutscene Cutscene
        {
            get
            {
                return Parent as Cutscene;
            }
        }

        public virtual List<DirectorObject> GetTracks()
        {
            return Children;
        }

        public List<Type> GetAllowedTrackTypes()
        {
            if (allowedTrackTypes == null)
            {
                allowedTrackTypes = DirectorRuntimeHelper.GetAllowedTrackTypes(this);
            }

            return allowedTrackTypes;
        }

        /// <summary>
        /// Ordinal for UI ranking.
        /// </summary>
        public int Ordinal
        {
            get { return id; }
            set { id = value; }
        }

        public override void Export(XmlElement xmlElement)
        {
            base.Export(xmlElement);
            foreach (TimelineTrack item in Children)
            {
                item.Export(xmlElement);
            }
        }
    }
}