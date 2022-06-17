using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class CutsceneWrapper : UnityBehaviourWrapper
{
    private Dictionary<Behaviour, TrackGroupWrapper> TrackGroupMap;
    private float duration;
    private float runningTime;
    private bool isPlaying;

    public CutsceneWrapper(Behaviour behaviour) : base(behaviour)
    {
        this.TrackGroupMap = new Dictionary<Behaviour, TrackGroupWrapper>();
    }

    public void AddTrackGroup(Behaviour behaviour, TrackGroupWrapper wrapper)
    {
        this.TrackGroupMap.Add(behaviour, wrapper);
    }

    public bool ContainsTrackGroup(Behaviour behaviour, out TrackGroupWrapper trackGroupWrapper) => 
        this.TrackGroupMap.TryGetValue(behaviour, out trackGroupWrapper);

    public TrackGroupWrapper GetTrackGroupWrapper(Behaviour behaviour) => 
        this.TrackGroupMap[behaviour];

    public void RemoveTrackGroup(Behaviour behaviour)
    {
        this.TrackGroupMap.Remove(behaviour);
    }

    public float Duration
    {
        get => 
            this.duration;
        set => 
            (this.duration = value);
    }

    public float RunningTime
    {
        get => 
            this.runningTime;
        set => 
            (this.runningTime = value);
    }

    public bool IsPlaying
    {
        get => 
            this.isPlaying;
        set => 
            (this.isPlaying = value);
    }

    public IEnumerable<TrackGroupWrapper> TrackGroups =>
        this.TrackGroupMap.Values;

    public IEnumerable<Behaviour> Behaviours =>
        this.TrackGroupMap.Keys;
}

