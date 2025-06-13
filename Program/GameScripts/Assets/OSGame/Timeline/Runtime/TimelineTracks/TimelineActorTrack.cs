using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineTrack("Actor Track", new[] { TimelineTrackGenre.ActorTrack }, TimelineItemGenre.ActorItem)]
    public class TimelineActorTrack : TimelineTrack, IActorTrack
    {
        [SerializeField]
        private TimelineActorEvent[] m_CacheActorEvents;

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            if (this == null)
                return;
            m_CacheActorEvents = GetComponentsInChildren<TimelineActorEvent>();
            m_CacheActorActions = GetComponentsInChildren<TimelineActorAction>();
            foreach (var timelineActorAction in m_CacheActorActions)
            {
                var type = timelineActorAction.GetType();
                if (!hitActions.ContainsKey(type))
                {
                    hitActions[type] = new List<TimelineActorAction>();
                }
            }
        }
#endif

        private TimelineActorEvent[] ActorEvents
        {
            get
            {
                if (m_CacheActorEvents != null)
                {
                    return m_CacheActorEvents;
                }
                m_CacheActorEvents = GetComponentsInChildren<TimelineActorEvent>();
                return m_CacheActorEvents;
            }
        }

        private TimelineActorAction[] m_CacheActorActions;

        public TimelineActorAction[] ActorActions
        {
            get
            {
                if (m_CacheActorActions != null)
                {
                    return m_CacheActorActions;
                }

                m_CacheActorActions = GetComponentsInChildren<TimelineActorAction>();
                foreach (var timelineActorAction in m_CacheActorActions)
                {
                    var type = timelineActorAction.GetType();
                    if (!hitActions.ContainsKey(type))
                    {
                        hitActions[type] = new List<TimelineActorAction>();
                    }
                }
                return m_CacheActorActions;
            }
        }

        public readonly Dictionary<Type, List<TimelineActorAction>> hitActions = new();

        public Transform Actor
        {
            get
            {
                var atg = trackGroup as ActorTrackGroup;
                if (atg == null)
                {
                    Debug.LogError("No ActorTrackGroup found on parent.");
                    return null;
                }
                if (atg.Actors != null && atg.Actors.Count > 0)
                    return atg.Actors[0];
                return null;
            }
        }

        public List<Transform> Actors
        {
            get
            {
                var atg = trackGroup as ActorTrackGroup;
                if (atg == null)
                    return null;
                return atg.Actors;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Actor == null)
            {
                return;
            }
            for (var i = 0; i < ActorEvents.Length; i++)
                ActorEvents[i].Initialize(Actor.gameObject);
        }

        public override void SetTime(float time)
        {
            var previousTime = elapsedTime;
            base.SetTime(time);
            if (Actor == null)
            {
                return;
            }
            for (var i = 0; i < timelineItems.Count; i++)
            {
                var timelineEvent = timelineItems[i] as TimelineActorEvent;
                if (timelineEvent != null)
                {
                    if ((previousTime < timelineEvent.Firetime && time >= timelineEvent.Firetime) || (timelineEvent.Firetime == 0f && previousTime <= timelineEvent.Firetime && time > timelineEvent.Firetime))
                    {
                        foreach (var actor in Actors)
                        {
                            timelineEvent.Trigger(actor.gameObject);
                        }
                    }
                    else if (previousTime > timelineEvent.Firetime && time <= timelineEvent.Firetime)
                    {
                        timelineEvent.Reverse(Actor.gameObject);
                    }
                }
                else
                {
                    var action = timelineItems[i] as TimelineActorAction;
                    if (action != null)
                        action.SetTime(Actor.gameObject, time - action.Firetime, time - previousTime);
                }
            }
        }

        private void UpdateEvent(TimelineActorEvent timelineEvent, float previousTime, float time)
        {
            if ((previousTime < timelineEvent.Firetime && time >= timelineEvent.Firetime) || (timelineEvent.Firetime == 0f && previousTime <= timelineEvent.Firetime && time > timelineEvent.Firetime))
            {
                foreach (var actor in Actors)
                {
                    if (actor)
                    {
                        timelineEvent.Trigger(actor.gameObject);
                    }
                }
            }
            else if (previousTime >= timelineEvent.Firetime && elapsedTime <= timelineEvent.Firetime)
            {
                foreach (var actor in Actors)
                {
                    if (actor)
                    {
                        timelineEvent.Reverse(actor.gameObject);
                    }
                }
            }
        }

        private void UpdateAction(TimelineActorAction action, float previousTime, float time, float deltaTime)
        {
            var actors = Actors;
            if (actors == null)
                return;
            if ((previousTime < action.Firetime || previousTime <= 0f) && elapsedTime >= action.Firetime && elapsedTime < action.EndTime)
            {
                foreach (var actor in actors)
                {
                    if (actor)
                    {
                        action.Trigger(actor.gameObject);
                    }
                }
            }
            else if (previousTime < action.EndTime && elapsedTime >= action.EndTime)
            {
                foreach (var actor in actors)
                {
                    if (actor)
                    {
                        action.End(actor.gameObject);
                    }
                }
            }
            else if (previousTime >= action.Firetime && previousTime < action.EndTime && elapsedTime <= action.Firetime)
            {
                foreach (var actor in actors)
                {
                    if (actor)
                    {
                        action.ReverseTrigger(actor.gameObject);
                    }
                }
            }
            else if ((previousTime > action.EndTime || previousTime >= timeline.Duration) && elapsedTime > action.Firetime && elapsedTime <= action.EndTime)
            {
                foreach (var actor in actors)
                {
                    if (actor)
                    {
                        action.ReverseEnd(actor.gameObject);
                    }
                }
            }
            else if (elapsedTime > action.Firetime && elapsedTime <= action.EndTime)
            {
                var runningTime = time - action.Firetime;
                hitActions[action.GetType()].Add(action);
                foreach (var actor in actors)
                {
                    if (actor)
                    {
                        action.UpdateTime(actor.gameObject, runningTime, deltaTime);
                    }
                }
            }
        }

        public override void UpdateTrack(float time, float deltaTime)
        {
            var previousTime = elapsedTime;
            elapsedTime = time;
            if (hitActions == null)
            {
                return;
            }
            foreach (var hitAction in hitActions)
            {
                hitAction.Value.Clear();
            }
            foreach (var timelineActorEvent in ActorEvents)
            {
                if (timelineActorEvent != null && timelineActorEvent.gameObject.activeSelf)
                    UpdateEvent(timelineActorEvent, previousTime, time);
            }
            foreach (var timelineActorAction in ActorActions)
            {
                if (timelineActorAction != null && timelineActorAction.gameObject.activeSelf)
                    UpdateAction(timelineActorAction, previousTime, time, deltaTime);
            }
        }

        public override void Pause()
        {
            base.Pause();
            if (Actor == null)
            {
                return;
            }
            for (var i = 0; i < timelineItems.Count; i++)
            {
                var action = timelineItems[i] as TimelineActorAction;
                if (action != null)
                    if (elapsedTime > action.Firetime && elapsedTime < action.Firetime + action.Duration)
                        action.Pause(Actor.gameObject);
            }
        }

        public override void Resume()
        {
            base.Resume();
            if (Actor == null)
            {
                return;
            }
            for (var i = 0; i < timelineItems.Count; i++)
            {
                var action = timelineItems[i] as TimelineActorAction;
                if (action != null)
                    if (elapsedTime > action.Firetime && elapsedTime < action.Firetime + action.Duration)
                        action.Resume(Actor.gameObject);
            }
        }

        public override void Stop()
        {
            elapsedTime = 0f;
            var actors = Actors;
            for (var i = 0; i < timelineItems.Count; i++)
            {
                var timelineEvent = timelineItems[i] as TimelineActorEvent;
                if (timelineEvent != null)
                {
                    foreach (var actor in actors)
                    {
                        if (actor)
                        {
                            timelineEvent.Stop(actor.gameObject);
                        }
                    }
                    continue;
                }

                var action = timelineItems[i] as TimelineActorAction;
                if (action != null)
                {
                    foreach (var actor in actors)
                    {
                        if (actor)
                        {
                            action.Stop(Actor.gameObject);
                        }
                    }
                }
            }
        }
    }
}
