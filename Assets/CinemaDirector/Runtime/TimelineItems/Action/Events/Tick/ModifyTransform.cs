using System;
using UnityEngine;
using Assets.Plugins.Common;
using System.Collections.Generic;
using CinemaDirector;
using Random = UnityEngine.Random;

namespace AGE
{
    [CutsceneItem("Transform", "ModifyTransform", CutsceneItemGenre.GenericItem, CutsceneItemGenre.TransformItem)]
    public class ModifyTransform : TickEvent
    {
        private const float CtrlPtScale = 0.6f;
        public static Vector3 axisWeight = new Vector3(1, 0, 1);
        
        private bool _currentInitialized = false;
        public enum Mode
        {
            Linear,
            Cubic,
            Bezier,
        }
        [Template]
        public int targetId = -1;
        [Template]
        public int objectSpaceId = -1;
        [Template]
        public int fromId = -1;
        [Template]
        public int toId = -1;
        public bool enableTranslation = true;
        public bool currentTranslation = false;
        public Vector3 translation = Vector3.zero;
        public Vector3 translationInTangent;
        public Vector3 translationOutTangent;
        public bool enableRotation = true;
        public bool currentRotation = false;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 rotationInTangent;
        public Vector3 rotationOutTangent;
        public bool enableScaling = false;
        public bool currentScaling = false;
        public Vector3 scaling = Vector3.one; //scaling is always local!!!
        public Vector3 scaleInTangent;
        public Vector3 scaleOutTangent;
        public bool normalizedRelative = false;
        public Mode mode;
        public bool enableRandomTranslation = false;
        public Vector3 randomTranslation = Vector3.zero;
        public string childPath;
        
        [Template]
        public int lookAtId = -1;

        public Vector3 lookAtOffset = Vector3.one;
        private Vector3 _translation;
        
