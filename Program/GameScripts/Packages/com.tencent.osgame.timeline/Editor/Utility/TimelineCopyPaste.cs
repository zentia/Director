using UnityEngine;

namespace TimelineEditor
{
    public class CurvesContext
    {
        public readonly TimelineControlState state;
        public readonly int frameNumber;
        public readonly TimelineClipCurveWrapper wrapper;

        public CurvesContext(TimelineClipCurveWrapper wrapper, int frameNumber, TimelineControlState state)
        {
            this.wrapper = wrapper;
            this.frameNumber = frameNumber;
            this.state = state;
        }
    }


    public class TimelineCopyPaste
    {
        private static Behaviour clipboard;
        public static CurvesContext copyFrameContext;
        public static bool copyKeyFrames = false;
        public static short pasteFrame;
        public static bool pasteKeyFrames = false;
        public static TimelineTrackControl TimelineTrackControl = null;

        public static bool NextKey = false;
        public static bool LastKey = false;
        public static float CurrentRunningTime = 0;
        public static TimelineCurveClipItemControl focusedControl = null;
        public static void Copy(Behaviour obj)
        {
            clipboard = obj;
        }

        public static GameObject Paste(Transform parent)
        {
            GameObject obj2 = null;
            if (clipboard != null)
            {
                obj2 = Object.Instantiate(clipboard.gameObject, parent, true);
                obj2.name = TimelineControlHelper.GetNameForDuplicate(clipboard, clipboard.name);
            }
            return obj2;
        }

        public static Behaviour Peek() => clipboard;
    }
}
