using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Timeline", "Play timeline", TimelineItemGenre.ActorItem)]
    public class PlayTimeline : TimelineActorEvent
    {
        public override void Trigger(GameObject actor)
        {
            var t = actor.GetComponent<Timeline>();
            if (t == null)
                return;
            t.Play();
        }
    }
}
