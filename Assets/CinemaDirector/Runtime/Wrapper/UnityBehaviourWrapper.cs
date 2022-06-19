using System;
using UnityEngine;

namespace CinemaDirector
{
	[Serializable]
	public class UnityBehaviourWrapper
	{
		[SerializeField]
		private DirectorObject behaviour;
		[SerializeField]
		private bool hasChanged = true;

		public DirectorObject Behaviour
		{
			get
			{
				return behaviour;
			}
			set
			{
				behaviour = value;
			}
		}

		public bool HasChanged
		{
			get
			{
				return hasChanged;
			}
			set
			{
				hasChanged = value;
				if (value)
				{
					behaviour.UpdateRaw();
				}
			}
		}

		public UnityBehaviourWrapper(DirectorObject behaviour)
		{
			this.behaviour = behaviour;
		}
	}
}