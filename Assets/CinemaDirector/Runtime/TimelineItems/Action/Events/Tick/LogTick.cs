using Assets.Plugins.Common;
using UnityEngine;

namespace AGE
{

    [EventCategory("Debug")]
    public class LogTick : TickEvent
    {
        public enum Type
        {
            Log,
            Warning,
            Error,
        }

        public string text = "";
        public Type type = Type.Log;

        public override void Process(Action _action, Track _track)
        {
            string content = text + " actionName:" + _action.actionName;
            if(type == Type.Log)
            {
                Log.LogD("AGE", content);
            }
            else if(type == Type.Warning)
            {
                Log.LogE("AGE", content);
            }
            else
            {
                Log.LogE("AGE", content);
            }
        }

        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as LogTick;
            text = copySrc.text;
            type = copySrc.type;
        }

        protected override void ClearData()
        {
            text = "";
            type = Type.Log;
        }
    }
}
