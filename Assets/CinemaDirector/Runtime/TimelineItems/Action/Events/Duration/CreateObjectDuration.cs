using CinemaDirector;
using UnityEngine;

namespace AGE
{
    [CutsceneItem("Utility", "CreateObjectDuration", CutsceneItemGenre.GenericItem)]
    public class CreateObjectDuration : DurationEvent
    {
        [Template]
        public int targetId = -1;

        [Template]
        public int parentId = -1;

        [Template]
        public int objectSpaceId = -1;

        //use in relative mode
        [Template]
        public int fromId = -1;

        [Template]
        public int toId = -1;

        public bool normalizedRelative = false;

        [Asset]
        public string prefabName = "";

        public bool recreateExisting = true;

        public bool modifyTranslation = true;
        public Vector3 translation = Vector3.zero;
        public bool modifyRotation = false;
        public Quaternion rotation = Quaternion.identity;
        public bool modifyScaling = false;
        public Vector3 scaling = Vector3.one;
        public Vector3 randomEffect = Vector3.zero;

        public bool enableLayer = false;
        public int layer = 0;
        public bool enableTag = false;
        public string tag = "";

        public bool destroyAtActionStop = true;

        public bool applyActionSpeedToAnimation = true;
        public bool applyActionSpeedToParticle = true;

        // 缩放
        public bool enableActorInfoScaling = false;
        public int actorInfoTargetId = -1;

        public override void Initialize()
        {
            base.Initialize();
            var _action = Cutscene;
            GameObject parentObj = _action.GetGameObject(parentId);
            Transform parentTrans = parentObj == null ? null : parentObj.transform;
            GameObject objectSpace = _action.GetGameObject(objectSpaceId);
            Transform objectSpaceTrans = objectSpace == null ? null : objectSpace.transform;
            GameObject fromObj = _action.GetGameObject(fromId);
            GameObject toObj = _action.GetGameObject(toId);
            Vector3 newPos = new Vector3();
            Quaternion newRot = new Quaternion();

            if (fromObj != null && toObj != null)
            {
                CalRelativeTransform(fromObj, toObj, ref newPos, ref newRot);
            }
            else if (parentTrans)
            {
                if (modifyTranslation)
                    newPos = parentTrans.localToWorldMatrix.MultiplyPoint(translation);
                if (modifyRotation)
                    newRot = parentTrans.rotation * rotation;
            }
            else if (objectSpaceTrans)
            {
                if (modifyTranslation)
                    newPos = objectSpaceTrans.localToWorldMatrix.MultiplyPoint(translation);
                if (modifyRotation)
                    newRot = objectSpaceTrans.rotation * rotation;
            }
            else
            {
                if (modifyTranslation)
                    newPos = translation;
                if (modifyRotation)
                    newRot = rotation;
            }

            if (randomEffect != Vector3.zero)
            {
                Vector3 posRandom = new Vector3(Random.Range(-randomEffect.x, randomEffect.x), Random.Range(-randomEffect.y, randomEffect.y), Random.Range(-randomEffect.z, randomEffect.z));
                newPos += posRandom;
            }

            bool enableNewScaling = modifyScaling || enableActorInfoScaling;
            Vector3 newScaling = Vector3.one;
            if (modifyScaling)
            {
                newScaling = scaling;
            }
            // 使用角色属性缩放
            if (enableActorInfoScaling)
            {
                float fscale = _action.GetActorInfoScale(actorInfoTargetId);
                newScaling = new Vector3(newScaling.x * fscale, newScaling.y * fscale, newScaling.z * fscale);
            }

            GameObject newObject = null;
            if (targetId >= 0)
            {
                while (targetId >= _action.gameObjects.Count)
                    _action.gameObjects.Add(null);

                if (recreateExisting && _action.gameObjects[targetId] != null)
                {
                    if (applyActionSpeedToAnimation)
                        _action.RemoveTempObject(Action.PlaySpeedAffectedType.ePSAT_Anim, _action.gameObjects[targetId]);
                    if (applyActionSpeedToParticle)
                        _action.RemoveTempObject(Action.PlaySpeedAffectedType.ePSAT_Fx, _action.gameObjects[targetId]);

                    ActionService.GetInstance().DestroyGameObject(_action.gameObjects[targetId]);
                    _action.gameObjects[targetId] = null;
                }

                if (_action.gameObjects[targetId] == null)
                {
                    if (string.IsNullOrEmpty(prefabName))
                    {
                        newObject = ActionService.GetInstance().GetNewGameObject();
                        if (null != newObject)
                        {
                            newObject.transform.position = newPos;
                            newObject.transform.rotation = newRot;
                        }


                        if (applyActionSpeedToParticle)
                        {
                            _action.AddTempObject(Action.PlaySpeedAffectedType.ePSAT_Fx, newObject);
                        }
                    }
                    else
                    {

                        newObject = ActionService.GetInstance().InstantiateParticleSystem(prefabName, _action, newPos, modifyRotation ? (Quaternion?)newRot : null, parentTrans,
                        destroyAtActionStop, applyActionSpeedToParticle, enableLayer ? layer : -1, enableTag ? tag : "", enableNewScaling ? (Vector3?)newScaling : null);
                    }
                    _action.gameObjects[targetId] = newObject;
                }
            }
            else
            {
                newObject = ActionService.GetInstance().InstantiateParticleSystem(prefabName, _action, newPos, modifyRotation ? (Quaternion?)newRot : null, parentTrans,
                    destroyAtActionStop, applyActionSpeedToParticle, enableLayer ? layer : -1, enableTag ? tag : "", enableNewScaling ? (Vector3?)newScaling : null);
            }

            if (newObject == null)
            {
                return;
            }

            if (applyActionSpeedToAnimation)
            {
                _action.AddTempObject(Action.PlaySpeedAffectedType.ePSAT_Anim, newObject);
            }
        }

