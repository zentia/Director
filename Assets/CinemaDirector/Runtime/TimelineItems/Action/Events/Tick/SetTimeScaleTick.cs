using UnityEngine;

namespace AGE
{
    public class SetTimeScaleTick : TickEvent
    {
        public float timeScale = 0f;

        public override void Process(Action _action, Track _track)
        {
            Time.timeScale = timeScale;
        }

        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as SetTimeScaleTick;
            timeScale = srcCopy.timeScale;
        }

        protected override void ClearData()
        {
            timeScale = 0f;
        }

        protected override uint GetPoolInitCount()
        {
            return 1;
        }
    }
}