        // 针对延迟加载特效的，延迟等待处理位置
        public override bool IsNeedWait(Action _action)
        {
            // 检查是否要等待异步加载
            return false;
        }
        bool CheckConflict(Action _action)
        {
            List<Track> arrayList = null;
            _action.GetTracks(typeof(CameraShakeDurationNew), ref arrayList);
            foreach(var t in arrayList)
            {
                if (!t.enabled)
                {
                    continue;
                }
                foreach (CameraShakeDurationNew e in t.trackEvents)
                {
                    if (e.targetId != targetId)
                    {
                        break;
                    }
                    if (e.time < _action.CurrentTime && e.time + e.length > _action.CurrentTime)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override void Process(Action _action, Track _track)
        {
            var targetObject = _action.GetGameObject(targetId);
            if (targetObject == null) 
                return;
            
            var targetTrans = targetObject.transform;
            if (!string.IsNullOrEmpty(childPath))
            {
                var child = targetTrans.Find(childPath);
                if (child)
                {
                    targetTrans = child;
                    targetObject = child.gameObject;
                }
            }
            _currentInitialized = false;
            SetCurrentTransform(targetTrans);
            var conflict = CheckConflict(_action);
            if (enableTranslation && !conflict)
            {
                targetTrans.position = GetTranslation(_action);
            }
            if (enableRotation && !conflict)
            {
                targetTrans.rotation = GetRotation(_action);
            }
            else if (lookAtId != -1)
            {
                targetTrans.LookAt(GetLookAtPosition(_action));
            }
            if (enableScaling)
            {
                targetTrans.localScale = scaling;

                LifeTimeHelper lifetimeComp = targetObject.GetComponent<LifeTimeHelper>();
                if (lifetimeComp != null)
                {
                    lifetimeComp.enableScaling = enableScaling;
                }
            }
        }

        public Vector3 LinearVector3(Action _action, Track _track, ModifyTransform _prevEvent, float _blendWeight, Func<ModifyTransform, Action, Vector3> func)
        {
            if (null == _action || null == _track || null == _prevEvent)
            {
                return Vector3.zero;
            }
            Vector3 prevPoint = func(_prevEvent, _action);
            Vector3 curnPoint = func(this, _action);
            float t1 = 1.0f - _blendWeight;
            float t2 = _blendWeight;
            return t2 * curnPoint + t1 * prevPoint;
        }

        public Quaternion Slerp(Action _action, Track _track, ModifyTransform modifyTransformm, float blendWeight, Func<ModifyTransform, Action, Quaternion> func)
        {
            if (null == _action || null == _track || null == modifyTransformm)
            {
                return Quaternion.identity;
            }
            var prevPoint = func(modifyTransformm, _action);
            var curPoint = func(this, _action);
            return Quaternion.Slerp(prevPoint, curPoint, blendWeight);
        }

        public Vector3 CubicInterpolation(Action _action, Track _track, ModifyTransform prevEvent, float _blendWeight, Func<ModifyTransform, Action,Vector3> func)
        {
            if (null == _action || null == _track)
            {
                return Vector3.zero;
            }

            int prevID = _track.GetIndexOfEvent(prevEvent);
            int curID = _track.GetIndexOfEvent(this);
            int evtCount = _track.GetEventsCount();
            int formerID = prevID - 1;
            if (formerID < 0)
            {
                if (Cutscene.IsLooping)
                {
                    formerID = evtCount - 1;
                    if (formerID < 0)
                        formerID = 0;
                }
                else
                    formerID = 0;
            }
            int latterId = curID + 1;
            if (latterId >= evtCount)
            {
                if (Cutscene.IsLooping)
                    latterId = 0;
                else
                    latterId = curID;
            }
            var formerEvent = _track.GetEvent(formerID) as ModifyTransform;
            if (null == formerEvent)
            {
                return Vector3.zero;
            }

            var latterEvent = _track.GetEvent(latterId) as ModifyTransform;
            if (null == latterEvent)
            {
                return Vector3.zero;
            }

            Vector3 prevPoint = func(prevEvent, _action);
            Vector3 curnPoint = func(this, _action);
            Vector3 formPoint = func(formerEvent, _action);
            Vector3 lattPoint = func(latterEvent, _action);

            Vector3 ctrlPoint1;
            Vector3 ctrlPoint2;
            CalculateCtrlPoint(formPoint, prevPoint, curnPoint, lattPoint, out ctrlPoint1, out ctrlPoint2);

            float t1 = 1.0f - _blendWeight;
            float t2 = _blendWeight;
            return prevPoint * t1 * t1 * t1 + ctrlPoint1 * 3 * t1 * t1 * t2 + ctrlPoint2 * 3 * t1 * t2 * t2 + curnPoint * t2 * t2 * t2;
        }

        public Quaternion Cubic(Action _action, Track _track, ModifyTransform prevEvent, float t, Func<ModifyTransform, Action, Quaternion> func)
        {
            if (null == _action || null == _track)
            {
                return Quaternion.identity;
            }

            int prevID = _track.GetIndexOfEvent(prevEvent);
            int curID = _track.GetIndexOfEvent(this);
            int evtCount = _track.GetEventsCount();
            int formerID = prevID - 1;
            if (formerID < 0)
            {
                if (Cutscene.IsLooping)
                {
                    formerID = evtCount - 1;
                    if (formerID < 0)
                        formerID = 0;
                }
                else
                    formerID = 0;
            }
            int latterId = curID + 1;
            if (latterId >= evtCount)
            {
                if (Cutscene.IsLooping)
                    latterId = 0;
                else
                    latterId = curID;
            }
            var formerEvent = _track.GetEvent(formerID) as ModifyTransform;
            if (null == formerEvent)
            {
                return Quaternion.identity;
            }

            var latterEvent = _track.GetEvent(latterId) as ModifyTransform;
            if (null == latterEvent)
            {
                return Quaternion.identity;
            }
            // squad姿态插值
            var qi1 = func(prevEvent, _action); // 上一个点
            var si = func(this, _action); // 当前点
            var qi = func(formerEvent, _action); // 上上一个
            var si1 = func(latterEvent, _action); // 下一个
            var k1 = Quaternion.Slerp(qi, qi1, t);
            var k2 = Quaternion.Slerp(si, si1, t);
            return Quaternion.Slerp(k1, k2, 2 * t * (1 - t));
        }

        public Vector3 BeizerInterpolation(Action _action, Track _track, ModifyTransform _prevEvent, float _blendWeight, Func<ModifyTransform, Action, Vector3> func, Func<ModifyTransform, Vector3> inTangentFunc, Func<ModifyTransform, Vector3> outTangentFunc)
        {
            if (null == _action || null == _track || null == _prevEvent)
            {
                return Vector3.zero;
            }

            Vector3 prevPoint = func(_prevEvent, _action);
            Vector3 curnPoint = func(this, _action);
            Vector3 formPoint = inTangentFunc(this);
            Vector3 lattPoint = outTangentFunc(_prevEvent);

            Vector3 ctrlPoint1;
            Vector3 ctrlPoint2;
            CalculateCtrlPoint(formPoint, prevPoint, curnPoint, lattPoint, out ctrlPoint1, out ctrlPoint2);

            float t1 = 1.0f - _blendWeight;
            float t2 = _blendWeight;
            return prevPoint * t1 * t1 * t1 + ctrlPoint1 * 3 * t1 * t1 * t2 + ctrlPoint2 * 3 * t1 * t2 * t2 + curnPoint * t2 * t2 * t2;
        }

        // 这里也是squad姿态插值
        public Quaternion Beizer(Action _action, Track _track, ModifyTransform _prevEvent, float t, Func<ModifyTransform, Action, Quaternion> func, Func<ModifyTransform, Quaternion> inTangentFunc, Func<ModifyTransform, Quaternion> outTangentFunc)
        {
            if (null == _action || null == _track || null == _prevEvent)
            {
                return Quaternion.identity;
            }

            var qi1 = func(_prevEvent, _action); // 上一个点
            var si = func(this, _action);
            var qi = inTangentFunc(this); // 上上
            var si1 = outTangentFunc(_prevEvent);
            var k1 = Quaternion.Slerp(qi, qi1, t);
            var k2 = Quaternion.Slerp(si, si1, t);
            return Quaternion.Slerp(k1, k2, 2 * t * (1 - t));
        }
        private void CalculateCtrlPoint(Vector3 formPoint, Vector3 prevPoint, Vector3 curnPoint, Vector3 lattPoint, out Vector3 ctrlPoint1, out Vector3 ctrlPoint2)
        {
            Vector3 midForwPrev = (formPoint + prevPoint) * 0.5f;
            Vector3 midPrevCurn = (curnPoint + prevPoint) * 0.5f;
            Vector3 midCurnLatt = (curnPoint + lattPoint) * 0.5f;

            Vector3 midForwPrevCurn = (midForwPrev + midPrevCurn) * 0.5f;
            Vector3 midPrevCurnLatt = (midCurnLatt + midPrevCurn) * 0.5f;

            Vector3 handle1 = midPrevCurn - midForwPrevCurn;
            Vector3 handle2 = midPrevCurn - midPrevCurnLatt;

            float s1 = CtrlPtScale;
            float s2 = CtrlPtScale;
            float lh1 = handle1.magnitude;
            float lh2 = handle2.magnitude;
            float halfl = (curnPoint - prevPoint).magnitude * 0.5f;

            if (halfl < lh1)
                s1 = halfl / lh1;
            if (halfl < lh2)
                s2 = halfl / lh2;
            ctrlPoint1 = prevPoint + (handle1) * s1;
            ctrlPoint2 = curnPoint + (handle2) * s2;
        }

        public override void ProcessBlend(Action _action, Track _track, TickEvent prevEvent, float _blendWeight)
        {
            var targetObject = _action.GetGameObject(targetId);
            var _prevEvent = prevEvent as ModifyTransform;
			if (targetObject == null || _prevEvent == null)
                return;

            if (HasDependObject(_action) == -1)
            {
                return;
            }
            var conflict = CheckConflict(_action);
            if (enableTranslation && !conflict)
            {
                switch (mode)
                {
                    case Mode.Linear:
                        targetObject.transform.position = LinearVector3(_action, _track, _prevEvent, _blendWeight, ResolvePosition);
                        break;
                    case Mode.Cubic:
                        targetObject.transform.position = CubicInterpolation(_action, _track, _prevEvent, _blendWeight, ResolvePosition);
                        break;
                    case Mode.Bezier:
                        targetObject.transform.position = BeizerInterpolation(_action, _track, _prevEvent, _blendWeight, ResolvePosition, ResolvePositionInTangent, ResolvePositionOutTangent);
                        break;
                }
            }
            if (enableRotation && !conflict)
            {
                switch (mode)
                {
                    case Mode.Linear:
                        targetObject.transform.rotation = Slerp(_action, _track, _prevEvent, _blendWeight, ResolveRotaion);
                        break;
                    case Mode.Cubic:
                        targetObject.transform.rotation = Cubic(_action, _track, _prevEvent, _blendWeight, ResolveRotaion);
                        break;
                    case Mode.Bezier:
                        targetObject.transform.rotation = Beizer(_action, _track, _prevEvent, _blendWeight, ResolveRotaion, ResolveRotationInTangent, ResolveRotationOutTangent);
                        break;
                }
            }
            else if(lookAtId != -1)
            {
                _action.GetGameObject(targetId).transform.LookAt(GetLookAtPosition(_action));
            }
            if (enableScaling)
            {
                switch (mode)
                {
                    case Mode.Linear:
                        targetObject.transform.localScale = LinearVector3(_action, _track, _prevEvent, _blendWeight, ResolveScale);
                        break;
                    case Mode.Cubic:
                        targetObject.transform.localScale = CubicInterpolation(_action, _track, _prevEvent, _blendWeight, ResolveScale);
                        break;
                    case Mode.Bezier:
                        targetObject.transform.localScale = BeizerInterpolation(_action, _track, _prevEvent, _blendWeight, ResolveScale, ResolveScaleInTangent, ResolveScaleOutTangent);
                        break;
                }
            }
        }

        // 0 : no depedent object
        // 1 : find depedent object
        // -1 : has depedent obejct, but not find in action
        public int HasDependObject(Action _action)
        {
            if (currentTranslation || currentRotation || currentScaling)
                return 1;
            if (fromId >= 0)
            {
                if (_action.GetGameObject(fromId) != null)
                    return 1;

                return -1;
            }
            if (toId >= 0)
            {
                if (_action.GetGameObject(toId) != null)
                    return 1;

                return -1;
            }
            if (objectSpaceId >= 0)
            {
                if (_action.GetGameObject(objectSpaceId) != null)
                    return 1;

                return -1;
            }
            return 0;
        }

        public Vector3 GetTranslation(Action _action)
        {
            if (_action.GetGameObject(targetId))
                SetCurrentTransform(_action.GetGameObject(targetId).transform);

            var fromObject = _action.GetGameObject(fromId);
            var toObject = _action.GetGameObject(toId);
          
            if (fromObject && toObject)
            {
                Transform fromTrans = fromObject.transform;
                Transform toTrans = toObject.transform;

                //relative mode
                Vector3 result = new Vector3();
                Vector3 lookDir = toTrans.position - fromTrans.position;
                float length = new Vector2(lookDir.x, lookDir.z).magnitude;
                lookDir = Vector3.Normalize(new Vector3(lookDir.x * axisWeight.x, lookDir.y * axisWeight.y, lookDir.z * axisWeight.z));
                Quaternion lookRotation = Quaternion.identity;
                if (lookDir != Vector3.zero)
                {
                    lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
                else
                {
#if UNITY_EDITOR
                        Log.LogE("AGE", "<color=red>[ ModifyTransform GetTranslation Attention]</color> LookDir is set to Zero(lookRotation will be set to Quaternion.identity), To Gamebject:"
                            + toObject.name + ", FromObject" + fromObject.name + ",action:" + _action.actionName);
#endif
                }

                if (normalizedRelative)
                {
                    result = lookRotation * _translation;
                    result = fromTrans.position + new Vector3(result.x * length, result.y, result.z * length);
                    result += new Vector3(0.0f, _translation.z * (toTrans.position.y - fromTrans.position.y), 0.0f);
                }
                else
                {
                    result = fromTrans.position + lookRotation * _translation;
                    if (length != 0)
                    {
                        result += new Vector3(0.0f, (_translation.z / length) * (toTrans.position.y - fromTrans.position.y), 0.0f);
                    }
                }
                return result;
            }
            else
            {
                var objectSpace = _action.GetGameObject(objectSpaceId);
                if (objectSpace)
                {
                    //in given space
                    return objectSpace.transform.localToWorldMatrix.MultiplyPoint(_translation);
                }
                else
                {
                    var targetObject = _action.GetGameObject(targetId);
					if (targetObject && targetObject.transform.parent)
                    {
                        //in parent space
                        return targetObject.transform.parent.localToWorldMatrix.MultiplyPoint(_translation);
                    }
                    else
                    {
                        //in world space
                        return _translation;
                    }
                }
            }
        }

        public Quaternion GetRotation(Action _action)
        {
            if (_action.GetGameObject(targetId))
                SetCurrentTransform(_action.GetGameObject(targetId).transform);

            var fromObject = _action.GetGameObject(fromId);
            var toObject = _action.GetGameObject(toId);

            if (fromObject && toObject)
            {
                //relative mode
                Vector3 lookDir = toObject.transform.position - fromObject.transform.position;
                lookDir = Vector3.Normalize(new Vector3(lookDir.x * axisWeight.x, lookDir.y * axisWeight.y, lookDir.z * axisWeight.z));

                Quaternion lookRotation = Quaternion.identity;
                if (lookDir != Vector3.zero)
                {
                    lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
                else
                {
#if UNITY_EDITOR
                        Log.LogE("AGE", "<color=red>[ ModifyTransform GetRotation Attention]</color> LookDir is set to Zero(lookRotation will be set to Quaternion.identity), To Gamebject:"
                            + toObject.name + ", FromObject" + fromObject.name + ",action:" + _action.actionName);
#endif
                }

                return lookRotation * rotation;
            }
            else
            {
                GameObject objectSpace = _action.GetGameObject(objectSpaceId);
                if (objectSpace)
                {
                    //in given space
                    return objectSpace.transform.rotation * rotation;
                }
                else
                {
                    GameObject targetObject = _action.GetGameObject(targetId);
					if (targetObject && targetObject.transform.parent)
                    {
                        //in parent space
                        return targetObject.transform.parent.rotation * rotation;
                    }
                    else
                    {
                        //in world space
                        return rotation;
                    }
                }
            }
        }

        Vector3 GetLookAtPosition(Action _action)
        {
            GameObject lookAtGameObject = _action.GetGameObject(lookAtId);
            if (lookAtGameObject != null)
            {
                Vector3 lookAtPosition = lookAtGameObject.transform.position + lookAtGameObject.transform.rotation * lookAtOffset;
                return lookAtPosition;
            }
            else
            {
                return Vector3.zero;
            }
        }

        void SetCurrentTransform(Transform _transform)
        {
			if (_currentInitialized) return;

            if (currentTranslation)
            {
                objectSpaceId = fromId = toId = -1;
                _translation = translation = _transform.localPosition;
            }

            if (currentRotation)
            {
                objectSpaceId = fromId = toId = -1;
                rotation = _transform.localRotation;
            }

            if (currentScaling)
                scaling = _transform.localScale;

			_currentInitialized = true;
		}

        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as ModifyTransform;
            enableTranslation = copySrc.enableTranslation;
            currentTranslation = copySrc.currentTranslation;
            _translation = translation = copySrc.translation;
            enableRotation = copySrc.enableRotation;
            if(!enableRotation)
            {
                currentRotation = false;
            }
            else
            {
                currentRotation = copySrc.currentRotation;
            }
            rotation = copySrc.rotation;
            enableScaling = copySrc.enableScaling;
            currentScaling = copySrc.currentScaling;
            scaling = copySrc.scaling;
            targetId = copySrc.targetId;
            objectSpaceId = copySrc.objectSpaceId;
            fromId = copySrc.fromId;
            toId = copySrc.toId;
            normalizedRelative = copySrc.normalizedRelative;
            mode = copySrc.mode;
            enableRandomTranslation = copySrc.enableRandomTranslation;
            randomTranslation = copySrc.randomTranslation;
            if (enableTranslation)
            {
                // 记录下随机的值
                if (enableRandomTranslation)
                {
                    // 如果有随机，触发一下随机范围
                    translation.x += Random.Range(-randomTranslation.x, randomTranslation.x);
                    translation.y += Random.Range(-randomTranslation.y, randomTranslation.y);
                    translation.z += Random.Range(-randomTranslation.z, randomTranslation.z);
                    _translation = translation;
                }
            }
        }

        protected override void ClearData()
        {
            enableTranslation = true;
            currentTranslation = false;
            _translation = translation = Vector3.zero;
            enableRotation = true;
            currentRotation = false;
            rotation = Quaternion.identity;
            enableScaling = false;
            currentScaling = false;
            scaling = Vector3.one;
            targetId = -1;
            objectSpaceId = -1;
            fromId = -1;
            toId = -1;
            normalizedRelative = false;
            mode = Mode.Cubic;
            enableRandomTranslation = false;
        }

        public override void OnActionStart(Action _action)
        {
            _currentInitialized = false;
            _translation = translation;
        }

        protected override uint GetPoolInitCount()
        {
            return 3;
        }

        public static Vector3 ResolvePosition(ModifyTransform modifyTransform, Action action)
        {
            return modifyTransform.GetTranslation(action);
        }
        
        public static Vector3 ResolvePositionInTangent(ModifyTransform modifyTransform)
        {
            return modifyTransform.translationInTangent;
        }

        public static Vector3 ResolvePositionOutTangent(ModifyTransform modifyTransform)
        {
            return modifyTransform.translationOutTangent;
        }

        public static Quaternion ResolveRotaion(ModifyTransform modifyTransform, Action action)
        {
            return modifyTransform.GetRotation(action);
        }

        public static Quaternion ResolveRotationInTangent(ModifyTransform modifyTransform)
        {
            return Quaternion.Euler(modifyTransform.rotationInTangent);
        }

        public static Quaternion ResolveRotationOutTangent(ModifyTransform modifyTransform)
        {
            return Quaternion.Euler(modifyTransform.rotationOutTangent);
        }

        public static Vector3 ResolveScale(ModifyTransform modifyTransform, Action action)
        {
            return modifyTransform.scaling;
        }

        public static Vector3 ResolveScaleInTangent(ModifyTransform modifyTransform)
        {
            return modifyTransform.scaleInTangent;
        }

        public static Vector3 ResolveScaleOutTangent(ModifyTransform modifyTransform)
        {
            return modifyTransform.scaleOutTangent;
        }
    }
}