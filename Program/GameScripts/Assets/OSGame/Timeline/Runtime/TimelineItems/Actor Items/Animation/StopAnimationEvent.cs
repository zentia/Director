using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Stop Animation", TimelineItemGenre.ActorItem)]
    public class StopAnimationEvent : TimelineActorEvent
    {
        public override void Trigger(GameObject actor)
        {
            var animations = actor.GetComponentsInChildren<Animation>();
            foreach (var anim in animations)
            {
                anim.Stop();
            }
        }

        public override void Stop(GameObject actor)
        {
            var animations = actor.GetComponentsInChildren<Animation>();
            foreach (var anim in animations)
            {
                anim.Play();
            }
        }
    }
}