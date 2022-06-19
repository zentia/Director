using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
    public class CameraShakeDuration : DurationEvent
    {
        public bool useMainCamera = false;

        [Template]
        public int targetId = -1;

        public Vector3 shakeRange = Vector3.zero;

        private Vector3 originPos = Vector3.zero;
        private float minDistance = 0.001f;
        private float shock = 0.01f;
        private float recovery = 0.1f;

        private GameObject targetObject = null;

        private bool enterShaking = false;

        public override void Enter(Action _action, Track _track)
        {
            if (useMainCamera && Camera.main)
                targetObject = Camera.main.gameObject;
            else
                targetObject = _action.GetGameObject(targetId);

            if (targetObject == null || targetObject.transform == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            enterShaking = true;

            originPos =   targetObject.transform.localPosition;
            shock = shakeRange.x;


        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            //相机在被其他修改时（如技能相机）不震动，避免冲突
            if (!enterShaking)
                return;

            if (useMainCamera && Camera.main)
                targetObject = Camera.main.gameObject;
            else
                targetObject = _action.GetGameObject(targetId);

            if (targetObject == null || targetObject.transform == null)
            {
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }


            if (shock >  minDistance)
            {
                Vector3 offset = new Vector3(Random.Range(-shock, shock), Random.Range(-shock, shock), 0.0f);
                targetObject.transform.localPosition = offset + originPos;

                shock *= 1.0f - recovery;
            }
            else
            {
                shock = 0.0f;
                targetObject.transform.localPosition = originPos;
            }
        }

        public override void Leave(Action _action, Track _track)
        {
            //相机在被其他修改时（如技能相机）不震动，避免冲突
            if (!enterShaking)
                return;

            if (useMainCamera && Camera.main)
                targetObject = Camera.main.gameObject;
            else
                targetObject = _action.GetGameObject(targetId);

            if (targetObject == null || targetObject.transform == null)
            {
                enterShaking = false;
                Log.LogE("AGE", " Event CameraShakeDuration failed to find camera object, by action: [" + _action.actionName + "].");
                return;
            }

            shock = 0.0f;

            targetObject.transform.localPosition = originPos;
            enterShaking = false;
        }


        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as CameraShakeDuration;
            useMainCamera = srcCopy.useMainCamera;
            targetId = srcCopy.targetId;
            shakeRange = srcCopy.shakeRange;
            originPos = srcCopy.originPos;
            shock = srcCopy.shock;
            recovery = srcCopy.recovery;
            targetObject = srcCopy.targetObject;
            enterShaking = srcCopy.enterShaking;
            minDistance = srcCopy.minDistance;
        }

        protected override void ClearData()
        {
            base.ClearData();
            useMainCamera = false;
            targetId = -1;
            shakeRange = Vector3.zero;
            originPos = Vector3.zero;
            shock = 0.01f;
            recovery = 0.1f;
            targetObject = null;
            enterShaking = false;
            minDistance = 0.001f;
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }
    }

}