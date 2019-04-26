using System;
using UnityEngine;

namespace DirectorEditor
{
	public class DirectorBehaviourControlEventArgs : EventArgs
	{
		public Behaviour Behaviour;

		public DirectorBehaviourControl Control;

		public DirectorBehaviourControlEventArgs(Behaviour behaviour, DirectorBehaviourControl control)
		{
			this.Behaviour = behaviour;
			this.Control = control;
		}
	}
}
