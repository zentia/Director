using Assets.Scripts.Framework.Lua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Utility", "SendLogicEvent", TimelineItemGenre.GlobalItem)]


    public class SendLogicEvent : TimelineGlobalEvent
    {
        public string eventName = String.Empty;
        public string paramValue = String.Empty;
        public bool sendToLua  = false;
        public bool sendToCS = false;
        public override void Trigger()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            if (sendToLua)
            {
                LuaService.instance.Interaction.SendLuaEvent(eventName, paramValue);
            }
            if (sendToCS)
            {
                //等待有缘人拓展
            }
        }
    }
}
