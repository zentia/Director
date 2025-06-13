using Assets.Plugins.Common;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Play Animation Event", TimelineItemGenre.ActorItem)]
    internal class PlayAnimationEvent : TimelineActorEvent
    {
        [Animation]
        public string clipName;
        public override void Trigger(GameObject actor)
        {
            var anim = actor.GetComponent<Animation>();
            if (anim == null)
            {
                anim = actor.GetComponentInChildren<Animation>();
                if (anim == null)
                {
                    return;
                }
            }
            anim.enabled = true;
            foreach (AnimationState state in anim)
            {
                if (clipName.Equals(state.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    clipName = state.name;
                    break;
                }
            }
            if (anim.GetClip(clipName))
            {
                anim.Play(clipName);
            }
            else
            {
                Log.LogE(LogTag.Timeline, "The timeline={0} actor={1} clip={2} could not be played because it couldn't be found!", timeline.name, actor.name, clipName);
            }
        }
    }
}
