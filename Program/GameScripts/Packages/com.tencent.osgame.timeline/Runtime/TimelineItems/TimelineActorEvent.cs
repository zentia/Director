using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    public class TimelineActorEvent : TimelineItem
    {
        public virtual void Trigger(GameObject actor) { }

        public virtual void Reverse(GameObject actor) { }

        public virtual void SetTimeTo(float deltaTime) { }

        public virtual void Pause() { }

        public virtual void Resume() { }

        public virtual void Initialize(GameObject actor) { }

        public virtual void Stop(GameObject actor) { }

        public virtual Transform GetActor()
        {
            var track = (timelineTrack as TimelineActorTrack);
            if (track != null)
            {
                return track.Actor;
            }
            return null;
        }

        public List<Transform> GetActors()
        {
            var track = timelineTrack as TimelineActorTrack;

            if (track != null)
            {
                return track.Actors;
            }

            return null;
        }
    }
}