namespace CinemaDirector
{
	public class TimelineItemWrapper
	{
		protected float firetime;

		private TimelineItem timelineItem;

		public TimelineItem TimelineItem
		{
			get
			{
				return timelineItem;
			}
		}

		public float Firetime
		{
			get
			{
				return firetime;
			}
			set
			{
				firetime = value;
			}
		}

		public TimelineItemWrapper(TimelineItem behaviour, float firetime)
		{
			timelineItem = behaviour;
			this.firetime = firetime;
		}
	}

}
