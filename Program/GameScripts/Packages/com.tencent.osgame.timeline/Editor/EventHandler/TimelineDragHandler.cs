using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void TimelineDragHandler(object sender, TimelineDragArgs e);
    
    public class TimelineDragArgs : EventArgs
    {
        public Behaviour behaviour;
        public UnityEngine.Object[] references;

        public TimelineDragArgs(Behaviour behaviour, UnityEngine.Object[] references)
        {
            this.behaviour = behaviour;
            this.references = references;
        }
    }
}