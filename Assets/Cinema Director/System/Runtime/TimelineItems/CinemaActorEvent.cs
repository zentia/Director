using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CinemaDirector
{
    /// <summary>
    /// An implementation of an event that can be performed on an arbitrary actor.
    /// </summary>
    public enum CinemaActorEventState
    {
        None,
        Trigger,
        Stop,
    }
    [ExecuteInEditMode]
    public abstract class CinemaActorEvent : TimelineItem
    {
        protected GameObject go;
        protected AnimationClip animationClip;
        /// <summary>
        /// Trigger this event using the given actor.
        /// </summary>
        /// <param name="Actor">The actor to perform the event on.</param>
        public abstract void Trigger(GameObject Actor);

        /// <summary>
        /// Reverse the trigger.
        /// </summary>
        /// <param name="Actor">The actor to perform the event on.</param>
        public virtual void Reverse(GameObject Actor) { }

        public virtual void SetTimeTo(float deltaTime) { }

        public virtual void Pause() { }

        public virtual void Resume() { }

        public virtual void Initialize(GameObject Actor) { }

        public virtual void Stop(GameObject Actor) { }
#if UNITY_EDITOR
        [System.NonSerialized]
        public CinemaActorEventState m_State;
#endif
        public virtual void UpdateTrack(GameObject obj, float time, float deltaTime)
        {
            _time += deltaTime;
#if UNITY_EDITOR
            if (go == null)
                return;

            if (animationClip == null)
                return;

            // There is a bug in AnimationMode.SampleAnimationClip which crashes
            // Unity if there is no valid controller attached
            Animation animation = go.GetComponent<Animation>();
            if (animation == null)
                return;
            if (!EditorApplication.isPlaying && AnimationMode.InAnimationMode())
            {
                AnimationMode.BeginSampling();
                animationClip.SampleAnimation(go, _time);
                AnimationMode.EndSampling();

                SceneView.RepaintAll();
            }
#endif
        }

        /// <summary>
        /// Get the actors associated with this Actor Event. Can return null.
        /// </summary>
        /// <returns>A set of actors related to this actor event.</returns>
        public virtual List<Transform> GetActors()
        {
            IMultiActorTrack track = (TimelineTrack as IMultiActorTrack);
            if (track != null)
            {
                return track.Actors;
            }
            return null;
        }

        /// <summary>
        /// The Actor Track Group associated with this event.
        /// </summary>
        public ActorTrackGroup ActorTrackGroup
        {
            get
            {
                return TimelineTrack.TrackGroup as ActorTrackGroup;
            }
        }
    }
}