        public override void Leave(Action _action, Track _track)
        {
            if (targetId >= 0 && Cutscene.GetGameObject(targetId))
            {
                if (applyActionSpeedToAnimation)
                    Cutscene.RemoveTempObject(Action.PlaySpeedAffectedType.ePSAT_Anim, Cutscene.gameObjects[targetId]);
                if (applyActionSpeedToParticle)
                    Cutscene.RemoveTempObject(Action.PlaySpeedAffectedType.ePSAT_Fx, Cutscene.gameObjects[targetId]);
                ActionService.GetInstance().DestroyGameObject(Cutscene.GetGameObject(targetId));
                Cutscene.gameObjects[targetId] = null; //特效已被回收，不再保持引用关系
            }
        }

        void CalRelativeTransform(GameObject fromObj, GameObject toObj, ref Vector3 newPos, ref Quaternion newRot)
        {
            if(fromObj == null || toObj == null)
            {
                return;
            }

            Transform fromTrans = fromObj.transform;
            Transform toTrans = toObj.transform;
            if (modifyTranslation)
            {
                //relative mode
                Vector3 result = new Vector3();
                Vector3 lookDir = toTrans.position - fromTrans.position;
                float len = (new Vector2(lookDir.x, lookDir.z)).magnitude;
                lookDir = Vector3.Normalize(new Vector3(lookDir.x * ModifyTransform.axisWeight.x, lookDir.y * ModifyTransform.axisWeight.y, lookDir.z * ModifyTransform.axisWeight.z));
                Quaternion lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                if (normalizedRelative)
                {
                    result = lookRotation * translation;
                    result = fromTrans.position + new Vector3(result.x * len, result.y, result.z * len);
                    result += new Vector3(0.0f, translation.z * (toTrans.position.y - fromTrans.position.y), 0.0f);
                }
                else
                {
                    result = fromTrans.position + lookRotation * translation;
                    result += new Vector3(0.0f, (translation.z / len) * (toTrans.position.y - fromTrans.position.y), 0.0f);
                }
                newPos = result;
            }
            if (modifyRotation)
            {
                //relative mode
                Vector3 lookDir = toTrans.position - fromTrans.position;
                //float length = lookDir.magnitude;
                lookDir = Vector3.Normalize(new Vector3(lookDir.x * ModifyTransform.axisWeight.x, lookDir.y * ModifyTransform.axisWeight.y, lookDir.z * ModifyTransform.axisWeight.z));
                Quaternion lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                Quaternion result = lookRotation * rotation;
                newRot = result;
            }
        }

        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            CreateObjectDuration r = src as CreateObjectDuration;
            targetId = r.targetId;
            parentId = r.parentId;
            objectSpaceId = r.objectSpaceId;
            fromId = r.fromId;
            toId = r.toId;
            normalizedRelative = r.normalizedRelative;
            prefabName = r.prefabName;
            recreateExisting = r.recreateExisting;
            modifyTranslation = r.modifyTranslation;
            translation = r.translation;
            modifyRotation = r.modifyRotation;
            rotation = r.rotation;
            modifyScaling = r.modifyScaling;
            scaling = r.scaling;
            enableLayer = r.enableLayer;
            layer = r.layer;
            enableTag = r.enableTag;
            tag = r.tag;
            applyActionSpeedToAnimation = r.applyActionSpeedToAnimation;
            applyActionSpeedToParticle = r.applyActionSpeedToParticle;
            randomEffect = r.randomEffect;
            destroyAtActionStop = r.destroyAtActionStop;

