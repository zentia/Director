using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Time", "Set Time Scale", TimelineItemGenre.GlobalItem)]
    public class SetTimeScaleEvent : TimelineGlobalEvent
    {
        public float TimeScale = 1f;

        public override void Trigger()
        {
            Time.timeScale = TimeScale;
        }

        public override void Stop()
        {
            Time.timeScale = 1;
        }
    }
}