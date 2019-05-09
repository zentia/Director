using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItem("Animation", "Blend Animation", CutsceneItemGenre.ActorItem)]
    public class BlendAnimationEvent : CinemaActorEvent
    {
        public string Animation = string.Empty;
        public uint animId;
        private Animation m_Animation;

        public override void Trigger(GameObject actor)
        {
            
        }

        public override void Stop(GameObject actor)
        {
#if UNITY_EDITOR
            m_State = CinemaActorEventState.None;
#endif
        }
        public override void Reverse(GameObject actor)
        {
        }
#if UNITY_EDITOR

        public void AnimCallback(AnimationClip clip)
        {
            animationClip = clip;
        }
#endif
        public override void UpdateTrack(GameObject obj, float time, float deltaTime)
        {
            base.UpdateTrack(obj, time, deltaTime);
            if (!Application.isPlaying)
            {
                if (animationClip == null)
                    return;
                if (_time > animationClip.length)
                {
                    _time = 0;
                }
                return;
            }
        }
    }
}