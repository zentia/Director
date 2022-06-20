using CinemaDirector;
using UnityEngine;

namespace AGE
{
    [EventCategory("Animation")]
    public class TriggerParticleTick : TickEvent
    {
        public override bool SupportEditMode()
        {
            return true;
        }

        [Template]
        public int targetId = 0;

        [Template]
        public int objectSpaceId = -1;

        [Asset]
        public string resourceName = "";

        [SubObject]
        public string bindPointName = "";

        public Vector3 bindPosOffset = Vector3.zero;
        public Quaternion bindRotOffset = Quaternion.identity;
        public Vector3 scaling = new Vector3(1.0f, 1.0f, 1.0f); //size scale, lifetime scale, speed scale
        public Vector3 randomEffect = Vector3.zero;
		public Vector3 randomScale = Vector3.zero;

        public bool enableLayer = false;
        public int layer = 0;
        public bool enableTag = false;
        public string tag = "";
        public bool enableScaling = true;

        public bool applyActionSpeedToParticle = true;
        public bool destroyAtActionStop = true;
        public bool hasRandomRotationRange = false;
        public Vector3 randomRotationRange = Vector3.zero;

        //生命时间管理卸载
        public bool lifeTimeDestroy = false;
        public float lifeTime = 0f;

        // 缩放
        public bool enableActorInfoScaling = false;
        public int actorInfoTargetId = -1;

        private GameObject _particleObject = null;

        public override void Process(Action _action, Track _track)
        {
            Vector3 newPos = bindPosOffset;
            Quaternion newRot = bindRotOffset;
            GameObject targetObject = _action.GetGameObject(targetId);
            GameObject objectSpace = _action.GetGameObject(objectSpaceId);
            Transform parent = null;
            Transform virtualParent = null;

            if (bindPointName.Length == 0)
            {
                if (targetObject != null)
                    parent = targetObject.transform;
                else if (objectSpace != null)
                    virtualParent = objectSpace.transform;
            }
            else
            {
                Transform bindPoint = null;
                if (targetObject != null)
                {
                    GameObject bindObject = SubObject.FindSubObject(targetObject, bindPointName);
                    if (bindObject != null)
                    {
                        bindPoint = bindObject.transform;
                        if (bindPoint != null)
                            parent = bindPoint;
                    }
                    else
                    {
                        parent = targetObject.transform;
                    }
                }
                else if (objectSpace != null)
                {
                    GameObject bindObject = SubObject.FindSubObject(objectSpace, bindPointName);
                    if (bindObject != null)
                    {
                        bindPoint = bindObject.transform;
                        if (bindPoint != null)
                            virtualParent = bindPoint;
                    }
                    else
                    {
                        virtualParent = objectSpace.transform;
                    }
                }
            }
            if (parent != null)
            {
                newPos = parent.localToWorldMatrix.MultiplyPoint(bindPosOffset);
                newRot = parent.rotation * bindRotOffset;
                if(hasRandomRotationRange)
                    newRot *= GetRandomRotation(randomRotationRange);
            }
            else if (virtualParent != null)
            {
                newPos = virtualParent.localToWorldMatrix.MultiplyPoint(bindPosOffset);
                newRot = virtualParent.rotation * bindRotOffset;
                if (hasRandomRotationRange)
                    newRot *= GetRandomRotation(randomRotationRange);
            }

            if (randomEffect != Vector3.zero)
            {
                Vector3 posRandom = new Vector3(Random.Range(-randomEffect.x, randomEffect.x), Random.Range(-randomEffect.y, randomEffect.y), Random.Range(-randomEffect.z, randomEffect.z));
                newPos += posRandom;
            }

            bool enableNewScaling = enableScaling || enableActorInfoScaling;
            Vector3 newScaling = Vector3.one;
            if (enableScaling)
            {
                newScaling = scaling;
            }
            // 使用角色属性缩放
            if (enableActorInfoScaling)
            {
                float fscale = _action.GetActorInfoScale(actorInfoTargetId);
                newScaling = new Vector3(newScaling.x * fscale, newScaling.y * fscale, newScaling.z * fscale);
            }

			Vector3 randomS = Vector3.zero;
			randomS.x = Random.Range(-randomScale.x, randomScale.x);
			randomS.y = Random.Range(-randomScale.y, randomScale.y);
			randomS.z = Random.Range(-randomScale.z, randomScale.z);
			newScaling += randomS;

            // 替换异步加载
            if (_particleObject == null)
            {
                _particleObject = ActionService.GetInstance().InstantiateParticleSystem(resourceName, _action, newPos, newRot, parent, destroyAtActionStop, applyActionSpeedToParticle,
                    enableLayer ? layer : -1, enableTag ? tag : "", enableNewScaling ? (Vector3?)newScaling : null);  
                if(!destroyAtActionStop && lifeTimeDestroy)
                {
                    LifeTimeHelper lifeTimeHelper = _particleObject.GetComponent<LifeTimeHelper>();
                    if(lifeTimeHelper != null)
                    {
                        lifeTimeHelper.SetDestroyLifeTime(lifeTime);
                    }
                }
            }
            else
            {
                var particleSystems = _particleObject.GetComponentsInChildren<ParticleSystem>();
                foreach (var particleSystem in particleSystems)
                {
                    particleSystem.time = 0;
                }
            }
        }
        
