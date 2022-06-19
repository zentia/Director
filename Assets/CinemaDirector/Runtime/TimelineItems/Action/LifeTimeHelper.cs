using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace AGE
{
    [ExecuteInEditMode]
    public class LifeTimeHelper : MonoBehaviour
    {
        public float startTime = 0.0f;
        public bool destroyAtActionStop = true; //在action停止的时候直接干掉

        private bool destroyAtAsynLoad = false;  // 预加载的特效，自动停止规则改动（强制自动停止为false)
        private float asynDelayDestoryTime = 0f;    // 延迟销毁时间

        private bool lifeTimeDestroy = false;  // 通过设置生命时间管理卸载
        private float lifeTime = 0f;    // 生命周期

        private ParticleSystem[] particles = null;
        private Transform[] transforms = null;
        private Animator[] animators = null;
        private bool _checkAlive = false;

        // store flag of instant object, restore it from prefab obj when ActionManager.DestroyGameObject 
        public bool enableTag = false;
        public bool enableLayer = false;
        public bool enableScaling = false;

        private string _oldTag = "";
        private int _oldLayer = 0;
        private Vector3 _oldScale = Vector3.one;
        private bool _actionStop = false;   // 标记是否Action已经结束

        void Awake()
        {
            try
            {
                _oldTag = gameObject.tag;
            }
            catch(Exception)
            {
                _oldTag = "undefined";
                gameObject.tag = _oldTag;
            }

            _oldLayer = gameObject.layer;
            _oldScale = transform.localScale;
        }

        void OnEnable()
        {
            if(particles == null)
            {
                particles = GetComponentsInChildren<ParticleSystem>(true);
            }

            if(animators == null)
            {
                animators = GetComponentsInChildren<Animator>(true);
            }

            _checkAlive = (particles != null && particles.Length != 0) || (animators != null && animators.Length != 0);
        }

        void Update()
        {
            if (_checkAlive)
            {
                bool canDestroy = true;
                if(particles != null)
                {
                    for (int i = 0; i < particles.Length; ++i)
                    {
                        if (null == particles[i])
                        {
                            continue;
                        }

                        if (!particles[i].IsAlive(false))
                        {
                            continue;
                        }

                        if (!destroyAtActionStop && particles[i].loop)
                        {
                            particles[i].loop = false;
                        }

                        // 如果可以循环播放，切成非循环的
                        if (particles[i].loop && _actionStop && destroyAtAsynLoad)
                        {
                            particles[i].loop = false;
                        }

                        if (!particles[i].playOnAwake || !particles[i].isStopped)
                        {
                            canDestroy = false;
                            break;
                        }
                    }
                }

                if (canDestroy && animators != null)
                {
                    for (int i = 0, imax = animators.Length; i < imax; i++)
                    {
                        if (animators[i] != null)
                        {
                            AnimatorStateInfo info = animators[i].GetCurrentAnimatorStateInfo(0);
                            if ((info.loop || info.normalizedTime < 1.0f) && info.length != 0f)//length为0，动画有异常
                            {
                                canDestroy = false;
                                // 如果可以循环播放，切异步加载的，内部手动终止（外部已经通知结束）
                                if (info.loop && _actionStop && destroyAtAsynLoad)
                                {
                                    canDestroy = true;
                                }
                                break;
                            }
                        }
                    }
                }

                // 如果异步记载且已经结束，然后delaytime放完，也可以借宿
                if(destroyAtAsynLoad)
                {
                    asynDelayDestoryTime = asynDelayDestoryTime - Time.deltaTime;   // 因为aciton已经不在了，这里用time来处理，可能会延长表现时间
                    if (asynDelayDestoryTime <= 0)
                    {
                        canDestroy = true;
                    }
                }

                if (lifeTimeDestroy)
                {
                    lifeTime = lifeTime - Time.deltaTime;
                    if (lifeTime <= 0)
                    {
                        canDestroy = true;
                    }
                }

                if (canDestroy)
                {
                    ActionService.GetInstance().DestroyGameObject(gameObject);
                    particles = null;
                }
            }
        }

        public void ResetParam()
        {
            enabled = true;
            startTime = 0.0f;
            asynDelayDestoryTime = 0f;
            lifeTimeDestroy = false;
            lifeTime = 0f;
            destroyAtActionStop = true;
            destroyAtAsynLoad = false;
            _actionStop = false;

            enableLayer = false;
            enableTag = false;
            enableScaling = false;

        }

        public void SetLayer(int layer)
        {
            if(transforms == null)
            {
                transforms = GetComponentsInChildren<Transform>(true);
            }

            if(gameObject != null)
            {
                gameObject.SetLayerNoRecursively(layer);
            }

            if (transforms != null && transforms.Length != 0)
            {
                for (int i = 0; i < transforms.Length; ++i)
                {
                    if(transforms[i] != null && transforms[i].gameObject != null)
                    {
                        transforms[i].gameObject.SetLayerNoRecursively(layer);
                    }
                }
            }
        }

        public void SetTag(string tag)
        {
            if (transforms == null)
            {
                transforms = GetComponentsInChildren<Transform>(true);
            }

            if(gameObject != null)
            {
                gameObject.tag = tag;
            }

            if (transforms != null && transforms.Length != 0)
            {
                for (int i = 0; i < transforms.Length; ++i)
                {
                    if(transforms[i] != null && transforms[i].gameObject != null)
                    {
                        transforms[i].gameObject.tag = tag;
                    }
                }
            }
        }

        public void SetScale(Vector3 scale)
        {
            Vector3 oldScale = _oldScale;
            transform.localScale = scale;
            _oldScale = scale;

			if (particles == null)
			{
				particles = GetComponentsInChildren<ParticleSystem>(true);
			}

            if (particles != null)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].startSize *= scale.x / oldScale.x;
                    particles[i].startSpeed *= scale.x / oldScale.x;
                    particles[i].gravityModifier *= scale.x / oldScale.x;
                }
            }
        }

        private void ResetComponents()
        {
            if(gameObject.layer != _oldLayer)
            {
                SetLayer(_oldLayer);
            }

            if(gameObject.CompareTag(_oldTag))
            {
                SetTag(_oldTag);
            }

            if(_oldScale != Vector3.one)
            {
                SetScale(Vector3.one);
            }

            if (particles != null)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if(particles[i] != null)
                    {
                        particles[i].playbackSpeed = 1;
                        particles[i].Clear();
                        particles[i].Stop();
                    }
                }
            }
        }

        // 行动结束，如果异步加载的，还不会停止，切换为自动结束
        public void ActionStop(float cuttime)
        {
            _actionStop = true;
            if (destroyAtAsynLoad == true)
            {
                destroyAtActionStop = false;
            }
            if (cuttime>0)
            {
                asynDelayDestoryTime = asynDelayDestoryTime - cuttime;
            }
        }

        // 设置action结束时间，防止提前终止
        public void SetDestroyAtAsyn(float curtime)
        {
            destroyAtAsynLoad = true;    // 是异步加载的，再结束释放的时候留一手
            asynDelayDestoryTime = curtime; // 延迟销毁时间
        }

        //通过设置生命时间来管理卸载
        public void SetDestroyLifeTime(float time)
        {
            lifeTimeDestroy = true;
            lifeTime = time;
        }

        public static LifeTimeHelper CreateTimeHelper(GameObject obj)
        {
            LifeTimeHelper comp = obj.GetComponent<LifeTimeHelper>();
            if (comp == null)
            {
                comp = obj.AddComponent<LifeTimeHelper>();
            }
            else
            {
                comp.ResetComponents();
            }

            comp.ResetParam();

            return comp;
        }
    }
}
