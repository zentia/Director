using Highlight;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Camera", "镜头位置、旋转、FOV等线性运动", TimelineItemGenre.ActorItem)]
    public class PlayCameraAllMotionAction : TimelineActorAction
    {
        [LabelText("位置(localPosition)")]
        public CurveValue<Vector3> customCurveOfPosition;

        [LabelText("旋转(localEulerAngles)")]
        public CurveValue<Vector3> customCurveOfRotation;

        [LabelText("设置fieldOfView")]
        public CurveValue<float> customCurveOfFieldOfView;

        private Vector3 _startPosition;
        private Vector3 _startAngle;
        private float _startFieldOfView;

        private ICameraOp _cameraOp;

        public override void Trigger(GameObject actor)
        {
            _cameraOp = VirtualCameraExtensions.Create(actor);

            _startPosition = _cameraOp.Position;
            _startAngle = _cameraOp.Angles;
            _startFieldOfView =_cameraOp.FieldOfView;
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            var proportion = runningTime / Duration;

            _cameraOp.FieldOfView = customCurveOfFieldOfView.Calculate(_startFieldOfView, proportion);
            _cameraOp.Position =  customCurveOfPosition.Calculate(_startPosition, proportion);
            _cameraOp.Angles = customCurveOfRotation.Calculate(_startAngle, proportion);
        }

        public override void End(GameObject actor)
        {
            if (customCurveOfPosition.use)
            {
                _cameraOp.Position = customCurveOfPosition.value;
            }

            if (customCurveOfRotation.use)
            {
                _cameraOp.Angles = customCurveOfRotation.value;
            }

            if (customCurveOfFieldOfView.use)
            {
                _cameraOp.FieldOfView = customCurveOfFieldOfView.value;
            }
        }

        public override void Stop(GameObject actor)
        {

        }
    }
}
