using UnityEngine;
using Assets.Plugins.Common;
using System.Collections.Generic;
using CinemaDirector;

namespace AGE
{
    public class CameraShakeDurationNew : DurationEvent
    {
        public bool useMainCamera = false;

        [Template]
        public int targetId = -1;

        public Vector3 shakeRange = Vector3.zero;
        public float shakeTime = 0;
        public float dampTime = 0;
        public float cycleTime = 0;

        private Vector3 originPos = Vector3.zero;
        private GameObject targetObject = null;
        private int positiveOrNegative = 1;
        private bool enterShaking = false;
        private float lastTime = 0;
        public override void Enter(Action _action, Track _track)
        {
            lastTime = 0;
            if (useMainCamera && Camera.main)
                targetObject = Camera.main.gameObject;
            else
                targetObject = _action.GetGameObject(targetId);

            if (targetObject == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            Transform targetTrans = targetObject.transform;
            if(targetTrans == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            enterShaking = true;
            originPos = targetTrans.localPosition;
        }

        Vector3 offset = Vector3.zero;
        public override void Process(Action _action, Track _track, float _localTime)
        {
            if (_localTime - lastTime < cycleTime)
            {
                return;
            }
            lastTime = _localTime;
            //相机在被其他修改时（如技能相机）不震动，避免冲突
            if (!enterShaking)
                return;
            offset = Vector3.zero;
            if (useMainCamera && Camera.main)
                targetObject = Camera.main.gameObject;
            else
                targetObject = _action.GetGameObject(targetId);

            if (targetObject == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            Transform targetTrans = targetObject.transform;
            if (targetTrans == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            positiveOrNegative *= -1;
            if (_localTime > shakeTime+dampTime)
            {
                targetTrans.localPosition = originPos;
                return;
            }
            else if (_localTime > shakeTime) //阻尼震动期间
            {
                float percent = dampTime / (_localTime - shakeTime) / 10;
                if (percent > 1)
                {
                    percent = 1;
                }
                RandomShakeValue(shakeRange * percent);
            }
            else //持续震动期间
            {
                RandomShakeValue(shakeRange);
            }
            targetTrans.localPosition = originPos + offset;
        }

        void RandomShakeValue(Vector3 shake)
        {
            offset.x += positiveOrNegative * Random.Range(0, shake.x);
            offset.y += positiveOrNegative * Random.Range(0, shake.y);
            offset.z += positiveOrNegative * Random.Range(0, shake.z);
        }

        ModifyTransform GetNearEvent(Action _action)
        {
            var tracks = new List<Track>();
            _action.GetTracks(typeof(ModifyTransform), ref tracks);
            foreach (var t in tracks)
            {
                if (!t.enabled)
                {
                    continue;
                }
                for (var i = t.trackEvents.Count - 1; i > 0; i--)
                {
                    ModifyTransform e = t.trackEvents[i] as ModifyTransform;
                    if (e.targetId != targetId)
                    {
                        break;
                    }
                    if (e.time < time + length)
                    {
                        return e;
                    }
                }
            }
            return null;
        }

        public override void Leave(Action _action, Track _track)
        {
            lastTime = 0;
            //相机在被其他修改时（如技能相机）不震动，避免冲突
            if (!enterShaking)
                return;

            if (useMainCamera && Camera.main)
                targetObject = Camera.main.gameObject;
            else
                targetObject = _action.GetGameObject(targetId);
            var modifyTransform = GetNearEvent(_action);
            if (modifyTransform != null)
            {

            }
            enterShaking = false;
            if (targetObject == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            Transform targetTrans = targetObject.transform;
            if (targetTrans == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            targetTrans.localPosition = originPos;
        }


        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as CameraShakeDurationNew;
            useMainCamera = srcCopy.useMainCamera;
            targetId = srcCopy.targetId;
            shakeRange = srcCopy.shakeRange;
            originPos = srcCopy.originPos;
            shakeTime = srcCopy.shakeTime;
            dampTime = srcCopy.dampTime;
            targetObject = srcCopy.targetObject;
            enterShaking = srcCopy.enterShaking;
        }

        protected override void ClearData()
        {
            base.ClearData();
            useMainCamera = false;
            targetId = -1;
            shakeRange = Vector3.zero;
            originPos = Vector3.zero;
            shakeTime = 0;
            dampTime = 0;
            targetObject = null;
            enterShaking = false;
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }
    }

}