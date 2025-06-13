using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void ActionFixedItemEventHandler(object sender, ActionFixedItemEventArgs e);
    public class ActionFixedItemEventArgs : EventArgs
    {
        public Behaviour actionItem;
        public float firetime;
        public float duration;
        public float inTime;
        public float outTime;
    
        public ActionFixedItemEventArgs(Behaviour actionItem, float firetime, float duration, float inTime, float outTime)
        {
            this.actionItem = actionItem;
            this.firetime = firetime;
            this.duration = duration;
            this.inTime = inTime;
            this.outTime = outTime;
        }
    }

}