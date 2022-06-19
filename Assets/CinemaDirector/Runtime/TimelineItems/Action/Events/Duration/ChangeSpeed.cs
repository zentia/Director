using UnityEngine;
using System.Collections;
using System;

namespace AGE
{
    [Serializable]
    [EventCategory("ChangeSpeed")]
    public class ChangeSpeed : DurationEvent
	{
		public enum Mode
		{ 
			Specified = 0, 
			AverageSpeed = 1, 
		};
		
		public Mode mode = Mode.Specified;
		public bool effectSubAction = true;
        //specified
        public float playSpeed = 1.0f;
		public bool effectCurSpeed = false;

        //by average speed of object
        public float averageSpeed = 1.0f;
        public int fromId = -1;
        public int toId = -1;

		public override bool SupportEditMode ()
		{
			return true;
		}

		public override void Enter(Action _action, Track _track)
		{
			switch (mode)
			{
			case Mode.Specified:
			{
				//_action.PlaySpeed = playSpeed;
				_action.SetPlaySpeed(effectCurSpeed ? playSpeed*_action.PlaySpeed : playSpeed,effectSubAction);
				break;
			}
			case Mode.AverageSpeed:
			{
				GameObject fromObject = _action.GetGameObject(fromId);
				GameObject toObject = _action.GetGameObject(toId);
				if (fromObject != null && toObject != null)
				{
					float range = averageSpeed * length;
					float realRange = (toObject.transform.position - fromObject.transform.position).magnitude;
					//_action.PlaySpeed = range / realRange;
					float spd = range / realRange;
					spd = Mathf.Clamp(spd, 0.0f, Action.MAX_ACTION_SPEED);
					_action.SetPlaySpeed(spd,effectSubAction);
				}
				break;
			}
			}
		}
		
		public override void Leave(Action _action, Track _track)
		{
			if( _track.IsBlending() )
				return;
			//_action.PlaySpeed = 1.0f;
			_action.SetPlaySpeed(effectCurSpeed ? 1.0f/playSpeed*_action.PlaySpeed : 1.0f);
		}


        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            ChangeSpeed r = src as ChangeSpeed;
            mode = r.mode;
			effectSubAction = r.effectSubAction;
            playSpeed = r.playSpeed;
			effectCurSpeed = r.effectCurSpeed;
            averageSpeed = r.averageSpeed;
            fromId = r.fromId;
            toId = r.toId;
        }

        protected override void ClearData()
        {
            base.ClearData();
            mode = Mode.Specified;
			effectSubAction = true;
            playSpeed = 1.0f;
			effectCurSpeed = false;
            averageSpeed = 1.0f;
            fromId = -1;
            toId = -1;
        }
    }
}
