using System.Collections.Generic;
using Assets.Plugins.Common;
using Assets.Scripts.Framework.AssetService;
using CinemaDirector;
using UnityEngine;

namespace AGE
{
    public class ActionService : MonoSingleton<ActionService>
    {
        private DictionaryView<Object, BaseAsset> _objectAssets = new DictionaryView<Object, BaseAsset>(8); //预制件资源

        private DictionaryView<Action, AgeAsset> _runingAgeAssets = new DictionaryView<Action, AgeAsset>(8); //AGE资源
        private CMList<Cutscene> _runningActions = new CMList<Cutscene>(8);
        private DictionaryView<GameObject, bool> _newGoPools = new DictionaryView<GameObject, bool>();  //空对象池化

        private GameObject _goPoolRoot;

#if UNITY_EDITOR
        public List<Cutscene> HistoryRunningActions { get; } = new List<Cutscene>();
#endif
        
        public bool IsActionValid(Cutscene action)
        {
            if(action == null)
            {
                return false;
            }

            return _runningActions.Contains(action) && !action.IsFinished;
        }

        public Cutscene PlayAction(string name, GameObject[] gameobjects, int callbackID = 0, LifeType lifeType = LifeType.UIState, GameObject sceneRoot = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.LogE("AGE", "ActionService.PlayAction() - invalid action name.");
                return null;
            }

            AgeAsset asset = AssetService.GetInstance().LoadAgeAsset(name, lifeType);
            if (asset != null && asset.Act != null)
            {
                _runingAgeAssets.Add(asset.Act, asset);
#if UNITY_EDITOR
                if (!HistoryRunningActions.Contains(asset.Act))
                {
                    HistoryRunningActions.Add(asset.Act);    
                }
#endif
                Cutscene result = asset.Act;

                result.callbackID = callbackID;
                result.enabled = true;
                result.ResetGameObjects(gameobjects);
                result.refGameObjectsCount = gameobjects == null ? 0 : gameobjects.Length;
                result.sceneRoot = sceneRoot;
                result.CutsceneFinished += OnCutsceneFinished;
                result.Play();

                _runningActions.Add(result);

                return asset.Act;
            }

            AssetService.GetInstance().Unload(asset);
            return null;
        }

        private void OnCutsceneFinished(Cutscene sender, CutsceneEventArgs e)
        {
            if (!_runingAgeAssets.ContainsKey(sender))
            {
                return;
            }
            AssetService.GetInstance().Unload(_runingAgeAssets[sender]);
            _runingAgeAssets.Remove(sender);
            _runningActions.Remove(sender);
        }

		public void StopAction(Cutscene _action,bool forceStop = false)
		{
			if(IsActionValid(_action))
            {
                if (_action != null)
                {
					_action.Stop(forceStop);
				}
			}
        }

        public void DestroyGameObject(Object obj)
        {
            if(obj == null)
            {
                return;
            }

            if(_objectAssets.ContainsKey(obj))
            {
                AssetService.GetInstance().Unload(_objectAssets[obj]);
                _objectAssets.Remove(obj);
                return;
            }

            GameObject go = obj as GameObject;
            if (go != null && _newGoPools.ContainsKey(go))
            {
                _newGoPools[go] = false;
            }
        }

        public Object InstantiateObject(Object prefab, Vector3 pos, Quaternion rot)
        {
            if (_objectAssets.ContainsKey(prefab))
            {
                BaseAsset ba = AssetService.GetInstance().LoadInstantiateAsset(_objectAssets[prefab].Resource.m_relativePath, LifeType.Immediate);
                _objectAssets.Add(ba.Go, ba);
                ba.Tf.ExtSetLocalPosition(pos);
                ba.Tf.ExtSetLocalRotation(rot);
                ba.Go.ExtSetActive(true);
                return ba.Go;
            }
            return Object.Instantiate(prefab, pos, rot);
        }

        public GameObject GetNewGameObject()
        {
            GameObject result = null;
            var em = _newGoPools.GetEnumerator();
            CMList<GameObject> nullList = null;
            while(em.MoveNext())
            {
                if(em.Current.Key == null)
                {
                    if(nullList == null)
                    {
                        nullList = new CMList<GameObject>();
                    }

                    nullList.Add(em.Current.Key);
                    continue;
                }

                if (!em.Current.Value)
                {
                    result = em.Current.Key;
                    break;
                }
            }

            if(nullList != null && nullList.Count != 0)
            {
                for(int i = 0, imax = nullList.Count; i < imax; i++)
                {
                    _newGoPools.Remove(nullList[i]);
                }
            }

            if(result == null)
            {
                result = new GameObject("TempObject");
            }

            if(_newGoPools.ContainsKey(result))
            {
                _newGoPools[result] = true;
            }
            else
            {
                _newGoPools.Add(result, true);
            }

			if (_goPoolRoot != null)
			{
				result.transform.parent = _goPoolRoot.transform;
			}
            return result;
        }
        
