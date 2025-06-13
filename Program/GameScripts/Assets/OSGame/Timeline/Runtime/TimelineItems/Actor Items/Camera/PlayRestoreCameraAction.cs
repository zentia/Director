using Root;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Camera", "还原镜头", TimelineItemGenre.ActorItem)]
    public class PlayRestoreCameraAction : TimelineActorAction
    {
        [LabelText("位置变化曲线")]
        public AnimationCurve posCurve;

        [LabelText("朝向变化曲线")]
        public AnimationCurve angleCurve;

        [LabelText("fieldOfView变化曲线")]
        public AnimationCurve fieldOfViewCurve;

        private Vector3 _endPos;
        private Vector3 _endAngles;
        private float _endFieldOfView;

        private Vector3 _startPos;
        private Vector3 _startAngles;
        private float _startFieldOfView = 31;

        private Camera _camera;

        private GameObject _gameObjectHandle;

        public override void Trigger(GameObject actor)
        {
            HighlightCameraManager.instance.TryInitData();
            _endPos = HighlightCameraManager.instance.OriginalPosition;
            _endAngles = HighlightCameraManager.instance.OriginalAngles;
            _endFieldOfView = HighlightCameraManager.instance.OriginalFieldOfView;

            _gameObjectHandle = CameraSystem.instance.mainCamera.gameObject;
            _startPos = _gameObjectHandle.transform.localPosition;
            _startAngles = _gameObjectHandle.transform.localEulerAngles;

            _camera = _gameObjectHandle.GetComponent<Camera>();
            if (_camera != null)
            {
                _startFieldOfView = _camera.fieldOfView;
            }
        }

        public override void UpdateTime(GameObject actor, float runningTime, float deltaTime)
        {
            var t = runningTime / Duration;
            if (_camera != null)
            {
                _camera.fieldOfView = _startFieldOfView +
                                      (_endFieldOfView - _startFieldOfView) * fieldOfViewCurve.Evaluate(t);
            }

            _gameObjectHandle.transform.localPosition = Vector3.Lerp(_startPos, _endPos, posCurve.Evaluate(t));
            _gameObjectHandle.transform.localEulerAngles = Vector3.Lerp(_startAngles, _endAngles, angleCurve.Evaluate(t));
        }

        public override void End(GameObject actor)
        {
            _gameObjectHandle.transform.localPosition = _endPos;
            _gameObjectHandle.transform.localEulerAngles = _endAngles;
            if (_camera != null)
            {
                _camera.fieldOfView = _endFieldOfView;
            }

            HighlightCameraManager.instance.RestoreSaveOriginalInfo();
        }

        public override void Stop(GameObject actor)
        {

        }
    }
}
