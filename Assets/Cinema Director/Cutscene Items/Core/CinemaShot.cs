using UnityEngine;

namespace CinemaDirector
{
    public enum ShotKind
    {
        None,
        Shake,
    }
    /// <summary>
    /// The representation of a Shot.
    /// </summary>
    [CutsceneItemAttribute("Shots", "Shot", CutsceneItemGenre.CameraShot)]
    public class CinemaShot : CinemaGlobalAction
    {
        public Camera shotCamera;
        public string cameraName;
        public string clipName;
        public string m_targetPath;
        private bool cachedState;
        private bool cachedEnable;
        private Transform m_cacheParent;
        private Animation animation;
        private AnimationClip clip;
        public static string animDir = "Animation\\Juqing\\";
        public float[] m_Args = new float[3];
        public ShotKind m_ShotKind;


        public void LoadAnim()
        {
            
        }

        public void InsureCamera()
        {
            if (shotCamera != null)
            {
                return;
            }

            
        }

        public override void Initialize()
        {
            InsureCamera();
            LoadAnim();
            if (shotCamera != null)
            {
                cachedState = shotCamera.gameObject.activeInHierarchy;
                cachedEnable = shotCamera.enabled;
            }
        }

        public override void Trigger()
        {
            if (shotCamera != null)
            {
                Cutscene.camera = shotCamera;
                shotCamera.gameObject.SetActive(true);
                shotCamera.enabled = true;
                
                if (Application.isPlaying)
                {
                    if (animation != null)
                    {
                        animation.Play(clip.name);
                    }
                }
#if UNITY_EDITOR
                else
                {
                    _time = 0.0f;
                    transform.position = shotCamera.transform.position;
                    transform.rotation = shotCamera.transform.rotation;
                    Play();
                }
#endif
            }
        }
        public Animator animatior;
        public AnimatorClipInfo[] clipInfo;
#if UNITY_EDITOR
        public bool m_HasBake;
        private float m_RecorderStopTime = 0.0f;
        private float m_RunningTime = 0f;
        private bool m_Playing = true;

        public void ReBake()
        {
            m_HasBake = false;
            Bake();
        }
        public void Bake()
        {
            if (m_HasBake)
            {
                return;
            }

            if (Application.isPlaying || animatior == null)
            {
                return;
            }

            int frameCount = Mathf.RoundToInt((clipInfo[0].clip.length* clipInfo[0].clip.frameRate));
            animatior.Rebind();
            animatior.StopPlayback();
            animatior.recorderStartTime = 0.0f;
            animatior.StartRecording(frameCount);
            for (int i = 0; i < frameCount - 1; i++)
            {
                animatior.Update(1.0f / clipInfo[0].clip.frameRate);
            }
            animatior.StopRecording();
            animatior.StartPlayback();
            m_HasBake = true;
            m_RecorderStopTime = animatior.recorderStopTime;
        }
        private void Play()
        {
            if (Application.isPlaying || animatior == null)
            {
                return;
            }
            Bake();
            m_RunningTime = 0f;
            m_Playing = true;
        }
        public override void UpdateTime(float time, float deltaTime)
        {
            if (Application.isPlaying || animatior == null)
            {
                return;
            }

            if (m_RecorderStopTime <= 0)
            {
                ReBake();
            }
            if (m_RunningTime > m_RecorderStopTime)
            {
                m_Playing = false;
                return;
            }

            animatior.playbackTime = m_RunningTime;
            animatior.Update(0);
            m_RunningTime += deltaTime;
        }
#endif
        public override void End()
        {
            if (shotCamera != null)
            {
                shotCamera.gameObject.SetActive(false);
                shotCamera.enabled = false;
            }
        }

        public override void Stop()
        {
            if (shotCamera != null)
            {
                shotCamera.gameObject.SetActive(cachedState);
                shotCamera.enabled = cachedEnable;
                if (m_cacheParent)
                {
                    shotCamera.transform.parent = m_cacheParent;
                }
            }
#if UNITY_EDITOR
            if (UnityEditor.AnimationMode.InAnimationMode())
            {
                UnityEditor.AnimationMode.StopAnimationMode();
            }
#endif
        }

        /// <summary>
        /// Accesses the time that the cut takes place
        /// </summary>
        public float CutTime
        {
            get { return Firetime; }
            set { Firetime = value; }
        }

        /// <summary>
        /// The length of this shot in seconds.
        /// </summary>
        public float ShotLength
        {
            get { return Duration; }
            set { Duration = value; }
        }
    }
}