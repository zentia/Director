using CinemaDirector;
using UnityEngine;

namespace AGE
{
    [CutsceneItem("Utility", "SetTimeScale", CutsceneItemGenre.GenericItem)]
    public class SetTimeScale : DurationEvent
    {
        public float timeScale = 1.0f;
        public float enterDuration = 0.0f;
        public float leaveDuration = 0.0f;

        private float mOldTimeScale = 1.0f;

        public override void Enter(Action _action, Track _track)
        {
            mOldTimeScale = Time.timeScale;
            if (enterDuration <= 0.00001f && enterDuration >= -0.00001f)
            {
                Time.timeScale = timeScale;
            }
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            
            if (Mathf.Abs(enterDuration) > 0.00001f && _localTime <= Start + enterDuration)
            {
                Time.timeScale = Mathf.Lerp(mOldTimeScale, timeScale, enterDuration);
            }

            if (Mathf.Abs(leaveDuration) > 0.00001f && _localTime + leaveDuration >= End)
            {
                Time.timeScale = Mathf.Lerp(timeScale, mOldTimeScale, leaveDuration);
            }
        }

        public override void Leave(Action _action, Track _track)
        {
            Time.timeScale = mOldTimeScale;
        }

        protected override void CopyData(BaseEvent src)
        {
            base.CopyData(src);
            var srcCopy = src as SetTimeScale;
            timeScale = srcCopy.timeScale;
        }

        protected override void ClearData()
        {
            base.ClearData();
            timeScale = 1.0f;
            enterDuration = 0.0f;
            leaveDuration = 0.0f;

            mOldTimeScale = 1.0f;
        }
    }
}

