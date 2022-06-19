/********************************************************************
	created:	2016/03/18
	created:	18:3:2016   14:47
	filename: 	C:\WorkSpace\Branch_Fight_Demo_Simple_20160219\unityproject\Assets\Plugins\AGE_Alpha_Extension\CopyObjectTick.cs
	file path:	C:\WorkSpace\Branch_Fight_Demo_Simple_20160219\unityproject\Assets\Plugins\AGE_Alpha_Extension
	file base:	CopyObjectTick
	file ext:	cs
	author:		jesslee
	
	purpose:	���ƽڵ�Action
*********************************************************************/
using UnityEngine;
using System.Collections;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
    public class CopyObjectTick : TickEvent
    {
        [Template]
        public int targetId = -1;

        [Template]
        public int parentId = -1;

        public bool isGetByName = false;
        public string subObjectName = "Bip001";
        public bool isGetByActice = false;

        [Template]
        public int templateObjectId = -1;

        public bool recreateExisting = true;

        public bool modifyTranslation = true;
        public Vector3 translation = Vector3.zero;
        public bool modifyRotation = false;
        public Quaternion rotation = Quaternion.identity;
        public bool modifyScaling = false;
        public Vector3 scaling = Vector3.one;

        public bool enableLayer = false;
        public int layer = 0;
        public bool enableTag = false;
        public string tag = "";

        public override void Process(Action _action, Track _track)
        {
            GameObject templateObj = _action.GetGameObject(templateObjectId);
            if (templateObj == null)
            {
                Log.LogE("AGE", " Event: CopyObjectTick Can't find template Object,  for action:" + _action.name);
                return;
            }
            GameObject parentObject = _action.GetGameObject(parentId);
            if (parentObject == null)
            {
                Log.LogE("AGE", " Event: CopyObjectTick Can't find parent Object,  for action:" + _action.name);
                return;
            }

            GameObject subParentObject = null;
            if (isGetByName)
            {
                Transform[] transforms = parentObject.GetComponentsInChildren<Transform>();
                for (int i = 0; i < transforms.Length; ++i)
                {
                    if (transforms[i].gameObject.name == subObjectName && (false == isGetByActice || transforms[i].gameObject.activeSelf))
                    {
                        subParentObject = transforms[i].gameObject;
                        break;
                    }
                }

                if (subParentObject == null)
                {
                    Log.LogD("AGE", " Warning: Failed to find sub object by name: <color=red>[ " + subObjectName + "--parent:" + parentObject.name + " ]</color>! "
                                      + " for Event: <color=red>[ " + this.GetType() + " ] </color> "
                                      + "by Action:<color=yellow>[ " + _action.actionName + " ] </color>");
                }
            }

            Vector3 newPos = new Vector3(0, 0, 0);
            Quaternion newRot = new Quaternion(0, 0, 0, 1);

            GameObject newObject = null;
            Transform tempObjTrans = templateObj.transform;

            if (targetId >= 0)
            {
                while (targetId >= _action.gameObjects.Count)
                    _action.gameObjects.Add(null);

                if (recreateExisting && _action.gameObjects[targetId] != null)
                {
                    ActionService.GetInstance().DestroyGameObject(_action.gameObjects[targetId]);
                    _action.gameObjects[targetId] = null;
                }

                if (_action.gameObjects[targetId] == null)
                {
                    if (!modifyRotation)
                        newRot = tempObjTrans.rotation;
                    newObject = ActionService.GetInstance().InstantiateObject(templateObj, newPos, newRot) as GameObject;

                    _action.gameObjects[targetId] = newObject;
                }
                else
                {
                    //use existing object
                    return;
                }
            }
            else
            {
                if (!modifyRotation)
                    newRot = tempObjTrans.rotation;

                newObject = ActionService.GetInstance().InstantiateObject(templateObj, newPos, newRot) as GameObject;
                if (newObject == null)
                {
                    Log.LogE("AGE", "Failed to copy object. template object \"" + templateObj.name + "\" can't Instantiate!");
                    return;
                }
            }

            if (newObject == null)
                return;

            Transform newObjTrans = newObject.transform;

            newObjTrans.parent = subParentObject ? subParentObject.transform : parentObject.transform;
           

            if (modifyTranslation)
            {
                newObjTrans.localPosition = translation;
            }

            if (modifyRotation)
            {
                newObjTrans.localRotation = rotation;
            }

            if (modifyScaling)
            {
                newObjTrans.localScale = scaling; //scaling is always local!!!
                ParticleSystem[] particsys = newObject.GetComponentsInChildren<ParticleSystem>();

                ParticleSystem newObjectParticleSystem = newObject.GetComponent<ParticleSystem>();
                if (newObjectParticleSystem  != null || particsys != null)
                {
                    for (int i = 0; i < particsys.Length; i++)
                    {
                        particsys[i].startSize *= scaling.x;
                        particsys[i].startSpeed *= scaling.x;
                        particsys[i].gravityModifier *= scaling.x;
                    }
                }
            }


            if (enableLayer)
            {
                newObject.SetLayerNoRecursively(layer);

                Transform[] transforms = newObject.GetComponentsInChildren<Transform>();
                for (int i = 0; i < transforms.Length; ++i)
                {
                    transforms[i].gameObject.SetLayerNoRecursively(layer);
                }
            }

            if (enableTag)
            {
                newObject.tag = tag;

                Transform[] transforms = newObject.GetComponentsInChildren<Transform>();
                for (int i = 0; i < transforms.Length; ++i)
                {
                    transforms[i].gameObject.tag = tag;
                }
            }

            LifeTimeHelper lifeTimeHelper = LifeTimeHelper.CreateTimeHelper(newObject);
            lifeTimeHelper.startTime = _action.CurrentTime;

            lifeTimeHelper.enableLayer = enableLayer;
            lifeTimeHelper.enableTag = enableTag;
            lifeTimeHelper.enableScaling = modifyScaling;
        }


        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as CopyObjectTick;
            targetId = srcCopy.targetId;
            parentId = srcCopy.parentId;
            isGetByName = srcCopy.isGetByName;
            subObjectName = srcCopy.subObjectName;
            isGetByActice = srcCopy.isGetByActice;
            templateObjectId = srcCopy.templateObjectId;
            recreateExisting = srcCopy.recreateExisting;
            modifyTranslation = srcCopy.modifyTranslation;
            translation = srcCopy.translation;
            modifyRotation = srcCopy.modifyRotation;
            rotation = srcCopy.rotation;
            modifyScaling = srcCopy.modifyScaling;
            scaling = srcCopy.scaling;
            enableLayer = srcCopy.enableLayer;
            layer = srcCopy.layer;
            enableTag = srcCopy.enableTag;
            tag = srcCopy.tag;
        }

        protected override void ClearData()
        {
            targetId = -1;
            parentId = -1;
            isGetByName = false;
            subObjectName = "Bip001";
            isGetByActice = false;
            templateObjectId = -1;
            recreateExisting = true;
            modifyTranslation = true;
            translation = Vector3.zero;
            modifyRotation = false;
            rotation = Quaternion.identity;
            modifyScaling = false;
            scaling = Vector3.one;
            enableLayer = false;
            layer = 0;
            enableTag = false;
            tag = "";
        }
        public override bool SupportEditMode()
        {
            return true;
        }
    }

}
