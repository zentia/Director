using CinemaDirector;
using UnityEngine;

namespace AGE
{

    [EventCategory("Utility")]
    public class CreateObjectTick : TickEvent
    {
        public override bool SupportEditMode()
        {
            return true;
        }

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

        [AssetReference]
        public string prefabName = "";

        public bool recreateExisting = true;

        public bool modifyTranslation = true;
        public Vector3 translation = Vector3.zero;
        public bool modifyRotation = false;
        public Quaternion rotation = Quaternion.identity;
        public bool modifyScaling = false;
        public Vector3 scaling = Vector3.one;
        public Vector3 randomEffect = Vector3.zero;
		public bool hasRandomRotationRange = false;
		public Vector3 randomRotationRange = Vector3.zero;

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

        public override void Process(Action _action, Track _track)
        {
            GameObject parentObj = _action.GetGameObject(parentId);
            Transform parentTrans = parentObj == null ? null : parentObj.transform;
            GameObject objectSpace = _action.GetGameObject(objectSpaceId);
            Transform objectSpaceTrans = objectSpace == null ? null : objectSpace.transform;
            GameObject fromObj = _action.GetGameObject(fromId);
            GameObject toObj = _action.GetGameObject(toId);
            Vector3 newPos = new Vector3(0, 0, 0);
            Quaternion newRot = new Quaternion(0, 0, 0, 1);

            if (fromObj != null && toObj != null)
            {
                CalRelativeTransform(fromObj, toObj, ref newPos, ref newRot);
            }
            else if (parentTrans)
            {
                if (modifyTranslation)
                    newPos = parentTrans.localToWorldMatrix.MultiplyPoint(translation);
                if (modifyRotation)
				{
					newRot = parentTrans.rotation * rotation;
					if (hasRandomRotationRange)
					{
						newRot *= GetRandomRotation(randomRotationRange);
					}
				}
            }
            else if (objectSpaceTrans)
            {
                if (modifyTranslation)
                    newPos = objectSpaceTrans.localToWorldMatrix.MultiplyPoint(translation);
                if (modifyRotation)
				{
					newRot = objectSpaceTrans.rotation * rotation;
					if (hasRandomRotationRange)
					{
						newRot *= GetRandomRotation(randomRotationRange);
					}
				}
     
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
                        if(newObject != null)
                        {
                            Transform trans = newObject.transform;
                            trans.position = newPos;
                            trans.rotation = newRot;
                        }

                        if (applyActionSpeedToParticle)
                        {
                            _action.AddTempObject(Action.PlaySpeedAffectedType.ePSAT_Fx, newObject);
                        }
                    }
                    else
                    {
                            newObject = ActionService.GetInstance().InstantiateParticleSystem(prefabName, _action, newPos, modifyRotation ? (Quaternion?)newRot : null, parentObj ? parentObj.transform : null,
                              destroyAtActionStop, applyActionSpeedToParticle, enableLayer ? layer : -1, enableTag ? tag : "", enableNewScaling ? (Vector3?)newScaling : null) as GameObject;
                    }
                    _action.gameObjects[targetId] = newObject;
                }
                else
                {
                    return;
                }
            }
            else
            {
                newObject = ActionService.GetInstance().InstantiateParticleSystem(prefabName, _action, newPos, modifyRotation ? (Quaternion?)newRot : null, parentObj ? parentObj.transform : null,
                    destroyAtActionStop, applyActionSpeedToParticle, enableLayer ? layer : -1, enableTag ? tag : "", enableNewScaling ? (Vector3?)newScaling : null) as GameObject;
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

        void CalRelativeTransform(GameObject fromObj, GameObject toObj, ref Vector3 pos, ref Quaternion rot)
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
                float length = (new Vector2(lookDir.x, lookDir.z)).magnitude;
                lookDir = Vector3.Normalize(new Vector3(lookDir.x * ModifyTransform.axisWeight.x, lookDir.y * ModifyTransform.axisWeight.y, lookDir.z * ModifyTransform.axisWeight.z));
                Quaternion lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                if (normalizedRelative)
                {
                    result = lookRotation * translation;
                    result = fromTrans.position + new Vector3(result.x * length, result.y, result.z * length);
                    result += new Vector3(0.0f, translation.z * (toTrans.position.y - fromTrans.position.y), 0.0f);
                }
                else
                {
                    result = fromTrans.position + lookRotation * translation;
                    result += new Vector3(0.0f, (translation.z / length) * (toTrans.position.y - fromTrans.position.y), 0.0f);
                }
                pos = result;
            }
            if (modifyRotation)
            {
                //relative mode
                Vector3 lookDir = toTrans.position - fromTrans.position;
                //float length = lookDir.magnitude;
                lookDir = Vector3.Normalize(new Vector3(lookDir.x * ModifyTransform.axisWeight.x, lookDir.y * ModifyTransform.axisWeight.y, lookDir.z * ModifyTransform.axisWeight.z));
                Quaternion lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                Quaternion result = lookRotation * rotation;
                rot = result;
            }
        }

		Quaternion GetRandomRotation(Vector3 rotationRange)
		{
			rotationRange.x = Random.Range(-rotationRange.x, rotationRange.x);
			rotationRange.y = Random.Range(-rotationRange.y, rotationRange.y);
			rotationRange.z = Random.Range(-rotationRange.z, rotationRange.z);
			//Debug.Log("Result:" + rotationRange);
			return Quaternion.Euler(rotationRange);
		}

        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as CreateObjectTick;
            targetId = copySrc.targetId;
            parentId = copySrc.parentId;
            objectSpaceId = copySrc.objectSpaceId;
            fromId = copySrc.fromId;
            toId = copySrc.toId;
            normalizedRelative = copySrc.normalizedRelative;
            prefabName = copySrc.prefabName;
            recreateExisting = copySrc.recreateExisting;
            modifyTranslation = copySrc.modifyTranslation;
            translation = copySrc.translation;
            modifyRotation = copySrc.modifyRotation;
            rotation = copySrc.rotation;
            modifyScaling = copySrc.modifyScaling;
            scaling = copySrc.scaling;
            enableLayer = copySrc.enableLayer;
            layer = copySrc.layer;
            enableTag = copySrc.enableTag;
            tag = copySrc.tag;
            applyActionSpeedToAnimation = copySrc.applyActionSpeedToAnimation;
            applyActionSpeedToParticle = copySrc.applyActionSpeedToParticle;
            destroyAtActionStop = copySrc.destroyAtActionStop;
            randomEffect = copySrc.randomEffect;
			randomRotationRange = copySrc.randomRotationRange;
			hasRandomRotationRange =  copySrc.hasRandomRotationRange;

            enableActorInfoScaling = copySrc.enableActorInfoScaling;
            actorInfoTargetId = copySrc.actorInfoTargetId;
        }

        protected override void ClearData()
        {
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
            destroyAtActionStop = true;
            randomEffect = Vector3.zero;
			randomRotationRange = Vector3.zero;
			hasRandomRotationRange = false;

            enableActorInfoScaling = false;
            actorInfoTargetId = -1;
        }

        // 行动结束，异步加载的结束标记还没加载处理的销毁
        public override void OnActionStop(Action _action)
        {
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
