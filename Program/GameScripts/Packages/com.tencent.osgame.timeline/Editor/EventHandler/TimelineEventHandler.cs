using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void TimelineEventHandler(object sender, TimelineArgs e);
    
    public class TimelineArgs : EventArgs
    {
        public float timeArg;
        public Behaviour behaviour;

        public TimelineArgs(Behaviour behaviour)
        {
            this.behaviour = behaviour;
        }

        public TimelineArgs(Behaviour behaviour, float time)
        {
            this.behaviour = behaviour;
            timeArg = time;
        }
    }
} 