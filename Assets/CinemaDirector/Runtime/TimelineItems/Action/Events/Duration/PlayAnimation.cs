using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;
using Sirenix.OdinInspector;

namespace AGE
{
    [CutsceneItem("Animation", "PlayAnimation", CutsceneItemGenre.GenericItem)]
    public class PlayAnimation : DurationEvent
	{
        [Template]
		public int targetId = 0;
        [CustomValueDrawer("DrawAnimation")]
		public string clipName = "";
        public string backUpClipName = "";

        public float startTime = 0.0f;
		public float endTime = 99999.0f;

        public bool applyActionSpeed = false;
        public bool useChildAnimation = false;
		public bool autoDisableAnimation = true;
        public AnimationCullingType animationCullingType;
        private AnimationState clipState = null;
        private Animation targetAnim = null;
        [ShowInInspector]
        private GameObject targetObject = null;
        private Transform targetTrans = null;

        private bool _checkAsynObj = false;
        private string finalClipName = "";

        private string DrawAnimation(string value, GUIContent label)
        {
            return TimelineItemCustomDraw.DrawAnimation(Cutscene, targetId, value, label);
        }
        
        public bool BindAnim(Action _action)
        {
            if(targetObject != null && targetAnim != null && targetTrans != null && clipState != null)
            {
                return true;
            }

            targetObject = _action.GetGameObject(targetId);
            if (targetObject == null)
            {
                Log.LogE("AGE", " Action:[{0}] Event PlayAnimationTick can't find targetObject!", _action.actionName);
                return false;
            }

            // 是临时的，要等下设置正式的时候再处理
            if (_action.IsAynsloadTarget(targetId))
            {
                _checkAsynObj = true;
                return false;
            }

            if (useChildAnimation)
            {
                Animation targetAni = targetObject.transform.GetComponentInChildren<Animation>();
                if (targetAni != null)
                {
                    targetObject = targetAni.gameObject;
                }
                else
                {
                    Log.LogE("AGE", " Action:[{0}] Event PlayAnimationTick can't find targetObject's Animation!", _action.name);
                }
            }

            targetTrans = targetObject.transform;
            if (clipState == null || targetAnim == null)
            {
                targetAnim = targetObject.GetComponent<Animation>();
                if (targetAnim == null)
                {
                    Log.LogE("AGE", " Action:[{0}] Event PlayAnimationTick can't find animation of target!", _action.name);
                    return false;
                }
                targetAnim.cullingType = animationCullingType;
                string lower = clipName.ToLower();
                string backupLower = backUpClipName.ToLower();
                bool findClip = false;
                bool findBackUpClip = false;
                if(string.IsNullOrEmpty(backupLower))
                {
                    findBackUpClip = true;
                }

                var em = targetAnim.GetEnumerator();
                while (em.MoveNext())
                {
                    if (em.Current == null)
                    {
                        continue;
                    }

                    AnimationState state = em.Current as AnimationState;
                    if (state == null || state.clip == null)
                    {
                        continue;
                    }

                    if (state.clip.name.ToLower() == lower)
                    {
                        clipName = state.clip.name;
                        findClip = true;
                    }

                    if (state.clip.name.ToLower() == backupLower)
                    {
                        backUpClipName = state.clip.name;
                        findBackUpClip = true;
                    }

                    if(findClip && findBackUpClip)
                    {
                        break;
                    }
                }

                if (targetAnim.GetClip(clipName) == null && targetAnim.GetClip(backUpClipName) == null)
                {
                    var trans = targetTrans.parent;
                    Log.LogE("AGE", " Failed to find animation clip: <color=red>[ {0} ]</color>! " + " for TargetObject: <color=red>[ {1}  -- Parent: {3} ] </color> " +
                                   "by Action:<color=yellow>[ {2} ] </color>", clipName, targetObject.name, _action.actionName, (trans != null) ? trans.name : "Null");
                    return false;
                }

                clipState = targetAnim[clipName];
                finalClipName = clipName;
                if (clipState == null)
                {
                    clipState = targetAnim[backUpClipName];
                    finalClipName = backUpClipName;
                }
            }

            return clipState != null && targetAnim != null;
        }

        public override void OnActionStart(Action _action)
        {
            applyActionSpeed = true;
            targetObject = null;
            targetAnim = null;
            targetTrans = null;
            clipState = null;
        }

