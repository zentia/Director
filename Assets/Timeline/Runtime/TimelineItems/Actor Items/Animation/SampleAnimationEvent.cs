using UnityEngine;
using System.Collections;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Sample Animation", TimelineItemGenre.ActorItem)]
    public class SampleAnimationEvent : TimelineActorEvent
    {
        public string Animation = string.Empty;
        public float Time = 0f;

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                Animation animation = actor.GetComponent<Animation>();
                if (!animation)
                {
                    return;
                }

                animation[Animation].time = Time;
                animation[Animation].enabled = true;
                animation.Sample();
                animation[Animation].enabled = false;
            }
        }

        public override void Reverse(GameObject actor)
        {
        }
    }
}