            enableActorInfoScaling = r.enableActorInfoScaling;
            actorInfoTargetId = r.actorInfoTargetId;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
            parentId = -1;
            objectSpaceId = -1;
            fromId = -1;
            toId = -1;
            normalizedRelative = false;
            prefabName = "";
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
            applyActionSpeedToAnimation = true;
            applyActionSpeedToParticle = true;
            randomEffect = Vector3.zero;
            destroyAtActionStop = true;
            enableActorInfoScaling = false;
            actorInfoTargetId = -1;
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }

        private void ResetTrans(Action action, GameObject obj)
        {
            GameObject parentObj = action.GetGameObject(parentId);
            Transform parentTrans = parentObj == null ? null : parentObj.transform;
            GameObject objectSpace = action.GetGameObject(objectSpaceId);
            Transform objectSpaceTrans = objectSpace == null ? null : objectSpace.transform;
            GameObject fromObj = action.GetGameObject(fromId);
            GameObject toObj = action.GetGameObject(toId);
            Vector3 newPos = new Vector3();
            Quaternion newRot = new Quaternion();

            if (fromObj != null && toObj != null)
            {
                CalRelativeTransform(fromObj, toObj, ref newPos, ref newRot);
            }
            else if (parentTrans)
            {
                if (modifyTranslation)
                    newPos = parentTrans.localToWorldMatrix.MultiplyPoint(translation);
                if (modifyRotation)
                    newRot = parentTrans.rotation * rotation;
            }
            else if (objectSpaceTrans)
            {
                if (modifyTranslation)
                    newPos = objectSpaceTrans.localToWorldMatrix.MultiplyPoint(translation);
                if (modifyRotation)
                    newRot = objectSpaceTrans.rotation * rotation;
            }
            else
            {
                if (modifyTranslation)
                    newPos = translation;
                if (modifyRotation)
                    newRot = rotation;
            }

            if (randomEffect != Vector3.zero)
            {
                Vector3 posRandom = new Vector3(Random.Range(-randomEffect.x, randomEffect.x), Random.Range(-randomEffect.y, randomEffect.y), Random.Range(-randomEffect.z, randomEffect.z));
                newPos += posRandom;
            }

            if (modifyTranslation)
                obj.transform.position = newPos;

            if (modifyRotation)
                obj.transform.rotation = newRot;
        }
    }

}
