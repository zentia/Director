using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Play Camera Animation", TimelineItemGenre.ActorItem)]
    public class PlayCameraAnimationAction : TimelineActorAction
    {
        public AnimationClip animationClip;
        public float beginTime;
        private Animation m_Animation;
        private AnimationState m_AnimationState;
        private bool m_CreateAnimation;
        
#if UNITY_EDITOR
        public void FixLength()
        {
            ValidateClip();
            if (animationClip != null)
            {
                duration = animationClip.length;
            }
        }
#endif

        private bool ValidateClip()
        {
            var actor = actorTrack.Actor;
            if (actor == null || !actor.gameObject.activeSelf || animationClip == null)
            {
                return false;
            }

            if (m_Animation == null)
            {
                m_Animation = actor.gameObject.GetComponent<Animation>();
                if (m_Animation != null)
                {
                    m_CreateAnimation = false;
                }
                else
                {
                    m_Animation = actor.gameObject.AddComponent<Animation>();
                    m_CreateAnimation = true;
                }
            }
            m_AnimationState = m_Animation[animationClip.name];
            if (m_AnimationState == null)
            {
                m_Animation.AddClip(animationClip, animationClip.name);
                m_AnimationState = m_Animation[animationClip.name];
            }
            m_Animation.clip = animationClip;
            m_Animation.enabled = false;
            return true;
        }

        private bool Play()
        {
            if (!ValidateClip())
                return false;

            m_Animation.Play();
            return true;
        }

        public override void Trigger(GameObject actor)
        {
            Play();
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            if (m_Animation == null || m_AnimationState == null)
            {
                if (!Play())
                    return;
            }

            if (m_Animation != null && m_AnimationState != null)
            {
                m_AnimationState.time = beginTime + runningTime;
                m_Animation.Sample();
            }
        }

        public override void SetTime(GameObject actor, float time, float deltaTime)
        {
            UpdateTime(actor, time, deltaTime);
        }

        public override void End(GameObject actor)
        {
            if (m_Animation == null || !m_CreateAnimation)
                return;
            if (Application.isPlaying)
            {
                Destroy(m_Animation);
            }
            else
            {
                DestroyImmediate(m_Animation);
            }

            m_Animation = null;
        }

        public override void Stop(GameObject actor)
        {
            End(actor);
        }
    }
}
