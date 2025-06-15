using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    public class TimelineActorAction : TimelineAction
    {
        public virtual void Trigger(GameObject Actor){}

        public virtual void UpdateTime(GameObject Actor, float time, float deltaTime) { }

        public virtual void End(GameObject actor){}

        public virtual void Stop(GameObject actor) { }

        public virtual void SetTime(GameObject actor, float time, float deltaTime) { }

        public virtual void ReverseTrigger(GameObject Actor) { }

        public virtual void ReverseEnd(GameObject Actor) { }

        public virtual void Pause(GameObject Actor) { }

        public virtual void Resume(GameObject Actor) { }

        protected Transform GetActor()
        {
            var track = (timelineTrack as TimelineActorTrack);
            if (track != null)
            {
                return track.Actor;
            }
            return null;
        }

        protected TimelineActorTrack actorTrack
        {
            get
            {
                if (timelineTrack == null)
                    timelineTrack = GetComponentInParent<TimelineTrack>();
                return timelineTrack as TimelineActorTrack;
            }
        }

        public List<Transform> GetActors()
        {
            var track = timelineTrack as IActorTrack;
            if (track != null)
            {
                return track.Actors;
            }

            return null;
        }
    }
}
