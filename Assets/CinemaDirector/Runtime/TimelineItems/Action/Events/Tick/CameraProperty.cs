using UnityEngine;

namespace AGE
{
	public class CameraProperty : TickEvent
	{
		public override bool SupportEditMode ()
		{
			return true;
		}

		public int targetId = 0;
        public bool UseCurProperty = false;
        public enum Projection
		{
			Perspective,
			Orthographic,
		};
		public Projection projection = Projection.Perspective;

		public float Size = 5;
		public float FOV = 60;
		public float NearPlane = 0.3f;
		public float FarPlane = 1000;
		public float Depth = -1;

        Camera camera = null;
        public override void OnActionStart(Action _action)
        {
            GameObject targetObj = _action.GetGameObject(targetId);
            if(targetObj != null)
            {
                camera = targetObj.GetComponent<Camera>();
            }
        }

        public override void Process (Action _action, Track _track)
		{
			GameObject targetObj = _action.GetGameObject(targetId);
			if (targetObj == null)
			{
				return;
			}
            camera = targetObj.GetComponent<Camera>();

            if (targetObj == null) 
				return;
			if (camera == null)
				return;
            if(UseCurProperty)
            {
                FOV = camera.fieldOfView;
                Size = camera.orthographicSize;
                return;
            }

			bool isOrtho = (projection == Projection.Orthographic);
			camera.fieldOfView = FOV;
//			camera.depth = Depth;
//			camera.nearClipPlane = NearPlane;
//			camera.farClipPlane = FarPlane;
			camera.orthographicSize = Size;
			camera.orthographic = isOrtho;
		}

		public override void ProcessBlend(Action _action, Track _track, TickEvent _prevEvent, float _blendWeight)
		{
			GameObject targetObj = _action.GetGameObject(targetId);
			if (targetObj == null || camera == null || _prevEvent == null) 
				return;
			float minusBW = 1.0f - _blendWeight;
			camera.fieldOfView 		= FOV       * _blendWeight + (_prevEvent as CameraProperty).FOV       * minusBW;
//			camera.depth 				= Depth     * _blendWeight + (_prevEvent as CameraProperty).Depth     * minusBW;
//			camera.nearClipPlane 		= NearPlane * _blendWeight + (_prevEvent as CameraProperty).NearPlane * minusBW;
//			camera.farClipPlane 		= FarPlane  * _blendWeight + (_prevEvent as CameraProperty).FarPlane  * minusBW;
			camera.orthographicSize 	= Size      * _blendWeight + (_prevEvent as CameraProperty).Size      * minusBW;
		}


        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as CameraProperty;
            targetId = copySrc.targetId;
            UseCurProperty = copySrc.UseCurProperty;
            projection = copySrc.projection;
            Size = copySrc.Size;
            FOV = copySrc.FOV;
            NearPlane = copySrc.NearPlane;
            FarPlane = copySrc.FarPlane;
            Depth = copySrc.Depth;
        }

        protected override void ClearData()
        {
            targetId = -1;
            UseCurProperty = false;
            projection = Projection.Perspective;
            Size = 5;
            FOV = 60;
            NearPlane = 0.3f;
            FarPlane = 1000;
            Depth = -1;

        }
    }

}
