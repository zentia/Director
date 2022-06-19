using System;

namespace CinemaDirector
{
	public class ActionItemEventArgs : EventArgs
	{
		public DirectorObject actionItem;

		public float firetime;

		public float duration;

		public ActionItemEventArgs(DirectorObject actionItem, float firetime, float duration)
		{
			this.actionItem = actionItem;
			this.firetime = firetime;
			this.duration = duration;
		}
	}
}