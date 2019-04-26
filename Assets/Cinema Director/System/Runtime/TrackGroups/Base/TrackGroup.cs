using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// The main organizational unit of a Cutscene, The TrackGroup contains tracks.
    /// </summary>
    [TrackGroupAttribute("Track Group", TimelineTrackGenre.GlobalTrack)]
    public abstract class TrackGroup : MonoBehaviour, IOptimizable
    {
        [SerializeField]
        private int ordinal = -1; // For ordering in UI

        [SerializeField]
        private bool canOptimize = true; // If true, this Track Group will load all tracks into cache on Optimize().
        // A cache of the tracks for optimization purposes.
        protected TimelineTrack[] trackCache;

        // A list of the types that this Track Group is allowed to contain.
        protected List<Type> allowedTrackTypes;

        private bool hasBeenOptimized;

        /// <summary>
        /// Prepares the TrackGroup by caching all TimelineTracks.
        /// </summary>
        public virtual void Optimize()
        {
            if (canOptimize)
            {
                trackCache = GetTracks();
                hasBeenOptimized = true;
            }
            foreach (TimelineTrack track in GetTracks())
            {
                track.Optimize();
            }
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
            foreach (var track in GetTracks())
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
                Cutscene cutscene = null;
                if (transform.parent != null)
                {
                    cutscene = transform.parent.GetComponentInParent<Cutscene>();
                    if (cutscene == null)
                    {
                        Debug.LogError("No Cutscene found on parent!", this);
                    }
                }
                else
                {
                    Debug.LogError("TrackGroup has no parent!", this);
                }
                return cutscene;
            }
        }

        public virtual TimelineTrack[] GetTracks()
        {
            // Return the cache if possible
            if (hasBeenOptimized)
            {
                return trackCache;
            }

            List<TimelineTrack> tracks = new List<TimelineTrack>();
            foreach (Type t in GetAllowedTrackTypes())
            {
                var components = GetComponentsInChildren(t);
                foreach(var component in components)
                {
                    tracks.Add((TimelineTrack)component);
                }
            }

            tracks.Sort(
                delegate(TimelineTrack track1, TimelineTrack track2)
                { 
                    return track1.Ordinal - track2.Ordinal;
                });
            return tracks.ToArray();
        }

        /// <summary>
        /// Provides a list of Types this Track Group is allowed to contain. Loaded by looking at Attributes.
        /// </summary>
        /// <returns>The list of track types.</returns>
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
            get { return ordinal; }
            set { ordinal = value; }
        }

        /// <summary>
        /// Enable this if the TrackGroup does not have Tracks added/removed during running.
        /// </summary>
        public bool CanOptimize
        {
            get { return canOptimize; }
            set { canOptimize = value; }
        }
    }
}