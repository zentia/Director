using System;
using UnityEngine;

public class CinemaDirectorArgs : EventArgs
{
    public float timeArg;
    public Behaviour cutscene;

    public CinemaDirectorArgs(Behaviour cutscene)
    {
        this.cutscene = cutscene;
    }

    public CinemaDirectorArgs(Behaviour cutscene, float time)
    {
        this.cutscene = cutscene;
        this.timeArg = time;
    }
}

