using System;
using UnityEngine;

namespace CinemaDirector
{
	public class CurveClipItemEventArgs : EventArgs
	{
		public TimelineItem curveClipItem;

		public float firetime;

		public float duration;

		public CurveClipItemEventArgs(TimelineItem curveClipItem, float firetime, float duration)
		{
			this.curveClipItem = curveClipItem;
			this.firetime = firetime;
			this.duration = duration;
		}
	}
}
