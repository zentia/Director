using System;

namespace CinemaDirector
{
	public class SidebarControlEventArgs : EventArgs
	{
		public DirectorObject Behaviour;

		public SidebarControl SidebarControl;

		public SidebarControlEventArgs(DirectorObject behaviour, SidebarControl control)
		{
			Behaviour = behaviour;
			SidebarControl = control;
		}
	}
}
