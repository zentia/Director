using System;

namespace CinemaDirector
{
	public class TrackItemEventArgs : EventArgs
	{
		public UnityEngine.Object item;

		public float firetime;

		public TrackItemEventArgs(UnityEngine.Object item, float firetime)
		{
			this.item = item;
			this.firetime = firetime;
		}
	}
}