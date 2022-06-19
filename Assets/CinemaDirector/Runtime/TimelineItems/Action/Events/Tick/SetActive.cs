using Assets.Plugins.Common;
using CinemaDirector;
using UnityEngine;

namespace AGE
{
    public class SetActive : TickEvent
    {
        public bool enabled = true;
        [Template]
        public int targetId = 0;

        public override void Process(Action _action, Track _track)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if(targetObject != null)
            {
                targetObject.ExtSetActive(enabled);
            }
        }

        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as SetActive;
            enabled = copySrc.enabled;
            targetId = copySrc.targetId;
        }

        protected override void ClearData()
        {
            enabled = true;
            targetId = 0;
        }
    }
}
