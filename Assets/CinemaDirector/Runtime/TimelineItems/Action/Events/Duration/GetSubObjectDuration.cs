using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
    [EventCategory("Utility")]
    public class GetSubObjectDuration : DurationEvent
    {

        public override bool SupportEditMode()
        {
            return true;
        }
        [Template]
        public int targetId = -1;

        [Template]
        public int parentId = -1;

        public bool isGetByName = false;
        public string subObjectName = "";
        public bool isDynamic = false;
        public bool resetTempObj = false;

        public override void Enter(Action _action, Track _track)
        {
            GameObject parentObject = _action.GetGameObject(parentId);
            if (parentObject == null) return;

            if (targetId >= 0)
            {
                while (targetId >= _action.gameObjects.Count)
                    _action.gameObjects.Add(null);

                GameObject subObject = _action.GetGameObject(targetId);
                if (subObject != null && subObject.activeInHierarchy) return;

                if (isGetByName)
                {
                    subObject = GetChildByName(parentObject, subObjectName);

                    if (subObject == null)
                    {
                        Log.LogD("AGE", " Warning: Failed to find sub object by name: <color=red>[ " + subObjectName + "--parent:" + parentObject.name + " ]</color>! "
                                         + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> "
                                         + "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                    }
                }
                else
                {
                    if (parentObject.transform.childCount > 0)
                        subObject = parentObject.transform.GetChild(0).gameObject;
                }

                if (targetId >= 0 && targetId < _action.gameObjects.Count)
                {
                    _action.gameObjects[targetId] = subObject;
                }
                else
                {
                        string msg = string.Format("Target id is out of range, targetId: {0}, gameobjects list count: {1}", targetId, _action.gameObjects.Count);
                    Log.LogE("AGE", msg + ", for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
                                      "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                }
            }
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            if (isDynamic)
            {
                GameObject parentObject = _action.GetGameObject(parentId);
                if (parentObject == null)
                    return;

                GameObject subObject = _action.GetGameObject(targetId);
                if (subObject != null && subObject.activeInHierarchy)
                {
                    return;
                }

                if (isGetByName)
                {
                    subObject = GetChildByName(parentObject, subObjectName);
#if UNITY_EDITOR
                    if (subObject == null)
                    {

                        Log.LogD("AGE", " Warning: Failed to find sub object by name: <color=red>[ " + subObjectName + "--parent:" + parentObject.name + " ]</color>! "
                                         + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> "
                                         + "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                    }
#endif
                }
                else
                {
                    if (parentObject.transform.childCount > 0)
                        subObject = parentObject.transform.GetChild(0).gameObject;
                }

                if (targetId >= 0 && targetId < _action.gameObjects.Count)
                {
                    _action.gameObjects[targetId] = subObject;
                }
                else
                {
#if UNITY_EDITOR
                        string msg = string.Format("Target id is out of range, targetId: {0}, gameobjects list count: {1}", targetId, _action.gameObjects.Count);
                    Log.LogE("AGE", msg + ", for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
                                      "by Action:<color=yellow>[ " + _action.name + " ] </color>");
#endif
                }


            }
        }

        public override void Leave(Action _action, Track _track)
        {
            if (resetTempObj || targetId >= _action.refGameObjectsCount)
            {
                if (targetId >= 0 && _action.GetGameObject(targetId))
                    _action.gameObjects[targetId] = null;
            }

        }

        private GameObject GetChildByName(GameObject parent, string childName)
        {
            if (parent == null || childName == null)
            {
                return null;
            }

            Transform[] transforms = parent.GetComponentsInChildren<Transform>();
            for (int i = 0; i < transforms.Length; ++i)
            {
                if ((transforms[i].gameObject != null) && (transforms[i].gameObject.name == childName))
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as GetSubObjectDuration;
            targetId = srcCopy.targetId;
            parentId = srcCopy.parentId;
            isGetByName = srcCopy.isGetByName;
            subObjectName = srcCopy.subObjectName;
            isDynamic = srcCopy.isDynamic;
            resetTempObj = srcCopy.resetTempObj;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
            parentId = -1;
            isGetByName = false;
            subObjectName = "";
            isDynamic = false;
            resetTempObj = false;
        }
    }

}