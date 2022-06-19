using System;
using UnityEngine;

namespace CinemaDirector
{
	public class TrackControlEventArgs : EventArgs
	{
		public Behaviour TrackBehaviour;

		public TimelineTrackControl TrackControl;

		public TrackControlEventArgs(Behaviour behaviour, TimelineTrackControl control)
		{
			TrackBehaviour = behaviour;
			TrackControl = control;
		}
	}
}