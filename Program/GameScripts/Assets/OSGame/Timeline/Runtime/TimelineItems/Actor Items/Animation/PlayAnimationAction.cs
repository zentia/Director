using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Animation", "Play Animation", TimelineItemGenre.ActorItem)]
    public class PlayAnimationAction : TimelineActorAction
    {
        [Animation]
        public string animationClipName;
        public bool stopWhenFinish;
        public float beginTime;
        private AnimationClip m_AnimationClip;
        private Animation m_Animation;
        private AnimationState m_AnimationState;
        [SerializeField]
        private bool onPlaySample = false;

#if UNITY_EDITOR
        [Button]
        private void FixLength()
        {
            ValidateClip();
            if (m_AnimationClip != null)
            {
                duration = m_AnimationClip.length;
            }
        }
#endif

        private bool ValidateClip()
        {
            var actor = actorTrack.Actor;
            if (actor == null || !actor.gameObject.activeSelf)
            {
                return false;
            }
            m_Animation = actor.GetComponent<Animation>();
            if (!m_Animation)
            {
                m_Animation = actor.GetComponentInChildren<Animation>(true);
                if (m_Animation == null)
                {
                    return false;
                }
            }

            foreach (AnimationState animationState in m_Animation)
            {
                if (animationState.name.Equals(animationClipName, StringComparison.OrdinalIgnoreCase))
                {
                    animationClipName = animationState.name;
                    m_AnimationState = animationState;
                    m_AnimationClip = m_AnimationState.clip;
                    return m_AnimationClip != null;
                }
            }

            return false;
        }

        private bool Play(PlayAnimationAction lastAction)
        {
            if (!ValidateClip())
                return false;

            if (lastAction != null)
            {
                var fadeLength = lastAction.EndTime - firetime;
                if (fadeLength < 0)
                {
                    m_Animation.Play(m_AnimationClip.name);
                }
                else
                {
                    m_Animation.CrossFade(animationClipName, fadeLength);
                }
            }
            else
            {
                if (onPlaySample && m_Animation!=null)
                {
                    var aniState = m_Animation[animationClipName];
                    if (aniState != null && aniState.clip!=null)
                    {
                        aniState.clip.SampleAnimation(m_Animation.gameObject, 0);
                    }
                }
                m_Animation.Play(m_AnimationClip.name);
            }

            m_Animation.enabled = false;
            return true;
        }

        public override void Trigger(GameObject actor)
        {
            var hitAction = actorTrack.hitActions[typeof(PlayAnimationAction)];
            if (hitAction.Count > 0)
            {
                var lastAction = hitAction[hitAction.Count - 1] as PlayAnimationAction;
                if (lastAction == this)
                {
                    return;
                }
                Play(lastAction);
            }
            else
            {
                Play(null);
            }
        }

        public void ReBindComponent(GameObject actor)
        {
            if (actor == null)
            {
                return;
            }
            if (!m_AnimationClip)
            {
                m_Animation = actor.GetComponent<Animation>();
                if (!m_Animation)
                {
                    m_Animation = actor.GetComponentInChildren<Animation>();
                }
                if (!m_Animation)
                {
                    return;
                }
            }
            if (!m_AnimationState && m_Animation)
            {
                m_AnimationState = m_Animation[animationClipName];
            }
            if (!m_AnimationClip && m_AnimationState)
            {
                m_AnimationClip = m_AnimationState.clip;
            }
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            if (m_Animation == null || m_AnimationState == null)
            {
                if (!Play(null))
                    return;
            }
            m_AnimationState.time = beginTime + runningTime;
            m_Animation.Sample();
        }

        public override void End(GameObject actor)
        {
            ReBindComponent(actor);
            if (m_Animation == null)
            {
                return;
            }
            if (!stopWhenFinish)
            {
                m_Animation.enabled = true;
                m_Animation = null;
            }
        }

        public override void Stop(GameObject actor)
        {
            End(actor);
            if (m_Animation)
                m_Animation.Stop();
            m_Animation = null;
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            name = animationClipName;
        }
#endif
    }
}
