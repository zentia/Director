using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemAttribute("特效", "Play", CutsceneItemGenre.ActorItem)]
    public class PlayParticleSystemEvent : CinemaActorEvent
    {
        public string _path;
        private Animator effectAnim;
        private AnimatorClipInfo[] clip;
        private string _clipName;
        public override void Trigger(GameObject actor)
        {
            
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                ParticleSystem ps = actor.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                }
            }
        }
        private void OnDestroy()
        {
        }
#if UNITY_EDITOR
        private ParticleSystem m_ParticleSystem;
        public void OpenParticle()
        {
            EffectSelector.Show(Callback, "Resources\\Effect");
        }

        public void Callback(string path)
        {
            _path = path;
        }

        private bool m_HasBake;
        private float m_RecorderStopTime = 0.0f;
        private float m_RunningTime = 0f;
        private bool m_Playing = true;
        private void Bake()
        {
            if (m_HasBake)
            {
                return;
            }

            if (Application.isPlaying || effectAnim == null)
            {
                return;
            }

            const float frameRate = 30.0f;
            int frameCount = ((clip.Length * (int)frameRate) + 2);
            effectAnim.Rebind();
            effectAnim.StopPlayback();
            effectAnim.recorderStartTime = 0.0f;
            effectAnim.StartRecording(frameCount);
            for (int i = 0; i < frameCount - 1; i++)
            {
                effectAnim.Update(1.0f/frameRate);
            }
            effectAnim.StopRecording();
            effectAnim.StartPlayback();
            m_HasBake = true;
            m_RecorderStopTime = effectAnim.recorderStopTime;
        }
        private void Play()
        {
            if (Application.isPlaying || effectAnim == null)
            {
                return;
            }
            Bake();
            m_RunningTime = 0f;
            m_Playing = true;
        }
#endif
        public override void UpdateTrack(GameObject obj, float time, float deltaTime)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && effectAnim != null)
            {
                if (m_RunningTime > m_RecorderStopTime)
                {
                    m_Playing = false;
                    return;
                }
                effectAnim.playbackTime = m_RunningTime;
                effectAnim.Update(0);
                if (m_ParticleSystem)
                {
                    m_ParticleSystem.Simulate(m_RunningTime, true);
                }
                m_RunningTime += deltaTime;
            }
#endif
            if (go != null)
            {
                go.transform.position = obj.transform.position;
            }
        }
    }
}
