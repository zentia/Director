using UnityEngine;
using System.Collections;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{

    [EventCategory("Alpha")]
    public class InheritTransformDuration : DurationEvent
    {
        [Template]
        public int targetId = -1;
        [Template]
        public int parentId = -1;

        public bool inheritTranslation = true;
        public bool cachePosY = false;
        public bool inheritRotation = false;
        public bool inheritScaling = false;

        public bool modifyTranslation = false;
        public Vector3 translation = Vector3.zero;
        public bool modifyRotation = false;
        public Quaternion rotation = Quaternion.identity;
        public bool modifyScaling = false;
        public Vector3 scaling = Vector3.one;

        private float _cachePosY = 0f;
        public override void Enter(Action _action, Track _track)
        {
            if(cachePosY)
            {
                GameObject parentObject = _action.GetGameObject(parentId);

                if(parentObject != null)
                {
                    Transform parentTrans = parentObject.transform;
                    Vector3 pos = Vector3.zero;
                    if (inheritTranslation && parentTrans != null)
                    {
                        if (modifyTranslation)
                            pos = parentTrans.localToWorldMatrix.MultiplyPoint(translation);
                        else
                            pos = parentTrans.position;
                        _cachePosY = pos.y;
                    }
                }

            }
          
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            GameObject parentObject = _action.GetGameObject(parentId);

            if (targetObject == null || parentObject == null)
            {
                Log.LogE("AGE", " Failed to find target object or parentObject: " + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
               "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                return;
            }

            Transform targetTrans = targetObject.transform;
            Transform parentTrans = parentObject.transform;
            if (inheritTranslation)
            {
                Vector3 pos = Vector3.zero;
                if (modifyTranslation)
                    pos = parentTrans.localToWorldMatrix.MultiplyPoint(translation);
                else
                    pos = parentTrans.position;
               
                if (cachePosY)
                {
                    pos.y = _cachePosY;
                }

                targetTrans.position = pos;

            }

            if (inheritRotation)
            {
                if (modifyRotation)
                    targetTrans.rotation = parentTrans.rotation * rotation;
                else
                    targetTrans.rotation = parentTrans.rotation; 
               
            }

            if (inheritScaling)
            {
                if (modifyScaling)
                    targetTrans.localScale = scaling; 
                else
                    targetTrans.localScale = parentTrans.localScale; 
               
            }
        }

        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as InheritTransformDuration;
            targetId = srcCopy.targetId;
            parentId = srcCopy.parentId;
            inheritTranslation = srcCopy.inheritTranslation;
            cachePosY = srcCopy.cachePosY;
            inheritRotation = srcCopy.inheritRotation;
            inheritScaling = srcCopy.inheritScaling;
            modifyTranslation = srcCopy.modifyTranslation;
            translation = srcCopy.translation;
            modifyRotation = srcCopy.modifyRotation;
            rotation = srcCopy.rotation;
            modifyScaling = srcCopy.modifyScaling;
            scaling = srcCopy.scaling;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
            parentId = -1;
            inheritTranslation = true;
            inheritRotation = false;
            inheritScaling = false;
            modifyTranslation = false;
            translation = Vector3.zero;
            modifyRotation = false;
            rotation = Quaternion.identity;
            modifyScaling = false;
            scaling = Vector3.zero;
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }
    }

}