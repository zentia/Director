using UnityEditor;
using UnityEngine;
using TimelineRuntime;

namespace TimelineEditor
{
    [TimelineTrack(typeof(TimelineCurveTrack))]
    public class ActorCurveTrackControl : TimelineCurveTrackControl
    {
        public override void Initialize()
        {
            base.Initialize();
            isExpanded = true;
        }

        protected override void UpdateHeaderControl(Rect position)
        {
            var track = Wrapper.Track;
            if (track == null || track.timelineItems.Count > 0)
                return;
            var temp = GUI.color;
            GUI.color = Color.red;
            if (GUI.Button(position, string.Empty, TimelineTrackGroupControl.styles.AddIcon))
            {
                AddNewCurveItem(track, timelineControl.Wrapper.Duration);
            }
            GUI.color = temp;
        }

        protected virtual void AddNewCurveItem(TimelineTrack t, float duration)
        {
            Undo.RegisterCreatedObjectUndo(TimelineFactory.CreateActorClipCurve<TimelineActorCurveClip>(t, duration), "Created Actor Curve");
        }

        protected override void ShowBodyContextMenu(Event evt, TimelineControlState state = null)
        {
            TimelineCurveTrack itemTrack = Wrapper.Track as TimelineCurveTrack;
            if (itemTrack == null) return;

            Behaviour b = TimelineCopyPaste.Peek();

            PasteContext pasteContext = new PasteContext(evt.mousePosition, itemTrack);
            GenericMenu createMenu = new GenericMenu();
            if (b != null && TimelineHelper.IsTrackItemValidForTrack(b, itemTrack))
            {
                createMenu.AddItem(new GUIContent("Paste"), false, pasteItem, pasteContext);
            }
            else
            {
                if (TimelineCopyPaste.copyKeyFrames)
                {
                    float time = (Event.current.mousePosition.x - state.Translation.x) / state.Scale.x;
                    createMenu.AddItem(new GUIContent("Paste KeyFrames"), false, SetTime, time);
                }
                createMenu.AddDisabledItem(new GUIContent("Paste"));
            }
            createMenu.ShowAsContext();
        }

        private void SetTime(object time)
        {
            float realTime = (float)time;
            TimelineCopyPaste.pasteFrame = TimelineUtility.TimeToFrame(realTime);
            TimelineCopyPaste.copyKeyFrames = false;
            TimelineCopyPaste.pasteKeyFrames = true;
            TimelineCopyPaste.TimelineTrackControl = this;
        }

        private void pasteItem(object userData)
        {
            PasteContext data = userData as PasteContext;
            if (data != null)
            {
                float firetime = (data.mousePosition.x - state.Translation.x) / state.Scale.x;
                GameObject clone = TimelineCopyPaste.Paste(data.track.transform);

                TimelineCurveClip curveClip = clone.GetComponent<TimelineCurveClip>();
                curveClip.TranslateCurves(firetime - curveClip.Firetime);

                Undo.RegisterCreatedObjectUndo(clone, "Pasted " + clone.name);
            }
        }

        private class PasteContext
        {
            public Vector2 mousePosition;
            public TimelineCurveTrack track;

            public PasteContext(Vector2 mousePosition, TimelineCurveTrack track)
            {
                this.mousePosition = mousePosition;
                this.track = track;
            }
        }
    }
}
