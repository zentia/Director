using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class TimelineTrackWrapper : UnityBehaviourWrapper
{
    public bool IsLocked;
    private int ordinal;
    private Dictionary<Behaviour, TimelineItemWrapper> itemMap;

    public TimelineTrackWrapper(Behaviour behaviour) : base(behaviour)
    {
        this.itemMap = new Dictionary<Behaviour, TimelineItemWrapper>();
    }

    public void AddItem(Behaviour behaviour, TimelineItemWrapper wrapper)
    {
        this.itemMap.Add(behaviour, wrapper);
    }

    public bool ContainsItem(Behaviour behaviour, out TimelineItemWrapper itemWrapper) => 
        this.itemMap.TryGetValue(behaviour, out itemWrapper);

    public TimelineItemWrapper GetItemWrapper(Behaviour behaviour) => 
        this.itemMap[behaviour];

    public void RemoveItem(Behaviour behaviour)
    {
        this.itemMap.Remove(behaviour);
    }

    public IEnumerable<TimelineItemWrapper> Items =>
        this.itemMap.Values;

    public IEnumerable<Behaviour> Behaviours =>
        this.itemMap.Keys;

    public int Ordinal
    {
        get => 
            this.ordinal;
        set => 
            this.ordinal = value;
    }
}

