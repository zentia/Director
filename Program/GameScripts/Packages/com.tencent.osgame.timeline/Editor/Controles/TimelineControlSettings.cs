namespace TimelineEditor
{
    public class TimelineControlSettings
    {
        internal bool hRangeLocked { get; set; }

        internal float hRangeMax { get; set; } = float.PositiveInfinity;

        public float HorizontalRangeMin { get; set; } = float.NegativeInfinity;

        internal bool hSlider { get; set; } = true;

        internal bool scaleWithWindow { get; set; } = true;
    }
}
