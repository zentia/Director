using UnityEngine;

public class UnityBehaviourWrapper
{
	private Behaviour behaviour;

	private bool hasChanged = true;

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

	public bool HasChanged
	{
		get
		{
			return hasChanged;
		}
		set
		{
			hasChanged = value;
		}
	}

	public UnityBehaviourWrapper(Behaviour behaviour)
	{
		this.behaviour = behaviour;
	}
}
