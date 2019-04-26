using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// A track which maintains all timeline items marked for actor tracks and multi actor tracks.
    /// </summary>
    [TimelineTrackAttribute("ActorTrack", new TimelineTrackGenre[] { TimelineTrackGenre.EntityTrack, TimelineTrackGenre.ActorTrack, TimelineTrackGenre.MultiActorTrack }, CutsceneItemGenre.ActorItem)]
    public class ActorItemTrack : TimelineTrack, IActorTrack, IMultiActorTrack
    {
        /// <summary>
        /// Initialize this Track and all the timeline items contained within.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            foreach (CinemaActorEvent cinemaEvent in this.ActorEvents)
            {
                foreach (Transform actor in Actors)
                {
                    if (actor != null)
                    {
                        cinemaEvent.Initialize(actor.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// The cutscene has been set to an arbitrary time by the user.
        /// Processing must take place to catch up to the new time.
        /// </summary>
        /// <param name="time">The new cutscene running time</param>
        public override void SetTime(float time)
        {
            float previousTime = elapsedTime;
            base.SetTime(time);

            foreach (TimelineItem item in GetTimelineItems())
            {
                // Check if it is an actor event.
                CinemaActorEvent cinemaEvent = item as CinemaActorEvent;
                if (cinemaEvent != null)
                {
                    if ((previousTime < cinemaEvent.Firetime) && (((time >= cinemaEvent.Firetime))))
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
								cinemaEvent._time = 0;
                                cinemaEvent.Trigger(actor.gameObject);
                            }
                        }
                    }
                    else if (((previousTime >= cinemaEvent.Firetime) && (time < cinemaEvent.Firetime)))
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                                cinemaEvent.Reverse(actor.gameObject);
                        }
                    }
                }

                // Check if it is an actor action.
                CinemaActorAction action = item as CinemaActorAction;
                if (action != null)
                {
                    foreach (Transform actor in Actors)
                    {
                        if (actor != null)
                            action.SetTime(actor.gameObject, (time - action.Firetime), time - previousTime);
                    }
                }
            }
        }

        public override void UpdateTrack(float time, float deltaTime)
        {
            float previousTime = elapsedTime;
            base.UpdateTrack(time, deltaTime);
            TimelineItem[] items = GetTimelineItems();
            foreach (TimelineItem item in items)
            {
                CinemaActorEvent cinemaEvent = item as CinemaActorEvent;
                if (cinemaEvent != null)
                {
                    if (previousTime <= cinemaEvent.Firetime && elapsedTime > cinemaEvent.Firetime)
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
								cinemaEvent._time = 0;
                                cinemaEvent.Trigger(actor.gameObject);
                            }
                        }
                    }
                    #if UNITY_EDITOR
                    else if(elapsedTime > cinemaEvent.Firetime)
                    {
                        if (!Application.isPlaying)
                        {
                            foreach (Transform actor in Actors)
                            {
                                if (actor != null)
                                    cinemaEvent.UpdateTrack(actor.gameObject, time, deltaTime);
                            }
                        }
                    }
                    #endif
                    if (((previousTime >= cinemaEvent.Firetime) && (elapsedTime < cinemaEvent.Firetime)))
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                                cinemaEvent.Reverse(actor.gameObject);
                        }
                    }
                }

                CinemaActorAction action = item as CinemaActorAction;
                if (action != null)
                {
                    if ((previousTime <= action.Firetime && elapsedTime > action.Firetime) && elapsedTime < action.EndTime)
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
                                action.Trigger(actor.gameObject);
                            }
                        }
                    }
                    else if (previousTime <= action.EndTime && elapsedTime > action.EndTime)
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
                                action.End(actor.gameObject);
                            }
                        }
                    }
                    else if (previousTime >= action.Firetime && previousTime < action.EndTime && elapsedTime < action.Firetime)
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
                                action.ReverseTrigger(actor.gameObject);
                            }
                        }
                    }
                    else if ((previousTime > action.EndTime && (elapsedTime > action.Firetime) && (elapsedTime <= action.EndTime)))
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
                                action.ReverseEnd(actor.gameObject);
                            }
                        }
                    }
                    else if ((elapsedTime > action.Firetime) && (elapsedTime <= action.EndTime))
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
                                float runningTime = time - action.Firetime;
                                action.UpdateTime(actor.gameObject, runningTime, deltaTime);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resume playback after being paused.
        /// </summary>
        public override void Resume()
        {
            base.Resume();
            foreach (TimelineItem item in GetTimelineItems())
            {
                CinemaActorAction action = item as CinemaActorAction;
                if (action != null)
                {
                    if (((elapsedTime > action.Firetime)) && (elapsedTime < (action.Firetime + action.Duration)))
                    {
                        foreach (Transform actor in Actors)
                        {
                            if (actor != null)
                            {
                                action.Resume(actor.gameObject);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stop the playback of this track.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            elapsedTime = 0f;
            if (Actor == null)
            {
                return;
            }
            foreach (TimelineItem item in GetTimelineItems())
            {
                CinemaActorEvent cinemaEvent = item as CinemaActorEvent;
                if (cinemaEvent != null)
                {
                    foreach (var actor in Actors)
                    {
                        if (actor != null)
                            cinemaEvent.Stop(actor.gameObject);
                    }
                }
            
                CinemaActorAction action = item as CinemaActorAction;
                if (action != null)
                {
                    foreach (Transform actor in Actors)
                    {
                        if (actor != null)
                            action.Stop(actor.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Get the Actor associated with this track. Can return null.
        /// </summary>
        public Transform Actor
        {
            get
            {
                ActorTrackGroup atg = TrackGroup as ActorTrackGroup;
                if (atg == null)
                {
                    return null;
                }
                return atg.Actor;
            }
        }

        /// <summary>
        /// Get the Actors associated with this track. Can return null.
        /// In the case of MultiActors it will return the full list.
        /// </summary>
        public List<Transform> Actors
        {
            get
            {
                var trackGroup = TrackGroup as ActorTrackGroup;
                if (trackGroup != null)
                {
                    List<Transform> actors = new List<Transform>() { };
                    actors.Add(trackGroup.Actor);
                    return actors;
                }

                MultiActorTrackGroup multiActorTrackGroup = TrackGroup as MultiActorTrackGroup;
                if (multiActorTrackGroup != null)
                {
                    return multiActorTrackGroup.Actors;
                }
                return null;
            }
        }

        public CinemaActorEvent[] ActorEvents
        {
            get
            {
                return base.GetComponentsInChildren<CinemaActorEvent>();
            }
        }

        public CinemaActorAction[] ActorActions
        {
            get
            {
                return base.GetComponentsInChildren<CinemaActorAction>();
            }
        }
    }
}