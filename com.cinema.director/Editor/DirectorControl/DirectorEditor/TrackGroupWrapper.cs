using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class TrackGroupWrapper : UnityBehaviourWrapper
{
    private int ordinal;
    private Dictionary<Behaviour, TimelineTrackWrapper> trackMap;

    public TrackGroupWrapper(Behaviour behaviour) : base(behaviour)
    {
        this.trackMap = new Dictionary<Behaviour, TimelineTrackWrapper>();
    }

    public void AddTrack(Behaviour behaviour, TimelineTrackWrapper wrapper)
    {
        this.trackMap.Add(behaviour, wrapper);
    }

    public bool ContainsTrack(Behaviour behaviour, out TimelineTrackWrapper trackWrapper) => 
        this.trackMap.TryGetValue(behaviour, out trackWrapper);

    public TimelineTrackWrapper GetTrackWrapper(Behaviour behaviour) => 
        this.trackMap[behaviour];

    public void RemoveTrack(Behaviour behaviour)
    {
        this.trackMap.Remove(behaviour);
    }

    public IEnumerable<TimelineTrackWrapper> Tracks =>
        this.trackMap.Values;

    public IEnumerable<Behaviour> Behaviours =>
        this.trackMap.Keys;

    public int Ordinal
    {
        get => 
            this.ordinal;
        set => 
            this.ordinal = value;
    }
}