        public override void ProcessBlend(Action _action, Track _track, TickEvent _prevEvent, float _blendWeight)
        {
            
        }

        public override void PostProcess(Action _action, Track _track, float _localTime)
        {
            if (_particleObject == null)
            {
                return;
            }
            var particleSystems = _particleObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var particleSystem in particleSystems)
            {
                particleSystem.Simulate(_localTime, true, true);
                particleSystem.Pause(true);
                particleSystem.time = _localTime;
            }

            var animators = _particleObject.GetComponentsInChildren<Animator>();
            foreach (var animator in animators)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips.Length == 0)
                {
                    continue;
                }

                var clip = clips[0];
                clip.SampleAnimation(animator.gameObject, _localTime);
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
            var copySrc = src as TriggerParticleTick;

            targetId = copySrc.targetId;
            objectSpaceId = copySrc.objectSpaceId;
            resourceName = copySrc.resourceName;
            bindPointName = copySrc.bindPointName;
            bindPosOffset = copySrc.bindPosOffset;
            bindRotOffset = copySrc.bindRotOffset;
            scaling = copySrc.scaling;
            enableLayer = copySrc.enableLayer;
            layer = copySrc.layer;
            enableTag = copySrc.enableTag;
            tag = copySrc.tag;
            enableScaling = copySrc.enableScaling;
            applyActionSpeedToParticle = copySrc.applyActionSpeedToParticle;
            randomEffect = copySrc.randomEffect;
			randomScale = copySrc.randomScale;
            destroyAtActionStop = copySrc.destroyAtActionStop;
            randomRotationRange = copySrc.randomRotationRange;
            hasRandomRotationRange = copySrc.hasRandomRotationRange;
            lifeTimeDestroy = copySrc.lifeTimeDestroy;
            lifeTime = copySrc.lifeTime;

            enableActorInfoScaling = copySrc.enableActorInfoScaling;
            actorInfoTargetId = copySrc.actorInfoTargetId;
        }

        protected override void ClearData()
        {

            targetId = 0;
            objectSpaceId = -1;
            resourceName = "";
            bindPointName = "";
            bindPosOffset = Vector3.zero;
            bindRotOffset = Quaternion.identity;
            scaling = new Vector3(1.0f, 1.0f, 1.0f);
            enableLayer = false;
            layer = 0;
            enableTag = false;
            tag = "";
            enableScaling = true;
            applyActionSpeedToParticle = true;
            randomEffect = Vector3.zero;
			randomScale = Vector3.zero;
            destroyAtActionStop = true;
            hasRandomRotationRange = false;
            randomRotationRange = Vector3.zero;

            lifeTimeDestroy = false;
            lifeTime = 0f;

            enableActorInfoScaling = false;
            actorInfoTargetId = -1;
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }

        // 行动结束，异步加载的结束标记还没加载处理的销毁
        public override void OnActionStop(Action _action)
        {
        }
    }
}
