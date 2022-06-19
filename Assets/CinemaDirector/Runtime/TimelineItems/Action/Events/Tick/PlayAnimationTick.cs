using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{

	[EventCategory("Animation")]
	public class PlayAnimationTick : TickEvent
	{
		public override bool SupportEditMode ()
		{
			return true;
		}

		[Template]
		public int targetId = 0;

        public string clipName = "";
        public string backUpClipName = "";
        public float crossFadeTime = 0.0f;
		public float playSpeed = 1.0f;

		public bool applyActionSpeed = false;
        public bool useChildAnimation = false;
        private AnimationState clipState = null;
        private Animation targetAnim = null;
        private GameObject targetObject = null;
        private Transform targetTrans = null;

        private string finalClipName = "";

        // 针对延迟加载特效的，延迟等待处理位置
        public override bool IsNeedWait(Action _action)
        {            
            return false;
        }

        public bool BindAnim(Action _action)
        {
            if (targetObject != null && targetAnim != null && targetTrans != null && clipState != null)
            {
                return true;
            }

            targetObject = _action.GetGameObject(targetId);
            if (targetObject == null)
            {
                return false;
            }

            if (useChildAnimation)
            {
                Animation targetAni = targetObject.transform.GetComponentInChildren<Animation>();
                if (targetAni != null)
                {
                    targetObject = targetAni.gameObject;
                }else
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

                string lower = clipName.ToLower();
                string backupLower = backUpClipName.ToLower();
                bool findClip = false;
                bool findBackUpClip = false;
                if (string.IsNullOrEmpty(backupLower))
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

                    if (findClip && findBackUpClip)
                    {
                        break;
                    }
                }

                if (targetAnim.GetClip(clipName) == null && targetAnim.GetClip(backUpClipName) == null)
                {
                    Transform trans = targetTrans.parent;
                    Log.LogE("AGE", " Failed to find animation clip: <color=red>[ {0} ]</color>! " + " for TargetObject: <color=red>[ {1}  -- Parent: {3} ] </color> " +
                                   "by Action:<color=yellow>[ {2} ] </color>", clipName, targetObject.name, _action.name, (trans != null) ? trans.name : "Null");
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
            targetObject = null;
            targetAnim = null;
            targetTrans = null;
            clipState = null;
        }

        public override void Process (Action _action, Track _track)
		{
            try
            {
                if (!BindAnim(_action))
                {
                    return;
                }

                string clipLower = finalClipName.ToLower();
                if (clipLower == "idle" && targetAnim.IsPlaying(finalClipName))
                {
                    return;
                }

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPaused)
#endif
                {

                    if (clipState.enabled)
                        targetAnim.Stop();//stop same animation to allow replay
                    if (crossFadeTime > 0)
                        targetAnim.CrossFade(finalClipName, crossFadeTime);
                    else
                        targetAnim.Play(finalClipName);
                    clipState.speed = playSpeed * (applyActionSpeed ? _action.PlaySpeed : 1.0f);
                }

                //if (clipLower == "idle" || clipLower == "run")
                //{
                //    clipState.time = Random.Range(0, clipState.length);
                //}
            }
            catch(System.Exception e)
            {
                Log.LogE("AGE", e.ToString());
            }
        }

		public override void ProcessBlend (Action _action, Track _track, TickEvent _prevEvent, float _blendWeight)
		{
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPaused)
			{
				PlayAnimationTick prevEvent = _prevEvent as PlayAnimationTick;
                if(!prevEvent.BindAnim(_action) || !BindAnim(_action))
                {
                    return;
                }

                float localTime = _action.CurrentTime - prevEvent.time;
				float reverceSpeed = 1f/prevEvent.playSpeed;
                prevEvent.clipState.clip.SampleAnimation(targetObject, localTime / reverceSpeed / (applyActionSpeed ? _action.PlaySpeed : 1.0f));
			}
#endif
		}

		public override void PostProcess (Action _action, Track _track, float _localTime)
		{
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPaused)
			{
                if (!BindAnim(_action))
                {
                    return;
                }

                clipState.clip.SampleAnimation(targetObject, _localTime * playSpeed / (applyActionSpeed ? _action.PlaySpeed : 1.0f ));
            }
#endif
        }


        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as PlayAnimationTick;

            targetId = copySrc.targetId;
            clipName = copySrc.clipName;
            backUpClipName = copySrc.backUpClipName;
            crossFadeTime = copySrc.crossFadeTime;
            playSpeed = copySrc.playSpeed;
            applyActionSpeed = copySrc.applyActionSpeed;
            useChildAnimation = copySrc.useChildAnimation;
        }

        protected override void ClearData()
        {
            targetId = -1;
            clipName = "";
            backUpClipName = "";
            crossFadeTime = 0.0f;
            playSpeed = 1.0f;
            applyActionSpeed = false;
            useChildAnimation = false;
            finalClipName = "";
        }

        protected override uint GetPoolInitCount()
        {
            return 20;
        }
    }
}
