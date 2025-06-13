using Assets.Plugins.Common;
using NOAH.VFX;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Vfx Effect", "Play Vfx Effect", TimelineItemGenre.ActorItem)]
    public class PlayVfxEffect : TimelineActorEvent
    {
        public PlayType playType = PlayType.RePlay;
        public string mountPoint;

        public enum PlayType
        {
            RePlay,
            Stop,
            Pause,
        }

        public override void Trigger(GameObject targetObject)
        {
            if (targetObject == null)
            {
                Log.LogE(LogTag.Timeline, "PlayVfxEffect No modifyTargetId GameObject {0}" + timeline.name);
                return;
            }
            targetObject.SetActive(true);
            var vfxEffectHub = targetObject.GetComponent<VFXEffectHub>();
            if (vfxEffectHub != null)
            {
                switch (playType)
                {
                    case PlayType.RePlay:
                        vfxEffectHub.Reactivate();
                        break;
                    case PlayType.Stop:
                        vfxEffectHub.Stop();
                        break;
                    case PlayType.Pause:
                        vfxEffectHub.Pause(true);
                        break;
                }
            }
            else
            {
                Log.LogE(LogTag.Timeline, "the game object have no vfxEffectHub {0}" + timeline.name);
            }
        }

        public override void Stop(GameObject targetObject)
        {
            if (targetObject == null)
            {
                Log.LogE(LogTag.Timeline, "VfxEffectController No modifyTargetId GameObject {0}" + timeline.name);
                return;
            }
            targetObject.SetActive(false);
        }
    }
}
