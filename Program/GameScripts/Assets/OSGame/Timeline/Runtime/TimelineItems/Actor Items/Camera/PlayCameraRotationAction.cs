using Assets.Plugins.Common;
using Highlight;
using Root;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Camera", "镜头旋转", TimelineItemGenre.ActorItem)]
    public class PlayCameraRotationAction : TimelineActorAction
    {
        [LabelText("旋转到的角度")]
        public Vector3 endAngle;

        [LabelText("自定义运动曲线")]
        public AnimationCurve curve;

        [LabelText("自定义起始角度？")]
        public bool customStartingAngle;

        [LabelText("起始角度")]
        public Vector3 startingAngle;

        private ICameraOp _cameraOp;
        public override void Trigger(GameObject actor)
        {
            HighlightCameraManager.instance.TryInitData();
            _cameraOp = VirtualCameraExtensions.Create(actor);

            // Log.LogE(LogTag.Timeline, "================================触发");
            if (!customStartingAngle)
            {
                startingAngle = _cameraOp.Angles;
            }
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            _cameraOp.Angles = Vector3.Lerp(startingAngle, endAngle, curve.Evaluate(runningTime / duration));
            // Log.LogE(LogTag.Timeline, $"================================UpdateTime 运行时长：{runningTime} 间隔：{deltaTime} 比例：{runningTime / duration}");
        }

        public override void End(GameObject actor)
        {
            // Log.LogE(LogTag.Timeline, "================================End");

            _cameraOp.Angles = endAngle;
        }

        public override void Stop(GameObject actor)
        {
            // Log.LogE(LogTag.Timeline, "================================Stop");
        }
    }
}
