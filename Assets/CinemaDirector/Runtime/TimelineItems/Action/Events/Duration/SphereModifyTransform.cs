using UnityEngine;
using CinemaDirector;

namespace AGE
{
    [EventCategory("Movement")]
    public class SphereModifyTransform : DurationEvent
    {
        //操作对象
        [Template]
        public int targetId = 0;

        //球心对象
        [Template]
        public int centerId = 0;

        //终点相对于球心对象的平移量
        public Vector3 toOffset;

        //朝向相对于球心对象的平移量
        public Vector3 lookAtOffset;
        
        //Z轴旋转
        public float zAngle;

        //距离差值方式
        public LeanTweenType distanceTweenType;

        //水平旋转差值方式
        public LeanTweenType hAngleTweenType;

        //垂直旋转差值方式
        public LeanTweenType vAngleTweenType;

        //Z轴旋转差值方式
        public LeanTweenType zAngleTweenType;

        //插值起点
        private Vector3 fromPosition;
        //球心位置
        private Vector3 centerPosition;
        //插值终点
        private Vector3 toPosition;
        //朝向位置
        private Vector3 lookAtPosition;
        //操作对象
        private GameObject target;
        //球形差值的起点距离
        private float fromDistance;
        //球形差值的起点垂直旋转
        private float fromVAngle;
        //球形差值的起点水平旋转
        private float fromHAngle;
        //球形差值的起点Z轴旋转
        private float fromZAngle;
        //球形差值的终点距离
        private float toDistance;
        //球形差值的终点垂直旋转
        private float toVAngle;
        //球形差值的终点水平旋转
        private float toHAngle;
        //球形差值的终点Z轴旋转
        private float toZAngle;
        //操作对象的旋转
        private Quaternion toRatation;

        public override void Enter(Action _action, Track _track)
        {
            base.Enter(_action, _track);
            target = _action.GetGameObject(targetId);
            GameObject toGo = _action.GetGameObject(centerId);

            if (toGo)
            {
                centerPosition = toGo.transform.position;
                toPosition = toGo.transform.position + toGo.transform.rotation * toOffset;
                lookAtPosition = toGo.transform.position + toGo.transform.rotation * lookAtOffset;
                toRatation = toGo.transform.rotation;
                if (target)
                {
                    fromDistance = Vector3.Distance(target.transform.position, centerPosition);

                    Vector3 angles = Quaternion.FromToRotation(Vector3.forward, target.transform.forward).eulerAngles;

                    fromHAngle = angles.y;
                    fromVAngle = angles.x;
                    fromZAngle = angles.z;

                    toDistance = (lookAtPosition - toPosition).sqrMagnitude;

                    angles = Quaternion.FromToRotation(Vector3.forward, lookAtPosition - toPosition).eulerAngles;

                    toHAngle = fromHAngle + Mathf.DeltaAngle(fromHAngle, angles.y);
                    toVAngle = fromVAngle + Mathf.DeltaAngle(fromVAngle, angles.x);
                    toZAngle = fromZAngle + Mathf.DeltaAngle(fromZAngle, zAngle);
                }
            }
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            Process(_localTime);
            base.Process(_action, _track, _localTime);
        }

        private void Process(float _localTime)
        {
            float distance = LeanTweenExt.TweenLerp(fromDistance, toDistance, _localTime / length, distanceTweenType == LeanTweenType.notUsed ? LeanTweenType.linear : distanceTweenType);
            float vAngle = LeanTweenExt.TweenLerp(fromVAngle, toVAngle, _localTime / length, vAngleTweenType == LeanTweenType.notUsed ? LeanTweenType.linear : vAngleTweenType);
            float hAngle = LeanTweenExt.TweenLerp(fromHAngle, toHAngle, _localTime / length, hAngleTweenType == LeanTweenType.notUsed ? LeanTweenType.linear : hAngleTweenType);
            float zAngle = LeanTweenExt.TweenLerp(fromZAngle, toZAngle, _localTime / length, zAngleTweenType == LeanTweenType.notUsed ? LeanTweenType.linear : zAngleTweenType);


            Quaternion rotation = Quaternion.Euler(vAngle, hAngle, 0);

            if (target)
            {
                target.transform.position = centerPosition + (rotation * new Vector3(0, 0, -distance));

                rotation = Quaternion.LookRotation(lookAtPosition - target.transform.position);
                Vector3 rotationVector = rotation.eulerAngles;
                rotationVector.z = zAngle;
                target.transform.rotation = Quaternion.Euler(rotationVector);
            }
        }

        public override void Leave(Action _action, Track _track)
        {
            Process(length);
            base.Leave(_action, _track);
        }

        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as SphereModifyTransform;
            targetId = srcCopy.targetId;
            centerId = srcCopy.centerId;
            toOffset = srcCopy.toOffset;
            lookAtOffset = srcCopy.lookAtOffset;
            zAngle = srcCopy.zAngle;
            distanceTweenType = srcCopy.distanceTweenType;
            hAngleTweenType = srcCopy.hAngleTweenType;
            vAngleTweenType = srcCopy.vAngleTweenType;
            zAngleTweenType = srcCopy.zAngleTweenType;
        }

        protected override void ClearData()
        {
            targetId = -1;
            centerId = -1;
            toOffset.Set(0, 0, 0);
            lookAtOffset.Set(0, 0, 0);
            zAngle = 0;
            distanceTweenType = 0;
            hAngleTweenType = 0;
            vAngleTweenType = 0;
            zAngleTweenType = 0;

            fromPosition.Set(0, 0, 0);
            centerPosition.Set(0, 0, 0);
            toPosition.Set(0, 0, 0);
            lookAtPosition.Set(0, 0, 0);
            target = null;
            fromDistance = 0;
            fromVAngle = 0;
            fromHAngle = 0;
            fromZAngle = 0;
            toDistance = 0;
            toVAngle = 0;
            toHAngle = 0;
            toZAngle = 0;
            toRatation.Set(0, 0, 0, 0);
        }

        protected override uint GetPoolInitCount()
        {
            return 0;
        }
    }
}

