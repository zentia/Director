using System;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineBehaviourControlEventArgs : EventArgs
    {
        public Behaviour Behaviour;
        public TimelineBehaviourControl Control;

        public TimelineBehaviourControlEventArgs(Behaviour behaviour, TimelineBehaviourControl control)
        {
            Behaviour = behaviour;
            Control = control;
        }
    }
}

