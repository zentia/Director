using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemAttribute("Animation", "Stop Animation", CutsceneItemGenre.ActorItem)]
    public class StopAnimationEvent : CinemaActorEvent
    {
        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                Animation animation = actor.GetComponentInChildren<Animation>();
                if (!animation)
                {
                    return;
                }
                animation.Stop();
            }
        }
    }
}