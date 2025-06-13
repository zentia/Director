using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Blend Animation", TimelineItemGenre.ActorItem)]
    public class BlendAnimationEvent : TimelineActorEvent
    {
        [Animation]
        public string Animation = string.Empty;
        public float FadeLength = 0.3f;

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                Animation animation = actor.GetComponentInChildren<Animation>();
                if (!animation)
                {
                    return;
                }
                animation.enabled = true;
                foreach (AnimationState state in animation)
                {
                    if (Animation.Equals(state.name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Animation = state.name;
                        break;
                    }
                }
                animation.CrossFade(Animation, FadeLength);
            }
        }
    }
}