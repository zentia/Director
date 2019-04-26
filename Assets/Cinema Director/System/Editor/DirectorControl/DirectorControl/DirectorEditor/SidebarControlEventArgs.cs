using System;
using UnityEngine;

namespace DirectorEditor
{
	public class SidebarControlEventArgs : EventArgs
	{
		public Behaviour Behaviour;

		public SidebarControl SidebarControl;

		public SidebarControlEventArgs(Behaviour behaviour, SidebarControl control)
		{
			this.Behaviour = behaviour;
			this.SidebarControl = control;
		}
	}
}
