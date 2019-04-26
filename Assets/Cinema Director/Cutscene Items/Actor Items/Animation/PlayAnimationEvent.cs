using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemAttribute("Animation", "Play Animation", CutsceneItemGenre.ActorItem)]
    public class PlayAnimationEvent : CinemaActorAction
    {
        private AnimationClip animationClip = null;
		public AnimationClip m_AnimationClip;
        private Animation m_Animation;
        public string m_ClipName;
        public WrapMode wrapMode;

        public void Update()
        {
            if (wrapMode != WrapMode.Loop && animationClip)
                Duration = animationClip.length;
        }

        public override void Trigger(GameObject Actor)
        {
        }

        public override void UpdateTime(GameObject Actor, float runningTime, float deltaTime)
        {
            if (!m_Animation || animationClip == null)
            {
                return;
            }

            if (m_Animation[animationClip.name] == null)
            {
                m_Animation.AddClip(animationClip, animationClip.name);
            }

            AnimationState state = m_Animation[animationClip.name];

            if (!m_Animation.IsPlaying(animationClip.name))
            {
                m_Animation.wrapMode = wrapMode;
                m_Animation.Play(animationClip.name);
            }

            state.time = runningTime;
            state.enabled = true;
            m_Animation.Sample();
            state.enabled = false;
        }

        public override void End(GameObject Actor)
        {
            if (m_Animation)
                m_Animation.Stop();
        }
    }
}