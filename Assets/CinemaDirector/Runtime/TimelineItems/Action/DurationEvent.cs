using CinemaDirector;

namespace AGE
{
	public abstract class DurationEvent : TimelineAction
	{
		public float length = 0;
		
		//modifing Start changes length and keeps End unchanged
		//unless Start > End
		//in this case, length is set to 0, End is set to Start
		public float Start
		{
			get	{ return time; }
			set
			{
				float end = End;
				
				time = value;
				if (value < end)
					length = end - time;
				else
					length = 0;
			}
		}
		//modifing End changes length and keeps Start unchanged
		//unless Start > End
		//in this case, length is set to 0, Start is set to End
		public float End
		{
			get { return time + length; }
			set
			{
				if (value > time)
				{
					length = value - time;
				}
				else
				{
					length = 0;
					time = value;
				}
			}
		}
		
		//_localTime is always ensured to be between 0 and length
		public virtual void Process(Action _action, Track _track, float _localTime)
		{
			
		}
		public void ProcessBlend(Action _action, Track _track, float _localTime, DurationEvent _prevEvent, float _prevLocalTime, float _blendWeight)
		{
			if (_prevEvent != null)
				_prevEvent.Process(_action, _track, _prevLocalTime);
			Process(_action, _track, _localTime);
		}
		
		public virtual void Enter(Action _action, Track _track) {}
		public virtual void EnterBlend(Action _action, Track _track, BaseEvent _prevEvent, float _blendTime)
		{
			Enter (_action, _track);
		}

		public virtual void Leave(Action _action, Track _track) {}
		public void LeaveBlend(Action _action, Track _track, BaseEvent _nextEvent, float _blendTime)
		{
			Leave( _action, _track );
		}

	    protected override void CopyData(BaseEvent src)
	    {
	        DurationEvent r = src as DurationEvent;
	        length = r.length;
	    }
        protected override void ClearData()
        {
            length = 0;
        }

        public override bool IsDuration()
        {
            return true;
        }

        public override bool IsCondition()
        {
            return false;
        }
#if UNITY_EDITOR
		public virtual void ReverseTrigger(Action _action, Track _track)
        {

        }

		public virtual void ReverseEnd(Action _action, Track _track)
        {

        }
#endif
	}
}
