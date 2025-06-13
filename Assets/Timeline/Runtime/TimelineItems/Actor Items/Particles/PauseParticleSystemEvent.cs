using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Particle System", "Pause", TimelineItemGenre.ActorItem)]
    public class PauseParticleSystemEvent : TimelineActorEvent
    {
        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                ParticleSystem ps = actor.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Pause();
                }
            }
        }
    }
}