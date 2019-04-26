using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace DirectorEditor
{
	public abstract class DirectorBehaviourControl
	{
		private Behaviour behaviour;

		[method: CompilerGenerated]
		[CompilerGenerated]
		public event DirectorBehaviourControlHandler DeleteRequest;

		public Behaviour Behaviour
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

		public bool IsSelected
		{
			get
			{
				return Behaviour != null && Selection.Contains(Behaviour.gameObject);
			}
		}

		internal void Delete()
		{
			Undo.DestroyObjectImmediate(Behaviour.gameObject);
		}

		public void RequestDelete()
		{
			if (DeleteRequest != null)
			{
				DeleteRequest(this, new DirectorBehaviourControlEventArgs(this.behaviour, this));
			}
		}

		public void RequestDelete(DirectorBehaviourControlEventArgs args)
		{
			if (this.DeleteRequest != null)
			{
				this.DeleteRequest(this, args);
			}
		}
	}
}
