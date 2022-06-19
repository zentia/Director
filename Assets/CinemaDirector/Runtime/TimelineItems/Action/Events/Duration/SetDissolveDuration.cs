using UnityEngine;
using CinemaDirector;

namespace AGE
{
    [EventCategory("Alpha/Cutscene")]
    public class SetDissolveDuration : DurationEvent
    {

        public override bool SupportEditMode()
        {
            return true;
        }

        [Template]
        public int targetId = -1;

        public float StartU = 0f;
        public float EndU = 0f;
        public float StartV = 0f;
        public float EndV = 0f;

        private Renderer[] _renders;
		private string ShaderName = "AlphaGame/scene_blend_lightmap_rongjie";

        public override void Enter(Action _action, Track _track)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null)
            {
                return;
            }

            _renders = targetObject.GetComponents<Renderer>();
            if(_renders == null)
            {
                return;
            }

          
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            if (_renders == null)
            {
                return;
            }

            float percent = _localTime / length;
            Vector2 start = Vector2.zero;
            Vector2 end = Vector2.zero;
            start.x = StartU;
            start.y = StartV;
            end.x = EndU;
            end.y = EndV;
            Vector2 result = Vector2.Lerp(start, end, percent);
            for (int i = 0, imax = _renders.Length; i < imax; i++)
            {
                if (_renders[i].material.shader.name.Equals(ShaderName))
                {
                    _renders[i].material.SetFloat("_U", result.x);
                    _renders[i].material.SetFloat("_V", result.y);
                }
            }
        }

        public override void Leave(Action _action, Track _track)
        {


        }


        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as SetDissolveDuration;
            targetId = srcCopy.targetId;
            StartU = srcCopy.StartU;
            EndU = srcCopy.EndU;
            StartV = srcCopy.StartV;
            EndV = srcCopy.EndV;
        }

        protected override void ClearData()
        {
            base.ClearData();
            targetId = -1;
            _renders = null;
            StartU = 0f;
            EndU = 0f;
            StartV = 0f;
            EndV = 0f;
        }
    }

}