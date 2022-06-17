using System;
using UnityEngine;

public class CinemaActionWrapper : TimelineItemWrapper
{
    private float duration;

    public CinemaActionWrapper(Behaviour behaviour, float firetime, float duration) : base(behaviour, firetime)
    {
        this.duration = duration;
    }

    public float Duration
    {
        get => 
            this.duration;
        set => 
            (this.duration = value);
    }
}

