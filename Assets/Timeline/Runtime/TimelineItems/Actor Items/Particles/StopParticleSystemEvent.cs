using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Particle System", "Stop", TimelineItemGenre.ActorItem)]
    public class StopParticleSystemEvent : TimelineActorEvent
    {
        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                ParticleSystem ps = actor.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                }
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                ParticleSystem ps = actor.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }
    }
}