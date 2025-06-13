using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void ActionItemEventHandler(object sender, ActionItemEventArgs e);
    public class ActionItemEventArgs : EventArgs
    {
        public Behaviour actionItem;
        public float firetime;
        public float duration;

        public ActionItemEventArgs(Behaviour actionItem, float firetime, float duration)
        {
            this.actionItem = actionItem;
            this.firetime = firetime;
            this.duration = duration;
        }
    }
}