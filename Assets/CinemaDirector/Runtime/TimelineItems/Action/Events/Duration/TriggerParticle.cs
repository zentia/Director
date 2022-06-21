using UnityEngine;
using CinemaDirector;

namespace AGE
{
    [CutsceneItem("Effect", "TriggerParticle", CutsceneItemGenre.GenericItem)]
    public class TriggerParticle : DurationEvent
    {
        [Template]
        public int targetId = 0;
        [Template]
        public int objectSpaceId = -1;
        [Asset]
        public string resourceName = "";
        [SubObject]
        public string bindPointName = "";

        public Vector3 bindPosOffset =  Vector3.zero;
        public Quaternion bindRotOffset = Quaternion.identity;
        public Vector3 scaling = new Vector3(1.0f, 1.0f, 1.0f); //size scale, lifetime scale, speed scale
        public Vector3 randomEffect = Vector3.zero;

        public bool enableLayer = false;
        public int layer = 0;
        public bool enableTag = false;
        public string tag = "";
        public bool enableScaling = true;

        public bool applyActionSpeedToParticle = true;
        public bool destroyAtActionStop = true;

        private GameObject particleObject = null;

        // 自动在挂接角色隐藏时，隐藏
        public bool autoHide = false;
        public const int SELF_PLAYER_INDEX = 0;
        private SkinnedMeshRenderer[] selfPlayerMesh = null;

        // 缩放
        public bool enableActorInfoScaling = false;
        public int actorInfoTargetId = -1;
        
        
        public override void Enter(Action _action, Track _track)
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
            }
            else if (virtualParent != null)
            {
                newPos = virtualParent.localToWorldMatrix.MultiplyPoint(bindPosOffset);
                newRot = virtualParent.rotation * bindRotOffset;
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
            if (particleObject == null)
            {
                particleObject = ActionService.GetInstance().InstantiateParticleSystem(resourceName, _action, newPos, newRot, parent, destroyAtActionStop, applyActionSpeedToParticle, enableLayer ? layer : -1, enableTag ? tag : "", enableNewScaling ? (Vector3?)newScaling : null);
            }

            if (autoHide)
            {
                GameObject fromObject = _action.GetGameObject(SELF_PLAYER_INDEX);
                selfPlayerMesh = fromObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
        }

        public override void Leave(Action _action, Track _track)
        {
            if (particleObject != null)
            {
                if (autoHide)
                {
                    bool hide = CheckAutoHide();
                    if (hide)
                    {
                        particleObject.SetActive(true);
                    }
                }


                particleObject.transform.parent = null;
                ActionService.GetInstance().DestroyGameObject(particleObject);

                if (applyActionSpeedToParticle)
                {
                    _action.RemoveTempObject(Action.PlaySpeedAffectedType.ePSAT_Fx, particleObject);
                }
            }
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            if (autoHide && particleObject)
            {
                bool hide = CheckAutoHide();
                if (hide)
                {
                    particleObject.SetActive(false);
                }
                else
                {
                    particleObject.SetActive(true);
                }
            }
            if (particleObject == null)
            {
                return;
            }
            var particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>();
            foreach (var particleSystem in particleSystems)
            {
                particleSystem.Simulate(_localTime, true, true);
                particleSystem.Pause(true);
                particleSystem.time = _localTime;
            }

            var animators = particleObject.GetComponentsInChildren<Animator>();
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

#if UNITY_EDITOR
        public override void ReverseTrigger(Action _action, Track _track)
        {
            Leave(_action, _track);
        }

        public override void ReverseEnd(Action _action, Track _track)
        {
            Enter(_action, _track);
        }
#endif

        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var copySrc = src as TriggerParticle;
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
            destroyAtActionStop = copySrc.destroyAtActionStop;
            autoHide = copySrc.autoHide;
            particleObject = null;

            enableActorInfoScaling = copySrc.enableActorInfoScaling;
            actorInfoTargetId = copySrc.actorInfoTargetId;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
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
            destroyAtActionStop = true;
            autoHide = false;
            particleObject = null;

            enableActorInfoScaling = false;
            actorInfoTargetId = -1;
        }

        private bool CheckAutoHide()
        {
            if (!autoHide)
            {
                return false;
            }

            if (selfPlayerMesh != null)
            {
                for (int i = 0; i < selfPlayerMesh.Length; i++)
                {
                    if (selfPlayerMesh[i] != null && !selfPlayerMesh[i].enabled)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ResetTrans(Action action, GameObject obj)
        {
            Vector3 newPos = bindPosOffset;
            Quaternion newRot = bindRotOffset;
            GameObject targetObject = action.GetGameObject(targetId);
            GameObject objectSpace = action.GetGameObject(objectSpaceId);
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
            }
            else if (virtualParent != null)
            {
                newPos = virtualParent.localToWorldMatrix.MultiplyPoint(bindPosOffset);
                newRot = virtualParent.rotation * bindRotOffset;
            }

            if (randomEffect != Vector3.zero)
            {
                Vector3 posRandom = new Vector3(Random.Range(-randomEffect.x, randomEffect.x), Random.Range(-randomEffect.y, randomEffect.y), Random.Range(-randomEffect.z, randomEffect.z));
                newPos += posRandom;
            }

            obj.transform.position = newPos;
            obj.transform.rotation = newRot;
        }
    }
}