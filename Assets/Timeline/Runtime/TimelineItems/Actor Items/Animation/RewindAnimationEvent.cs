using UnityEngine;
using System.Collections;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Rewind Animation", TimelineItemGenre.ActorItem)]
    public class RewindAnimationEvent : TimelineActorEvent
    {
        public string Animation = string.Empty;

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                Animation animation = actor.GetComponent<Animation>();
                if (!animation)
                {
                    return;
                }

                animation.Rewind(Animation);
            }
        }

        public override void Reverse(GameObject actor)
        {
        }
    }
}