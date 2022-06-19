using System.Collections.Generic;

namespace AGE.Lua
{
    public class LuaDurationEvent : DurationEvent
    {
        private string _eventType = "";
        /// <summary>
        /// 供反射用，外部禁止使用
        /// </summary>
        public LuaDurationEvent()
        {
        }

        public LuaDurationEvent(string eventType)
        {
            _eventType = eventType;
            LuaBaseEvent = this.GetNewLuaBaseEvent(_eventType);
        }

        protected override void CopyData(BaseEvent src)
        {
            LuaDurationEvent eve = src as LuaDurationEvent;
            _eventType = eve._eventType;
            base.CopyData(src);
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
            base.ClearData();
            this.LuaClearData();
            LuaBaseEvent = null;
        }

        public override void Enter(Action _action, Track _track)
        {
            this.DurationEnter(_action, _track);
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            this.DurationProcess(_action, _track, _localTime);
        }

        public override void Leave(Action _action, Track _track)
        {
            this.DurationLeave(_action, _track);
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
