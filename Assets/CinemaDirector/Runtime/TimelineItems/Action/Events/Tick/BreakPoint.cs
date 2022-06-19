using UnityEngine;
using System.Collections;
using Assets.Plugins.Common;

namespace AGE
{

    [EventCategory("Debug")]
    public class BreakPoint : TickEvent
    {
        public bool enabled = true;
        public string info = "";

        public override void Process(Action _action, Track _track)
        {
#if UNITY_EDITOR
            if (enabled)
            {
                Log.LogD("AGE", "Action \"" + _action.name + "\" triggered break point on time: " + time.ToString() + "s\nInfo: " + info);
                UnityEditor.EditorApplication.isPaused = true;
            }
#endif
        }

        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as BreakPoint;
            enabled = copySrc.enabled;
            info = copySrc.info;
        }

        protected override void ClearData()
        {
            enabled = true;
            info = "";
        }
    }
}
