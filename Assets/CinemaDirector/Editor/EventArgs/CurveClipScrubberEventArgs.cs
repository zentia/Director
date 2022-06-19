using System;
using UnityEngine;

namespace CinemaDirector
{
	public class CurveClipScrubberEventArgs : EventArgs
	{
		public TimelineItem curveClipItem;

		public float time;

		public CurveClipScrubberEventArgs(TimelineItem curveClipItem, float time)
		{
			this.curveClipItem = curveClipItem;
			this.time = time;
		}
	}
	
}
