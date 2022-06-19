using System;
using System.Collections.Generic;

namespace AGE.Lua
{
    public class LuaTickEvent : TickEvent
    {
        private string _eventType = "";
        /// <summary>
        /// 供反射用，外部禁止使用
        /// </summary>
        public LuaTickEvent()
        {
        }

        public LuaTickEvent(string eventType)
        {
            _eventType = eventType;
            LuaBaseEvent = this.GetNewLuaBaseEvent(_eventType);
        }

        protected override void CopyData(BaseEvent src)
        {
            LuaTickEvent eve = src as LuaTickEvent;
            _eventType = eve._eventType;
            if (LuaBaseEvent == null)
            {
                LuaBaseEvent = this.GetNewLuaBaseEvent(_eventType, eve.LuaBaseEvent);
            }
            else
            {
                this.LuaCopyData(src);
            }
        }

        protected override void ClearData()
        {
            this.LuaClearData();
            LuaBaseEvent = null;
        }

        public override void Process(Action _action, Track _track)
        {
            this.TickProcess(_action, _track);
        }

        public override void PostProcess(Action _action, Track _track, float _localTime)
        {
            this.TickPostProcess(_action, _track, _localTime);
        }

        public override void ProcessBlend(Action _action, Track _track, TickEvent _prevEvent, float _blendWeight)
        {
            this.TickProcessBlend(_action, _track, _prevEvent, _blendWeight);
        }

        public override List<string> GetAssociatedAction()
        {
            return this.LuaGetAssociatedAction();
        }

        public override Dictionary<string, bool> GetAssociatedResources()
        {
            return this.LuaGetAssociatedResources();
        }

        public override void OnActionStart(Action _action)
        {
            this.LuaOnActionStart(_action);
        }

        public override void OnActionStop(Action _action)
        {
            this.LuaOnActionStop(_action);
        }
    }
}
