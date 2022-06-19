namespace AGE
{
    public abstract class TickEvent : BaseEvent
    {
	    public virtual void Process(Action _action, Track _track) {}
	    public virtual void ProcessBlend(Action _action, Track _track, TickEvent _prevEvent, float _blendWeight) {}
	    public virtual void PostProcess(Action _action, Track _track, float _localTime) {}
        // Tick的是否要等待触发
        public virtual bool IsNeedWait(Action _action)
        {
            return false;
        }

        public override bool IsDuration()
        {
            return false;
        }

        public override bool IsCondition()
        {
            return false;
        }
    }
}
