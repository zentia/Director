using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void TrackItemEventHandler(object sender, TrackItemEventArgs e);
    
    public class TrackItemEventArgs : EventArgs
    {
        public float FireTime;
        public Behaviour item;

        public TrackItemEventArgs(Behaviour item, float fireTime)
        {
            this.item = item;
            FireTime = fireTime;
        }
    }
}