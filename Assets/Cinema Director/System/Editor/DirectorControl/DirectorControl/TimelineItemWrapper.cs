using System;
using UnityEngine;

public class TimelineItemWrapper
{
	protected float firetime;

	private Behaviour behaviour;

	public Behaviour Behaviour
	{
		get
		{
			return behaviour;
		}
	}

	public float Firetime
	{
		get
		{
			return firetime;
		}
		set
		{
			firetime = value;
		}
	}

	public TimelineItemWrapper(Behaviour behaviour, float firetime)
	{
		this.behaviour = behaviour;
		this.firetime = firetime;
	}
}
