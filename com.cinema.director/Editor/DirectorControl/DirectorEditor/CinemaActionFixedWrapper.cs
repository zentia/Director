﻿using System;
using UnityEngine;

public class CinemaActionFixedWrapper : CinemaActionWrapper
{
    private float inTime;
    private float outTime;
    private float itemLength;

    public CinemaActionFixedWrapper(Behaviour behaviour, float firetime, float duration, float inTime, float outTime, float itemLength) : base(behaviour, firetime, duration)
    {
        this.inTime = inTime;
        this.outTime = outTime;
        this.itemLength = itemLength;
    }

    public float InTime
    {
        get => 
            this.inTime;
        set
        {
            this.inTime = value;
            base.Duration = this.outTime - this.inTime;
        }
    }

    public float OutTime
    {
        get => 
            this.outTime;
        set
        {
            this.outTime = value;
            base.Duration = this.outTime - this.inTime;
        }
    }

    public float ItemLength
    {
        get => 
            this.itemLength;
        set => 
            this.itemLength = value;
    }
}

