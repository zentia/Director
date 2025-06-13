using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Time", "Time Scale Curve", TimelineItemGenre.GlobalItem)]
    public class TimeScaleCurveAction : TimelineGlobalAction, IRecoverableObject
    {
        // The curve that controls time scale. Make sure that it goes from 0 to the duration of this action.
        public AnimationCurve Curve;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        // Keep track of the GameObject's previous state when calling Trigger.
        private float previousScale;

        /// <summary>
        /// Cache the initial state of the time scale.
        /// </summary>
        /// <returns>The Info necessary to revert this event.</returns>
        public RevertInfo[] CacheState()
        {
            return new RevertInfo[]{ new(this, typeof(Time), "timeScale", Time.timeScale)};
        }

        /// <summary>
        /// Trigger the start of the time curve. Save the current time scale.
        /// </summary>
        public override void Trigger()
        {
            previousScale = Time.timeScale;
        }

        /// <summary>
        /// Update the Time Scale using the animation curve.
        /// </summary>
        /// <param name="time">The time of this action.</param>
        /// <param name="deltaTime">The time since the last update.</param>
        public override void UpdateTime(float time, float deltaTime)
        {
            if (Curve != null)
            {
                Time.timeScale = Curve.Evaluate(time);
            }
        }

        /// <summary>
        /// End the Time Scale action.
        /// </summary>
        public override void End() { }

        /// <summary>
        /// Reset the time scale to the previous value when exiting this action in reverse.
        /// </summary>
        public override void ReverseTrigger()
        {
            Time.timeScale = previousScale;
        }

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Runtime.
        /// </summary>
        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}
