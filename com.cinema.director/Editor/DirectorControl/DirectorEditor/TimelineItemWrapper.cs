using System;
using UnityEngine;

public class TimelineItemWrapper
{
    protected float firetime;
    private UnityEngine.Behaviour behaviour;

    public TimelineItemWrapper(UnityEngine.Behaviour behaviour, float firetime)
    {
        this.behaviour = behaviour;
        this.firetime = firetime;
    }

    public UnityEngine.Behaviour Behaviour =>
        this.behaviour;

    public float Firetime
    {
        get => 
            this.firetime;
        set => 
            this.firetime = value;
    }
}

