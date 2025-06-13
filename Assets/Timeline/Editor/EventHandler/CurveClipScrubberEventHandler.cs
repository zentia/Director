using System;
using UnityEngine;

namespace TimelineEditor
{
    public delegate void CurveClipScrubberEventHandler(object sender, CurveClipScrubberEventArgs e);
    public class CurveClipScrubberEventArgs : EventArgs
    {
        public Behaviour curveClipItem;
        public float time;

        public CurveClipScrubberEventArgs(Behaviour curveClipItem, float time)
        {
            this.curveClipItem = curveClipItem;
            this.time = time;
        }
    }
}