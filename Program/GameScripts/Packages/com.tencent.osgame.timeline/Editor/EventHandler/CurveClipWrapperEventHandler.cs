using System;

namespace TimelineEditor
{
    public delegate void CurveClipWrapperEventHandler(object sender, CurveClipWrapperEventArgs e);
    public class CurveClipWrapperEventArgs : EventArgs
    {
        public TimelineClipCurveWrapper wrapper;

        public CurveClipWrapperEventArgs(TimelineClipCurveWrapper wrapper)
        {
            this.wrapper = wrapper;
        }
    }

}
