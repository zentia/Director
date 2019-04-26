using System;
using System.Collections.Generic;
using UnityEngine;

public class TimelineTrackWrapper : UnityBehaviourWrapper
{
	private int ordinal;

	private Dictionary<Behaviour, TimelineItemWrapper> itemMap = new Dictionary<Behaviour, TimelineItemWrapper>();

	public IEnumerable<TimelineItemWrapper> Items
	{
		get
		{
			return itemMap.Values;
		}
	}

	public IEnumerable<Behaviour> Behaviours
	{
		get
		{
			return itemMap.Keys;
		}
	}

	public int Ordinal
	{
		get
		{
			return ordinal;
		}
		set
		{
			ordinal = value;
		}
	}

	public TimelineTrackWrapper(Behaviour behaviour) : base(behaviour)
	{
	}

	public void AddItem(Behaviour behaviour, TimelineItemWrapper wrapper)
	{
		itemMap.Add(behaviour, wrapper);
	}

	public TimelineItemWrapper GetItemWrapper(Behaviour behaviour)
	{
		return itemMap[behaviour];
	}

	public void RemoveItem(Behaviour behaviour)
	{
		itemMap.Remove(behaviour);
	}

	public bool ContainsItem(Behaviour behaviour, out TimelineItemWrapper itemWrapper)
	{
		return itemMap.TryGetValue(behaviour, out itemWrapper);
	}
}