        public override void Enter(Action _action, Track _track)
		{
            _checkAsynObj = false;

            if (!BindAnim(_action))
            {
                return;
            }

#if UNITY_EDITOR
            if (_action.state == CutsceneState.Playing)
#endif
            {
                targetAnim.Stop();
                targetAnim.Play(finalClipName);

				if (startTime < 0)
					startTime = 0;
				if (startTime > clipState.length)
					startTime = clipState.length;
				if (endTime > clipState.length)
					endTime = clipState.length;
				if (endTime < startTime)
					endTime = startTime;

				float playLength = endTime - startTime;

                float localTime = _track.curTime - Start;
                clipState.speed = playLength / length * (applyActionSpeed ? _action.PlaySpeed : 1.0f);
                clipState.time = startTime + localTime * clipState.speed;
                clipState.enabled = true;
			}
		}

		public override void EnterBlend (Action _action, Track _track, BaseEvent _prevEvent, float _blendTime)
		{
            if (!BindAnim(_action))
            {
                return;
            }

#if UNITY_EDITOR
            if (_action.state == CutsceneState.Playing)
#endif
            {
                targetAnim.CrossFade(finalClipName, _blendTime);

				if (startTime < 0)
					startTime = 0;
				if (startTime > clipState.length)
					startTime = clipState.length;
				if (endTime > clipState.length)
					endTime = clipState.length;
				if (endTime < startTime)
					endTime = startTime;

				float playLength = endTime - startTime;

                float localTime = _track.curTime - Start;
				clipState.speed = playLength / length * (applyActionSpeed ? _action.PlaySpeed : 1.0f);
                clipState.time = startTime + localTime * clipState.speed;
                
                clipState.enabled = true;
			}
		}

		public override void Process (Action _action, Track _track, float _localTime)
        {
            // 如果开着等待异步加载
            if (_checkAsynObj)
            {
                // 反复找targetAnim
                if (BindAnim(_action))
                {
                    // 找到了，完事，播放
                    _checkAsynObj = false;

#if UNITY_EDITOR
                    if (_action.state == CutsceneState.Playing)
#endif
                    {
                        targetAnim.Stop();
                        targetAnim.Play(finalClipName);

                        if (startTime < 0)
                            startTime = 0;
                        if (startTime > clipState.length)
                            startTime = clipState.length;
                        if (endTime > clipState.length)
                            endTime = clipState.length;
                        if (endTime < startTime)
                            endTime = startTime;

                        float playLength = endTime - startTime;

                        float localTime = _track.curTime - Start;
                        clipState.speed = playLength / length * (applyActionSpeed ? _action.PlaySpeed : 1.0f);
                        clipState.time = startTime + localTime * clipState.speed;
                        clipState.enabled = true;
                    }
                }
                else
                {
                    return;
                }
            }

            if (targetObject == null || targetAnim == null || targetTrans == null || clipState == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (_action.state != CutsceneState.Playing)
            {
                if (startTime < 0)
                    startTime = 0;
                if (startTime > clipState.length)
                    startTime = clipState.length;
                if (endTime > clipState.length)
                    endTime = clipState.length;
                if (endTime < startTime)
                    endTime = startTime;
				clipState.enabled = true;
                float playLength = endTime - startTime;
                clipState.speed = playLength / length * (applyActionSpeed ? _action.PlaySpeed : 1.0f);
                clipState.time = startTime + _localTime * clipState.speed;
                clipState.clip.SampleAnimation(targetObject, startTime + _localTime * playLength / length / (applyActionSpeed ? 1.0f : _action.PlaySpeed));
            }
            else
#endif
            {
                float playLength = endTime - startTime;
                clipState.speed = playLength / length * (applyActionSpeed ? _action.PlaySpeed : 1.0f);
            }
        }

		public override void Leave (Action _action, Track _track)
		{
            if(clipState == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (_action.state == CutsceneState.Playing)
#endif
            {
				if(autoDisableAnimation)
				{
					clipState.enabled = false;
				}
			}
		}
		
		protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as PlayAnimation;
            targetId = srcCopy.targetId;
            clipName = srcCopy.clipName;
            backUpClipName = srcCopy.backUpClipName;
            startTime = srcCopy.startTime;
            endTime = srcCopy.endTime;
            applyActionSpeed = srcCopy.applyActionSpeed;
            useChildAnimation = srcCopy.useChildAnimation;
			autoDisableAnimation = srcCopy.autoDisableAnimation;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
            clipName = "";
            backUpClipName = "";
            startTime = 0.0f;
            endTime = 99999.0f;
            applyActionSpeed = false;
            useChildAnimation = false;
			autoDisableAnimation = true;
            clipState = null;
            targetAnim = null;
            targetObject = null;
            targetTrans = null;
            finalClipName = "";
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }
    }
}