        public GameObject InstantiateParticleSystem(string path, Action action, Vector3 pos, Quaternion? rot, Transform parent = null, bool destroyAtActionStop = true, bool applyActionSpeedToParticle = true, int layer = -1, string tag = "", Vector3? scale = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (action == null)
            {
                Log.LogE("AGE", "the action is null, effect path:{0}", path);
                return null;
            }

            BaseAsset ba = AssetService.GetInstance().LoadInstantiateAsset(path, LifeType.Immediate);
            if(ba == null || ba.Go == null)
            {
                Log.LogE("AGE", "load effect fail, path:{0} actionName:{1}", path, action.actionName);
                AssetService.GetInstance().Unload(ba);
                return null;
            }

            if(!_objectAssets.ContainsKey(ba.Go))
            {
                _objectAssets.Add(ba.Go, ba);
            }

            Quaternion rotation = Quaternion.identity;
            if (rot == null)
            {
                rotation = ba.Tf.rotation;
            }
            else
            {
                rotation = rot.Value;
            }

            ba.Tf.ExtSetPosition(pos);
            ba.Tf.ExtSetRotation(rotation);
            Transform particleTrans = ba.Go.transform;
            particleTrans.parent = parent;
            LifeTimeHelper lifeTimeHelper = LifeTimeHelper.CreateTimeHelper(ba.Go);
            lifeTimeHelper.destroyAtActionStop = destroyAtActionStop;
            lifeTimeHelper.enableLayer = layer != -1;
            lifeTimeHelper.enableTag = !string.IsNullOrEmpty(tag);
            lifeTimeHelper.enableScaling = (scale != null && scale.Value.x != 1.0f);
            lifeTimeHelper.startTime = action.CurrentTime;

            if (lifeTimeHelper.enableLayer)
            {
                lifeTimeHelper.SetLayer(layer);
            }

            if (lifeTimeHelper.enableTag)
            {
                lifeTimeHelper.SetTag(tag);
            }

            if (lifeTimeHelper.enableScaling && scale != null)
            {
                lifeTimeHelper.SetScale(scale.Value);
            }
            ba.Go.ExtSetActive(true);
			if (applyActionSpeedToParticle)
			{
				action.AddTempObject(Action.PlaySpeedAffectedType.ePSAT_Fx, ba.Go);
			}

            ParticleSystem particleSystem = ba.Go.GetComponent<ParticleSystem>();
            if (null != particleSystem)
            {
                particleSystem.Play(true);
            }
            return ba.Go;
        }


        protected override void Init()
        {
            BaseEventReflection.InitEventTypeDic();

            if (Application.isPlaying)
            {
                _goPoolRoot = new GameObject();
                _goPoolRoot.ExtDontDestroyOnLoad();
                _goPoolRoot.name = "AGETempObjectRoot";
            }
            DirectorEvent.StartCoroutine.AddListener(OnStartCoroutine);
            DirectorEvent.StopCoroutine.AddListener(OnStopCoroutine);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
            DirectorEvent.StartCoroutine.RemoveListener(OnStartCoroutine);
            DirectorEvent.StopCoroutine.RemoveListener(OnStopCoroutine);
        }
        
        private void OnStartCoroutine(CoroutineEvent coroutineEvent)
        {
            StartCoroutine(coroutineEvent.Invoke());
        }

        private void OnStopCoroutine(CoroutineEvent coroutineEvent)
        {
            StopCoroutine(coroutineEvent.Invoke());
        }

        private void Dispose()
        {
            var raaEm = _runingAgeAssets.GetEnumerator();
            while (raaEm.MoveNext())
            {
                AssetService.GetInstance().Unload(raaEm.Current.Value);
            }
            _runingAgeAssets.Clear();
            _runningActions.Clear();

            var oaEm = _objectAssets.GetEnumerator();
            while (oaEm.MoveNext())
            {
                AssetService.GetInstance().Unload(oaEm.Current.Value);
            }
            _objectAssets.Clear();

            var ngpEm = _newGoPools.GetEnumerator();
            while (ngpEm.MoveNext())
            {
                if (ngpEm.Current.Key != null)
                {
                    ngpEm.Current.Key.ExtDestroy();
                }
            }
            _newGoPools.Clear();
            _goPoolRoot.ExtDestroy();
        }
    }
}
