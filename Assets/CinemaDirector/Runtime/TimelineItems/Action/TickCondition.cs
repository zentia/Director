using UnityEngine;
using System.Collections;

namespace AGE
{
    public abstract class TickCondition : TickEvent
    {
        public override void Process(Action _action, Track _track)
        {
            _action.SetCondition(_track, Check(_action, _track));
        }
        // 	public override void ProcessBlend(Action _action, Track _track, TickEvent _prevEvent, float _blendWeight)
        // 	{
        // 		//_action.SetCondition(_track, false);
        // 	}
        // 	public override void PostProcess(Action _action, Track _track, float _localTime)
        // 	{
        // 		//_action.SetCondition(_track, false);
        // 	}

        public virtual bool Check(Action _action, Track _track) { return true; }

        public override bool IsCondition()
        {
            return true;
        }
    }
}
