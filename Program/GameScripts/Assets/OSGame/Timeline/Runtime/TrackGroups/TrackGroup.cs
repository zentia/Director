using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineTrackGroup("Track Group", TimelineTrackGenre.GlobalTrack)]
    public abstract class TrackGroup : MonoBehaviour
    {
        [ReadOnly]
        public List<TimelineTrack> timelineTracks = new ();

        public virtual void Initialize()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }
            foreach (var timelineTrack in timelineTracks)
            {
                if (!timelineTrack.gameObject.activeSelf)
                {
                    continue;
                }
                timelineTrack.trackGroup = this;
                timelineTrack.timeline = timeline;
                timelineTrack.Initialize();
            }
        }

        public virtual void UpdateTrackGroup(float time, float deltaTime)
        {
            foreach (var timelineTrack in timelineTracks)
            {
                if (timelineTrack.gameObject.activeSelf && timeline != null)
                    timelineTrack.UpdateTrack(time, deltaTime);
            }
        }

        public virtual void ScrubToTime(float time)
        {
            foreach (var timelineTrack in timelineTracks)
            {
                timelineTrack.ScrubToTime(time);
            }
        }

        public virtual void Pause()
        {
            foreach (var timelineTrack in timelineTracks)
            {
                if (timelineTrack.gameObject.activeSelf)
                {
                    timelineTrack.Pause();
                }
            }
        }

        public virtual void Stop()
        {
            foreach (var timelineTrack in timelineTracks)
            {
                if (timelineTrack.gameObject.activeSelf)
                {
                    timelineTrack.Stop();
                }
            }
        }

        public virtual void Resume()
        {
            foreach (var timelineTrack in timelineTracks)
            {
                if (timelineTrack.gameObject.activeSelf)
                {
                    timelineTrack.Resume();
                }
            }
        }

        public virtual void SetRunningTime(float time)
        {
            foreach (var timelineTrack in timelineTracks)
            {
                if (timelineTrack.gameObject.activeSelf)
                {
                    timelineTrack.SetTime(time);
                }
            }
        }

        public virtual List<float> GetMilestones(float from, float to)
        {
            List<float> times = new List<float>();
            foreach (var timelineTrack in timelineTracks)
            {
                List<float> trackTimes = timelineTrack.GetMilestones(from, to);
                for (int j = 0; j < trackTimes.Count; j++)
                {
                    if(!times.Contains(trackTimes[j]))
                    {
                        times.Add(trackTimes[j]);
                    }
                }
            }
            times.Sort();
            return times;
        }

        [NonSerialized]
        public Timeline timeline;

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
            var parent = transform.parent;
            if (parent == null)
                return;
            timeline = parent.GetComponent<Timeline>();
            if (timeline == null)
            {
                timeline = GetComponentInParent<Timeline>(true);
                if (timeline != null)
                {
                    transform.SetParent(timeline.transform, true);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            timeline.OnValidate();
            GetComponentsInChildren(true, timelineTracks);
        }
#endif
    }
}
