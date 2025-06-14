namespace TimelineRuntime
{
    /// <summary>
    /// A basic global event that can be Triggered when the Firetime is reached.
    /// </summary>
    public abstract class TimelineGlobalEvent : TimelineItem
    {
        /// <summary>
        /// Called when the running time of the timeline hits the firetime of the event.
        /// </summary>
        /// <remarks>
        /// <c>Trigger()</c> is only called when the timeline hits the firetime while moving forward, either through regular playback or by scrubbing. For when the playback is reversed, see <see cref="Reverse"/>.
        /// </remarks>
        public abstract void Trigger();

        /// <summary>
        /// Reverse trigger. Called when scrubbing backwards past the firetime of the event.
        /// </summary>
        public virtual void Reverse() { }
    }
}