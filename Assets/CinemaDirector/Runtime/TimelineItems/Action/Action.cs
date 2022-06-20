using UnityEngine;
using System.Collections.Generic;
using System;
using Assets.Scripts.Framework.Lua;
using Assets.Plugins.Common;
using CinemaDirector;
using Sirenix.OdinInspector;

namespace AGE
{
    public enum CutsceneState
    {
        Inactive,
        Playing,
        PreviewPlaying,
        Scrubbing,
        Paused
    }

    [Serializable]
    public class Action:DirectorObject
    {
        public delegate void OnComplete();

        public event OnComplete OnCompleteEvent;

        public enum PlaySpeedAffectedType
        {
            ePSAT_Anim = 1,
            ePSAT_Fx = 2,
        };

        #region 全局变量
        public const float SpeedEpsilon = 0.00001f;
        public const float MAX_ACTION_SPEED = 100.0f;
        private static int _unuseInstanceID = 0;
        #endregion

        #region 序列化变量
        public string tag = "";
        public float length = 5.0f;
        public bool loop = false;
        #endregion

        #region 运行时变量
        public string actionName => name;  
        public bool enabled = true;
        [ReadOnly]
        public float deltaTime = 0.0f;  //
        public float time = 0.0f;  //当前时间
        public ListView<GameObject> gameObjects = new ListView<GameObject>();  //传入的引用对象
        public List<string> templateObjectIds = new List<string>();
        public RefParamOperator refParams = new RefParamOperator();  //引用对象  在event和外部逻辑之间数据交互
        DictionaryView<uint, ListView<GameObject>> tempObjsAffectedByPlaySpeed = new DictionaryView<uint, ListView<GameObject>>();  //区分是animation特效还是particle特效，主要用于改变速度
        [HideInInspector]
        public List<Track> tracks = new List<Track>();  //所有track  可以考虑修改为CMList
        DictionaryObjectView<Track, bool> conditions = new DictionaryObjectView<Track, bool>();  //每个track都的条件,条件为true才能执行  Q：为啥这个不在track内部处理呢
        private bool conditionChanged = true;  //优化处理，条件改变了才重新判断track的条件
        private ListView<Cutscene> _subActions = new ListView<Cutscene>();  //所有子Action

        public Dictionary<int, float> gameObjectsScale = new Dictionary<int, float>();  //对象index索引，对象的缩放

        // 针对异步加载的Object的处理
        private Dictionary<int, bool> gameObjectsAynsLoad = new Dictionary<int, bool>();  //对象index索引，对象是否是正式而临时
        
        private int _instanceID = 0;   //唯一标识，注意池化的话需要重置这个值

        public bool bWaitWhenPlayEnd = false;//停留在最后一帧等待,如果在最后一帧等待就不允许stop Q:如果一直为true则无法stop
        public bool IsWaitingForEnd  //是否已经等在最后一帧了
        {
            get;
            private set;
        }

        public bool IsFinished  //是否播放结束
        {
            get;
            private set;
        }

        private float playSpeed = 1.0f;  //当前速度值
        public float PlaySpeed
        {
            set { SetPlaySpeed(value); }
            get { return playSpeed; }
        }

        [Obsolete]
        public int refGameObjectsCount = -1;  //传入对象个数  可以考虑废弃直接使用数组长度
        [ReadOnly]
        public int callbackID = 0;
        public GameObject sceneRoot { get; set; }
        #endregion

        public Action()
        {
            _instanceID = ++_unuseInstanceID;
        }

        public void Reset()
        {
            length = 5.0f;

            enabled = true;
            deltaTime = 0.0f;
            time = 0.0f;
            gameObjects.Clear();
            templateObjectIds.Clear();
            refParams.refParamList.Clear();
            refParams.refDataList.Clear();
            if (tempObjsAffectedByPlaySpeed.Count != 0)
            {
                tempObjsAffectedByPlaySpeed.Clear();
            }
            if (tracks.Count != 0)
            {
                tracks.Clear();
            }
            if (conditions.Count != 0)
            {
                conditions.Clear();
            }
            conditionChanged = true;
            if (_subActions.Count != 0)
            {
                _subActions.Clear();
            }

            _instanceID = 0;

            bWaitWhenPlayEnd = false;
            IsWaitingForEnd = false;
            IsFinished = false;

            playSpeed = 1.0f;

            refGameObjectsCount = -1;
            callbackID = 0;
            gameObjectsScale.Clear();
            gameObjectsAynsLoad.Clear();
        }

