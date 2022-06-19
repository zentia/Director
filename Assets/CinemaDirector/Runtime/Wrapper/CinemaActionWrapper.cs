using UnityEngine;

namespace CinemaDirector
{
	public class CinemaActionWrapper : TimelineItemWrapper
	{
		private float duration;

		public float Duration
		{
			get
			{
				return duration;
			}
			set
			{
				duration = value;
			}
		}

		public CinemaActionWrapper(TimelineItem behaviour, float firetime, float duration) : base(behaviour, firetime)
		{
			this.duration = duration;
		}
	}
}
