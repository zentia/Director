using System.Collections.Generic;

namespace CinemaDirector
{
    [TimelineTrack("Curve Track", TimelineTrackGenre.CurveTrack, CutsceneItemGenre.CurveClipItem)]
    public class CurveTrack : TimelineTrack
    {
        public List<int> fieldList = new List<int>();
        public override void UpdateTrack(float time, float deltaTime)
        {
            elapsedTime = time;
            
            foreach (TimelineItem item in GetTimelineItems())
            {
                CinemaActorClipCurve actorClipCurve = item as CinemaActorClipCurve;
                if (actorClipCurve != null)
                {
                    actorClipCurve.SampleTime(time);
                }
            }
        }

        public override void SetTime(float time)
        {
            elapsedTime = time;
            foreach (TimelineItem item in GetTimelineItems())
            {
                CinemaActorClipCurve actorClipCurve = item as CinemaActorClipCurve;
                if (actorClipCurve != null)
                {
                    actorClipCurve.SampleTime(time);
                }
            }
        }

        public override void Stop()
        {
            foreach (TimelineItem item in GetTimelineItems())
            {
                CinemaActorClipCurve actorClipCurve = item as CinemaActorClipCurve;
                if (actorClipCurve != null)
                {
                    actorClipCurve.Reset();
                }
            }
        }
    }
}