        public int GetInstanceID()
        {
            return _instanceID;
        }

        public void ForceStart()
        {
            time = 0.0f;
            IsWaitingForEnd = false;
            bWaitWhenPlayEnd = false;
            IsFinished = false;
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    Track track = tracks[i];
                    if (track.execOnForceStopped || track.execOnActionCompleted)
                        continue;
                    if (track.waitForConditions.Count == 0)
                        track.Start(this);
                }
            }
        }

        public Action PlaySubAction(string _actionName, params GameObject[] _gameObjects)
        {
            var action = ActionService.GetInstance().PlayAction(_actionName, _gameObjects);
            if(action != null)
            {
                _subActions.Add(action);
                if(Mathf.Abs(action.playSpeed - playSpeed) < SpeedEpsilon)
                {
                    action.PlaySpeed = playSpeed;
                }
            }
            return action;
        }

        public virtual void Pause()
        {
            if (!enabled)
                return;
            enabled = false;
            SetPlaySpeed(0.0f);
        }

        public void StopSubAction()
        {
            for (int i = 0, imax = _subActions.Count; i < imax; i++)
            {
                if (ActionService.GetInstance().IsActionValid(_subActions[i]))
                {
                    ActionService.GetInstance().StopAction(_subActions[i]);
                }
            }
            _subActions.Clear();
        }

        public virtual void Stop(bool forceStop = false)
        {
            StopSubAction();

            //如果在最后一帧停留就不允许stop
            if (bWaitWhenPlayEnd)
            {
                if(!IsWaitingForEnd)
                {
                    IsWaitingForEnd = true;
                    ForceUpdate(length+0.033f);
					if (tempObjsAffectedByPlaySpeed != null)
					{
						RestoreFxSpeed();
						tempObjsAffectedByPlaySpeed.Clear();
					}
				}
				return;
			}

            IsFinished = true;

            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    Track track = tracks[i];
                    if (!track.enabled)
                    {
                        continue;
                    }

					if ((forceStop && track.execOnForceStopped) || (!forceStop && track.execOnActionCompleted))
                    {
                        track.Start(this);
                        float oldDeltaTime = deltaTime;
                        deltaTime = length + SpeedEpsilon;
                        track.Process(deltaTime);
                        track.Stop(this);
                        deltaTime = oldDeltaTime;
                    }
                    else if (track.started)
                        track.Stop(this);
                }
            }

            if (tempObjsAffectedByPlaySpeed != null)
            {
                RestoreFxSpeed();
                tempObjsAffectedByPlaySpeed.Clear();
            }

            Type trackType = typeof(Track);
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    ActionClassPoolManager.Instance.GetActionClassPool(trackType).ReleaseActionObject(tracks[i]);
                }
                tracks.Clear();
            }
            conditions.Clear();

            if(callbackID != 0)
            {
                LuaService.GetInstance().Interaction.OnPlayActionCompleted(callbackID);
                callbackID = 0;
            }

            if (OnCompleteEvent != null)
            {
                OnCompleteEvent();
            }
        }

        public void Update()
        {
            if (!enabled || playSpeed <= SpeedEpsilon || Time.timeScale <= SpeedEpsilon)
                return;

            float newTime = time + Time.deltaTime * playSpeed;
            if(bWaitWhenPlayEnd && newTime > length)
            {
                IsWaitingForEnd = true;
            }
            else
            {
                ForceUpdate(newTime);
            }
        }

        public void ForceUpdate(float _time)
        {
			float lastTime = time;
            deltaTime = _time - time;
            time = _time;
            
            if (time > length)
            {
                if (loop)
                {
                    time -= length;
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        Track track = tracks[i];
                        if (track.waitForConditions.Count == 0)
                            track.DoLoop();
                    }
                }
                else
                {
                    time = length + SpeedEpsilon;
					deltaTime = time - lastTime;
                    Process(time);
                    for (var i = _subActions.Count - 1; i > 0; i--)
                    {
                        var subAction = _subActions[i];
                        if (subAction.IsFinished)
                        {
                            _subActions.RemoveAt(i);
                        }
                    }
                    if (_subActions.Count == 0)
                        Stop();
                    return;
                }
            }
            Process(time);
        }

        public void ResetGameObjects(GameObject[] _gameObjects)
        {
            gameObjects.Clear();
            if(_gameObjects != null)
            {
                for (int i = 0; i < _gameObjects.Length; i++)
                {
                    gameObjects.Add(_gameObjects[i]);
                }
            }
        }

        public void ResetGameObjects(ListView<GameObject> _gameObjects)
        {
            gameObjects.Clear();
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                gameObjects.Add(_gameObjects[i]);
            }
            refGameObjectsCount = gameObjects.Count;
        }

        public void SetOrAddGameObject(int index, GameObject go)
        {
            if (index < 0)
            {
                return;
            }

            if(index >= gameObjects.Count)
            {
                for(int i = gameObjects.Count -1; i < index;i++)
                {
                    gameObjects.Add(null);
                }
            }

            gameObjects[index] = go;
            refGameObjectsCount = gameObjects.Count;
        }

        public void Process(float _time)
        {
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    Track track = tracks[i];
                    if (track.waitForConditions.Count == 0)
                    {
                        if (track.started && track.enabled)
                            track.Process(_time);
                    }
                    else
                    {
                        if (conditionChanged && !track.started && track.CheckConditions(this) && !track.execOnActionCompleted && !track.execOnForceStopped)
                        {
                            track.Start(this);
                            if (!loop)
                            {
                                float trackLength = track.GetEventEndTime();
                                if (length < trackLength)
                                    length = trackLength;
                            }
                        }
                        if (track.started)
                        {
                            track.Process(track.curTime + deltaTime);
                            float trackLength = track.GetEventEndTime();
                            if (track.curTime > trackLength && !track.HasConditionCount(this))
                                track.Stop(this);
                        }
                    }
                }
            }
            conditionChanged = false;
        }

        public Track AddTrack(string _eventTypeName)
        {
            Track newTrack = new Track(this, _eventTypeName);
            newTrack.trackIndex = tracks.Count;
            tracks.Add(newTrack);
            conditions.Add(newTrack, false);
            return newTrack;
        }

        public Track AddTrack(Track _track)
        {
            _track.trackIndex = tracks.Count;
            tracks.Add(_track);
            conditions.Add(_track, false);
            return _track;
        }

        public GameObject GetGameObject(int _index)
        {
            if (_index < 0)
            {
                return null;
            }
            if (_index >= gameObjects.Count)
            {
                if (templateObjectIds.Count <= _index || !sceneRoot)
                {
                    return null;
                }
                var templateName = templateObjectIds[_index];
                var obj = sceneRoot.FindChildBFS(templateName);
                if (obj == null)
                {
                    Log.LogE("AGE", "{0} not found.", templateName);
                    return null;
                }
                var camera = obj.GetComponent<Camera>();
                if (camera != null)
                {
                    camera = CameraSystem.instance.mainCamera;
                    if (camera != null)
                    {
                        return camera.gameObject;
                    }
                }
                return obj.gameObject;
            }
            else 
                return gameObjects[_index];
        }

        public float CurrentTime
        {
            get
            {
                return time;
            }
        }

        public void GetTracks(Type evtType, ref List<Track> resLst)
        {
            if (resLst == null)
                resLst = new List<Track>();
            for (int i = 0; i < tracks.Count; i++)
            {
                Track track = tracks[i];
                if (track != null && track.EventType == evtType)
                    resLst.Add(track);
            }
        }

        public bool GetCondition(Track _track)
        {
            return conditions[_track];
        }

        public int GetConditionCount()
        {
            return conditions.Count;
        }

        public void SetCondition(Track _track, bool _status)
        {
            if (!conditions.ContainsKey(_track))
            {
                return;
            }

            bool oldStatus = conditions[_track];
            if (oldStatus != _status)
            {
                conditionChanged = true;
                conditions[_track] = _status;
            }
        }

        public void CopyRefParam(Action resource)
        {
            refParams.refParamList.Clear();
            refParams.refDataList.Clear();

            var refParamIter = resource.refParams.refParamList.GetOriEnumerator();
            while (refParamIter.MoveNext())
            {
                RefParamObject refParam = (RefParamObject)refParamIter.Current.Value;
                refParams.AddRefParam(refParamIter.Current.Key, refParam.value);
            }

            var refDataIter = resource.refParams.refDataList.GetOriEnumerator();
            while (refDataIter.MoveNext())
            {
                string k = refDataIter.Current.Key;
                ListView<RefData> dl = resource.refParams.refDataList[k];
                for (int idx = 0; idx < dl.Count; idx++)
                {
                    RefData data = dl[idx];
                    if (data.dataObject is Track)
                    {
                        Track _track = (Track)(data.dataObject);
                        refParams.AddRefData(k, data.fieldInfo, tracks[_track.trackIndex]);
                    }
                    else if (data.dataObject is BaseEvent)
                    {
                        BaseEvent _event = (BaseEvent)(data.dataObject);
                        int eid = _event.track.GetIndexOfEvent(_event);
                        Track ct = tracks[_event.track.trackIndex];
                        BaseEvent ce = ct.GetEvent(eid);
                        refParams.AddRefData(k, data.fieldInfo, ce);
                    }
                }
            }
        }

        public void AddTemplateObject(string str)
        {
            templateObjectIds.Add(str);
        }

        //get resource names/paths associated with this action
        //result.key stands for resource paths
        //result.value stands for whether the resource needs to be re-loaded
        public Dictionary<string, bool> GetAssociatedResources()
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>();
            for (int i = 0; i < tracks.Count; i++)
            {
                Track track = tracks[i];

                if (!track.enabled)
                {
                    continue;
                }

                Dictionary<string, bool> trackResources = track.GetAssociatedResources();
                if (trackResources != null)
                {
                    Dictionary<string, bool>.Enumerator trackResIter = trackResources.GetEnumerator();
                    while (trackResIter.MoveNext())
                    {
                        string resName = trackResIter.Current.Key;
                        if (result.ContainsKey(resName))
                            result[resName] |= trackResources[resName];
                        else
                            result.Add(resName, trackResources[resName]);
                    }
                }
            }
            return result;
        }

        public List<string> GetAssociatedAction()
        {
            List<string> result = new List<string>();
            for (int i = 0; i < tracks.Count; i++)
            {
                Track track = tracks[i];

                if (!track.enabled)
                {
                    continue;
                }

                List<string> trackActions = track.GetAssociatedAction();
                if (trackActions != null)
                {
                    for (int j = 0; j < trackActions.Count; j++)
                    {
                        string acName = trackActions[j];
                        if (!result.Contains(acName))
                            result.Add(acName);
                    }
                }
            }
            return result;
        }
        
        public void AddTempObject(PlaySpeedAffectedType type, GameObject obj)
        {
            if (obj == null)
                return;

            if (tempObjsAffectedByPlaySpeed == null)
                tempObjsAffectedByPlaySpeed = new DictionaryView<uint, ListView<GameObject>>();
            if (!tempObjsAffectedByPlaySpeed.ContainsKey((uint)type))
                tempObjsAffectedByPlaySpeed.Add((uint)type, new ListView<GameObject>());
            ListView<GameObject> tempObjects = tempObjsAffectedByPlaySpeed[(uint)type];
            for (int i = 0; i < tempObjects.Count; i++)
            {
                if (tempObjects[i] == obj)
                    return;
            }
            tempObjects.Add(obj);

            //update temp object at once
            if (type == PlaySpeedAffectedType.ePSAT_Anim)
            {
                Animation[] animations = obj.GetComponentsInChildren<Animation>();
                for (int i = 0; i < animations.Length; i++)
                {
                    Animation animation = animations[i];
                    if (animation.playAutomatically && animation.clip)
                    {
                        AnimationState state = animation[animation.clip.name];
                        if (state)
                            state.speed = playSpeed;
                    }
                }

                Animator[] animators = obj.GetComponentsInChildren<Animator>();
                for (int i = 0; i < animators.Length; i++)
                {
                    animators[i].speed = playSpeed;
                }
            }
            else
            {
                ParticleSystem[] pslst = obj.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < pslst.Length; i++)
                {
                    pslst[i].playbackSpeed = playSpeed;
                }
            }
        }

        public void RemoveTempObject(PlaySpeedAffectedType type, GameObject obj)
        {
            if (tempObjsAffectedByPlaySpeed == null)
                return;
            if (!tempObjsAffectedByPlaySpeed.ContainsKey((uint)type))
                return;
            tempObjsAffectedByPlaySpeed[(uint)type].Remove(obj);
        }

        void RestoreFxSpeed()
        {
            if (tempObjsAffectedByPlaySpeed != null)
            {
                var tempObjIter = tempObjsAffectedByPlaySpeed.GetOriEnumerator();
                while (tempObjIter.MoveNext())
                {
                    PlaySpeedAffectedType type = (PlaySpeedAffectedType)tempObjIter.Current.Key;
                    if (type == PlaySpeedAffectedType.ePSAT_Fx)
                    {
                        var tempObjects = tempObjsAffectedByPlaySpeed[(uint)type];
                        for (int oidx = 0; oidx < tempObjects.Count; oidx++)
                        {
                            GameObject obj = tempObjects[oidx];
                            if (obj == null) continue;
#if UNITY_EDITOR
                            if(!Application.isPlaying)
                            {
                                GameObject.DestroyImmediate(obj);
                            }
                            else
#endif
                            {
                                LifeTimeHelper life = obj.GetComponent<LifeTimeHelper>();
                                if (life != null)
                                {
									// 通知下action结束，准备删除
                                    life.ActionStop(length - CurrentTime);
                                }

                                if (life == null || life.destroyAtActionStop)
                                {
                                    ActionService.GetInstance().DestroyGameObject(obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        void UpdateTempObjectSpeed()
        {
            if (tempObjsAffectedByPlaySpeed == null)
                return;
            var tempObjIter = tempObjsAffectedByPlaySpeed.GetOriEnumerator();
            while (tempObjIter.MoveNext())
            {
                PlaySpeedAffectedType type = (PlaySpeedAffectedType)tempObjIter.Current.Key;
                var tempObjects = tempObjsAffectedByPlaySpeed[(uint)type];
                for (int oid = 0; oid < tempObjects.Count; oid++)
                {
                    GameObject obj = tempObjects[oid];
                    if (obj == null) continue;

                    if (type == PlaySpeedAffectedType.ePSAT_Anim)
                    {
                        Animation[] animations = obj.GetComponentsInChildren<Animation>();
                        for (int i = 0; i < animations.Length; i++)
                        {
                            Animation animation = animations[i];
                            if (animation.playAutomatically && animation.clip)
                            {
                                AnimationState state = animation[animation.clip.name];
                                if (state)
                                    state.speed = playSpeed;
                            }
                        }

                        Animator[] animators = obj.GetComponentsInChildren<Animator>();
                        for (int i = 0; i < animators.Length; i++)
                        {
                            animators[i].speed = playSpeed;
                        }
                    }
                    else if (type == PlaySpeedAffectedType.ePSAT_Fx)
                    {
                        ParticleSystem[] pslst = obj.GetComponentsInChildren<ParticleSystem>();
                        for (int i = 0; i < pslst.Length; i++)
                        {
                            pslst[i].playbackSpeed = playSpeed;
                        }
                    }
                }
            }
        }

		public void SetPlaySpeed(float _speed,bool effectSubAction = true)
		{
#if UNITY_EDITOR
            if (_speed > MAX_ACTION_SPEED)
            {
                Log.LogE("AGE", "[ATTENSION]: Action Speed is too large:" + _speed + "for action: " + actionName);
                _speed = Mathf.Clamp(_speed, 0.0f, MAX_ACTION_SPEED);
            }
#endif
            if(IsFinished)
            {
                Log.LogE("AGE", "the action is finished:" + actionName);
                return;
            }

            playSpeed = _speed;
            if (playSpeed <= SpeedEpsilon)
            {
                ForceUpdate(time);
                enabled = false;
            }
            else
            {
                enabled = true;
            }

            UpdateTempObjectSpeed();

			if(effectSubAction)
			{
				for (int i = 0, imax = _subActions.Count; i < imax; i++)
				{
					if (ActionService.GetInstance().IsActionValid(_subActions[i]))
					{
						_subActions[i].PlaySpeed = playSpeed;
					}
				}
			}
		}

        public float GetActorInfoScale(int index)
        {
            float res;
            if (false == gameObjectsScale.TryGetValue(index, out res))
            {
                res = 1;
            }

            return res;
        }


        // 加载好了，通知全部等待的TickEvent
        public void UpdateWaitAsyn(int targetId, bool state)
        {
            if (targetId > 0 )
            {
                // 加载结束
                gameObjectsAynsLoad[targetId] = state;
            }
        }

        // 外面获取是否正在异步加载
        public bool IsAynsloadTarget(int targetId)
        {
            bool aynsload;
            if (gameObjectsAynsLoad.TryGetValue(targetId, out aynsload))
            {
                return aynsload;
            }

            return false;
        }

        public void UpdateTempObjectForPreview(float _oldProgress, float _newProgress)
        {
#if UNITY_EDITOR
            if (tempObjsAffectedByPlaySpeed == null)
                return;
            var tempObjIter = tempObjsAffectedByPlaySpeed.GetOriEnumerator();
            while (tempObjIter.MoveNext())
            {
                PlaySpeedAffectedType type = (PlaySpeedAffectedType)tempObjIter.Current.Key;
                var tempObjects = tempObjsAffectedByPlaySpeed[(uint)type];
                for (int oid = 0; oid < tempObjects.Count; oid++)
                {
                    GameObject obj = tempObjects[oid];
                    if (obj == null) continue;
                    LifeTimeHelper lifeTimeHelper = obj.GetComponent<LifeTimeHelper>();

                    if (type == PlaySpeedAffectedType.ePSAT_Anim)
                    {
                        Animation[] animations = obj.GetComponentsInChildren<Animation>();
                        for (int i = 0; i < animations.Length; i++)
                        {
                            Animation animation = animations[i];
                            if (animation.playAutomatically && animation.clip && !animation.isPlaying)
                            {
                                animation.clip.SampleAnimation(animation.gameObject, _newProgress - lifeTimeHelper.startTime);
                            }
                        }

                        Animator[] animators = obj.GetComponentsInChildren<Animator>();

                        for (int aid = 0; aid < animators.Length; aid++)
                        {
                            Animator animtor = animators[aid];
                            for (int i = 0; i < animtor.layerCount; ++i)
                            {
                                AnimatorStateInfo sinfo = animtor.GetCurrentAnimatorStateInfo(i);
                                animtor.Play(sinfo.nameHash);
                                AnimatorClipInfo[] infos = animtor.GetCurrentAnimatorClipInfo(i);
                                for (int ifid = 0; ifid < infos.Length; ifid++)
                                {
                                    AnimatorClipInfo info = infos[ifid];
                                    info.clip.SampleAnimation(animtor.gameObject, _newProgress - lifeTimeHelper.startTime);
                                }
                            }
                        }
                    }
                    else if (type == PlaySpeedAffectedType.ePSAT_Fx)
                    {
                        ParticleSystem[] pslst = obj.GetComponentsInChildren<ParticleSystem>();
                        for (int pid = 0; pid < pslst.Length; pid++)
                        {
                            pslst[pid].Simulate((_newProgress - _oldProgress) / playSpeed, false, false);
                        }
                    }
                }
            }
#endif
        }
    }
}
