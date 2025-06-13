using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Particle System", "ModifyParticleTexture", TimelineItemGenre.ActorItem)]
    public class ModifyParticleTexture : TimelineActorEvent
    {
        public Texture2D texture;
        public string path;

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                if (string.IsNullOrEmpty(path))
                {
                    var particle = actor.GetComponent<ParticleSystem>();
                    if (particle)
                    {
                        var shape = particle.shape;
                        shape.texture = texture;
                    }
                }
                else
                {
                    var o = actor.FindChildBFS(path);
                    if (o)
                    {
                        var particle = o.GetComponent<ParticleSystem>();
                        if (particle)
                        {
                            var shape = particle.shape;
                            shape.texture = texture;
                        }
                    }
                }
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                
            }
        }

    }
}
