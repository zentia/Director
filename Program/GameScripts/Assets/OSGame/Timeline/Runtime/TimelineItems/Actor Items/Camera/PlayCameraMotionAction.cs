using System;
using Highlight;
using Root;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Camera", "镜头运动", TimelineItemGenre.ActorItem)]
    public class PlayCameraMotionAction : TimelineActorAction
    {
        [LabelText("位置(localPosition)")]
        public CustomCurveOfVector3 customCurveOfPosition;

        [LabelText("旋转(localEulerAngles)")]
        public CustomCurveOfVector3 customCurveOfRotation;

        [LabelText("fieldOfView")]
        public CustomCurve fieldOfView;

        [LabelText("设置初始位置(localPosition)")]
        public StartValue<Vector3> startPosition;

        [LabelText("设置初始旋转(localEulerAngles)")]
        public StartValue<Vector3> startAngles;

        [LabelText("设置初始fieldOfView")]
        public StartValue<float> startFieldOfView;

        private Vector3 _startPosition;
        private Vector3 _startAngle;
        private float _startFieldOfView;

        private ICameraOp _cameraOp;

        public override void Trigger(GameObject actor)
        {
            _cameraOp = VirtualCameraExtensions.Create(actor);
            _startPosition = startPosition.use ? startPosition.value : _cameraOp.Position;

            _startAngle = startAngles.use ? startAngles.value : _cameraOp.Angles;

            _startFieldOfView = startFieldOfView.use ? startFieldOfView.value : _cameraOp.FieldOfView;
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            var proportion = runningTime / Duration;
            RefreshFieldOfView(proportion);

            _cameraOp.Position =  CalculateCurveOfVector3(customCurveOfPosition, _startPosition, proportion);
            _cameraOp.Angles = CalculateCurveOfVector3(customCurveOfRotation, _startAngle, proportion);
        }

        public override void End(GameObject actor)
        {
            var pos = CopyFromCustomCurveOfVector3(customCurveOfPosition, actor.transform.localPosition);
            _cameraOp.Position = pos;

            var angles = CopyFromCustomCurveOfVector3(customCurveOfRotation, actor.transform.localEulerAngles);
            _cameraOp.Angles = angles;

            if (fieldOfView.use)
            {
                _cameraOp.FieldOfView = fieldOfView.value;
            }
        }

        public override void Stop(GameObject actor)
        {

        }

        private static Vector3 CopyFromCustomCurveOfVector3(CustomCurveOfVector3 customCurveOfVector3, Vector3 v)
        {
            if (customCurveOfVector3.x.use)
            {
                v.x = customCurveOfVector3.x.value;
            }
            if (customCurveOfVector3.y.use)
            {
                v.y = customCurveOfVector3.y.value;
            }

            if (customCurveOfVector3.z.use)
            {
                v.z = customCurveOfVector3.z.value;
            }

            return v;
        }

        private void RefreshFieldOfView(float proportion)
        {
            _cameraOp.FieldOfView = CalculateCurve(fieldOfView, _startFieldOfView, proportion);
        }

        private static float CalculateCurve(CustomCurve curve, float startValue, float proportion)
        {
            if (!curve.use)
                return startValue;

            return startValue + (curve.value - startValue) * curve.curve.Evaluate(proportion);
        }

        private static Vector3 CalculateCurveOfVector3(CustomCurveOfVector3 curve, Vector3 startValue, float proportion)
        {
            var res = startValue;

            res.x = CalculateCurve(curve.x, startValue.x, proportion);
            res.y = CalculateCurve(curve.y, startValue.y, proportion);
            res.z = CalculateCurve(curve.z, startValue.z, proportion);

            return res;
        }

        [Serializable]
        public class CustomCurve
        {
            [LabelText("使用？")]
            public bool use;

            [LabelText("值")]
            [ShowIf("use")]
            public float value;

            [LabelText("自定义运动曲线")]
            [ShowIf("use")]
            public AnimationCurve curve;
        }

        [Serializable]
        public class CustomCurveOfVector3
        {
            [LabelText("X")]
            public CustomCurve x;

            [LabelText("Y")]
            public CustomCurve y;

            [LabelText("Z")]
            public CustomCurve z;
        }

        [Serializable]
        public class StartValue<T>
        {
            [LabelText("使用？")]
            public bool use;

            [LabelText("值")]
            [ShowIf("use")]
            public T value;
        }
    }
}
