using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Timeline", "Stop timeline", TimelineItemGenre.ActorItem)]
    public class StopTimeline : TimelineActorEvent
    {
        public override void Trigger(GameObject actor)
        {
            var t = actor.GetComponent<Timeline>();
            if (t == null)
                return;
            t.Stop();
        }
    }
}
