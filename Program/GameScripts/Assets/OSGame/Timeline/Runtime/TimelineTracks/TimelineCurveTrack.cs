using System.Collections.Generic;
using Assets.Plugins.Common;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineTrack("Curve Track", TimelineTrackGenre.ActorTrack, TimelineItemGenre.CurveClipItem)]
    public class TimelineCurveTrack : TimelineTrack, IActorTrack
    {
        public Transform Actor
        {
            get
            {
                var atg = trackGroup as ActorTrackGroup;
                if (atg == null)
                {
                    Log.LogE(LogTag.Timeline, "No ActorTrackGroup found on parent.");
                    return null;
                }

                if (atg.Actors != null && atg.Actors.Count > 0)
                {
                    return atg.Actors[0];
                }

                return null;
            }
        }

        public List<Transform> Actors
        {
            get
            {
                var atg = trackGroup as ActorTrackGroup;
                if (atg == null)
                {
                    Log.LogE(LogTag.Timeline, "No ActorTrackGroup found on parent.");
                    return null;
                }

                return atg.Actors;
            }
        }

        public override void UpdateTrack(float time, float deltaTime)
        {
            var previousTime = time - deltaTime;
            elapsedTime = time;
            var actors = Actors;
            if (actors == null)
                return;
            if (timelineActions == null)
                return;
            for (var i = 0; i < timelineActions.Length; i++)
            {
                var actorCurveClip = timelineActions[i];
                if (actorCurveClip == null || !actorCurveClip.gameObject.activeSelf || actorCurveClip.timeline == null)
                {
                    continue;
                }
                if ((previousTime < actorCurveClip.Firetime || previousTime <= 0f) && elapsedTime >= actorCurveClip.Firetime && elapsedTime < actorCurveClip.EndTime)
                {
                    foreach (var actor in actors)
                    {
                        if (actor != null)
                        {
                            actorCurveClip.SampleTime(actor, actorCurveClip.Firetime);
                        }
                    }
                }
                else if (previousTime < actorCurveClip.EndTime && elapsedTime >= actorCurveClip.EndTime)
                {
                    foreach (var actor in actors)
                    {
                        if (actor != null)
                        {
                            actorCurveClip.SampleTime(actor, actorCurveClip.EndTime);
                        }
                    }
                }
                else if (previousTime >= actorCurveClip.Firetime && previousTime < actorCurveClip.EndTime && elapsedTime <= actorCurveClip.Firetime)
                {
                    foreach (var actor in actors)
                    {
                        if (actor != null)
                        {
                            actorCurveClip.SampleTime(actor, actorCurveClip.Firetime);
                        }
                    }
                }
                else if ((previousTime > actorCurveClip.EndTime || previousTime >= actorCurveClip.timeline.Duration) && elapsedTime > actorCurveClip.Firetime && elapsedTime <= actorCurveClip.EndTime)
                {
                    foreach (var actor in actors)
                    {
                        if (actor != null)
                        {
                            actorCurveClip.SampleTime(actor, actorCurveClip.EndTime);
                        }
                    }
                }
                else if (elapsedTime > actorCurveClip.Firetime && elapsedTime <= actorCurveClip.EndTime)
                {
                    foreach (var actor in actors)
                    {
                        if (actor != null)
                        {
                            actorCurveClip.SampleTime(actor, time);
                        }
                    }
                }
            }
        }

        public override void SetTime(float time)
        {
            elapsedTime = time;
            for (var i = 0; i < timelineActions.Length; i++)
            {
                var actorCurveClip = timelineActions[i];
                if (actorCurveClip != null)
                {
                    var actors = Actors;
                    foreach (var actor in actors)
                    {
                        if (actor != null)
                        {
                            actorCurveClip.SampleTime(actor, time);
                        }
                    }
                }
            }
        }
    }
}
