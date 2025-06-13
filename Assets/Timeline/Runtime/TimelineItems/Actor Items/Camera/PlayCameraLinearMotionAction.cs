using Assets.Plugins.Common;
using Highlight;
using Root;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Camera", "镜头线性运动", TimelineItemGenre.ActorItem)]
    public class PlayCameraLinearMotionAction : TimelineActorAction
    {
        [LabelText("终点位置")]
        public Vector3 endPosition;

        [LabelText("自定义运动曲线")]
        public AnimationCurve curve;

        [LabelText("自定义起始位置？")]
        public bool customStartingPosition;

        [LabelText("起始位置")]
        public Vector3 startingPosition;

        private ICameraOp _cameraOp;

        public override void Trigger(GameObject actor)
        {
            HighlightCameraManager.instance.TryInitData();

            _cameraOp = VirtualCameraExtensions.Create(actor);

            // Log.LogE(LogTag.Timeline, "================================触发");
            if (!customStartingPosition)
            {
                startingPosition = _cameraOp.Position;
            }
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            _cameraOp.Position = Vector3.Lerp(startingPosition, endPosition, curve.Evaluate(runningTime / duration));
            // Log.LogE(LogTag.Timeline, $"================================UpdateTime 运行时长：{runningTime} 间隔：{deltaTime} 比例：{runningTime / duration}");
        }

        public override void End(GameObject actor)
        {
            // Log.LogE(LogTag.Timeline, "================================End");

            // _cameraOp.Position = endPosition;
        }

        public override void Stop(GameObject actor)
        {
            // Log.LogE(LogTag.Timeline, "================================Stop");
        }
    }
}
