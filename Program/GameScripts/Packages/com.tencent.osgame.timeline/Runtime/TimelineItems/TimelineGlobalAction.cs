using System;
using UnityEngine;

namespace TimelineRuntime
{
    [ExecuteInEditMode]
    public abstract class TimelineGlobalAction : TimelineAction
    {
        public abstract void Trigger();

        public virtual void UpdateTime(float time, float deltaTime) { }

        public abstract void End();

        public virtual void SetTime(float time, float deltaTime) { }

        public virtual void Pause() { }

        public virtual void Resume() { }

        public virtual void ReverseTrigger() { }

        public virtual void ReverseEnd() { }
    }
}