using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{

    public class GetSubObjectTick : TickEvent
    {

        [Template]
        public int targetId = -1;

        [Template]
        public int parentId = -1;

        public bool isGetByName = false;
        public string subObjectName = "Mesh";
        public bool isGetByActice = false;
        public bool recoverAtStop = false;
        public bool resetTransform = false;
        public int objectIndex = 0;

        private GameObject subObject;
        private Vector3 recoverPos;
        private Vector3 recoverRot;
        private Vector3 recoverSca;

        public override bool SupportEditMode()
        {
            return true;
        }

        public override void Process(Action _action, Track _track)
        {
            GameObject parentObject = _action.GetGameObject(parentId);
            if (parentObject == null) return;

            while (targetId >= _action.gameObjects.Count)
                _action.gameObjects.Add(null);

            subObject = _action.GetGameObject(targetId);
            if (isGetByName)
            {
                Transform[] transforms = parentObject.ExtGetComponentsInChildren<Transform>(true);
                if(transforms != null)
                {
                    int findIndex = 0;
                    for (int i = 0; i < transforms.Length; ++i)
                    {
                        GameObject go = transforms[i].gameObject;
                        if (go != parentObject && go.name == subObjectName && (false == isGetByActice || go.activeSelf))
                        {
                            if(objectIndex == findIndex)
                            {
                                subObject = go;
                                break;
                            }
                            findIndex++;
                        }
                    }
                }
            }
            else
            {
                Transform parentTrans = parentObject.transform;
                for (int i = 0; i < parentTrans.childCount; i++)
                {
                    GameObject go = parentTrans.GetChild(i).gameObject;
                    if (!isGetByActice || go.activeSelf)
                    {
                        subObject = go;
                    }
                }
            }

            if (subObject == null)
            {
                Log.LogE("AGE", " Warning: Failed to find sub object by name: <color=red>[ " + subObjectName + "--parent:" + parentObject.name + " ]</color>! "
                                  + " for Event: <color=red>[ " + this.GetType() + " ] </color> "
                                  + "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                return;
            }


            Transform trans = subObject.transform;
            if (recoverAtStop)
            {
                recoverPos = trans.position;
                recoverRot = trans.rotation.eulerAngles;
                recoverSca = trans.localScale;
            }

            if(resetTransform)
            {
                trans.position = Vector3.zero;
                trans.rotation = Quaternion.identity;
                trans.localScale = Vector3.one;
            }

            if (targetId >= 0 && targetId < _action.gameObjects.Count)
            {
                _action.gameObjects[targetId] = subObject;
            }
            else
            {
                string msg = string.Format("Target id is out of range, targetId: {0}, gameobjects list count: {1}", targetId, _action.gameObjects.Count);
                Log.LogE("AGE", msg + ", for Event: <color=red>[ " + this.GetType()+ " ] </color> " +
                                       "by Action:<color=yellow>[ " + _action.name + " ] </color>");
            }

        }

        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as GetSubObjectTick;
            targetId = srcCopy.targetId;
            parentId = srcCopy.parentId;
            isGetByName = srcCopy.isGetByName;
            subObjectName = srcCopy.subObjectName;
            isGetByActice = srcCopy.isGetByActice;
            recoverAtStop = srcCopy.recoverAtStop;
            resetTransform = srcCopy.resetTransform;
            objectIndex = srcCopy.objectIndex;

            subObject = null;
        }

        public override void OnActionStop(Action _action)
        {
            if (recoverAtStop && subObject != null)
            {
                Transform trans = subObject.transform;
                trans.position = recoverPos;
                trans.rotation = Quaternion.Euler(recoverRot);
                trans.localScale = recoverSca;
            }
        }

        protected override void ClearData()
        {
            targetId = -1;
            parentId = -1;
            isGetByName =false;
            subObjectName = "Mesh";
            isGetByActice = false;
            objectIndex = 0;
        }
    }

}
