using System;

namespace CinemaDirector
{
	public class DirectorBehaviourControlEventArgs : EventArgs
	{
		public DirectorObject Behaviour;

		public DirectorBehaviourControl Control;

		public DirectorBehaviourControlEventArgs(DirectorObject behaviour, DirectorBehaviourControl control)
		{
			Behaviour = behaviour;
			Control = control;
		}
	}
}
