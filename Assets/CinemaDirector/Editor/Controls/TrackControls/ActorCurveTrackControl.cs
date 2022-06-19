using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    [CutsceneTrack(typeof(CurveTrack))]
    public class ActorCurveTrackControl : CinemaCurveTrackControl
    {
        public override void Initialize()
        {
            base.Initialize();
            isExpanded = true;
        }

        protected override void updateHeaderControl3(Rect position)
        {
            CurveTrack track = TargetTrack.Behaviour as CurveTrack;
            if (track == null)
            {
                return;
            }

            Color temp = GUI.color;
            GUI.color = (track.GetTimelineItems().Count > 0) ? Color.green : Color.red;

            if (GUI.Button(position, string.Empty, TrackGroupControl.styles.addIcon))
            {
                
            }
            GUI.color = temp;
        }

        protected override void updateHeaderControl4(Rect position)
        {
            if (!state.IsInCurveMode)
            {
                base.updateHeaderControl4(position);
                return;
            }
            CurveTrack track = TargetTrack.Behaviour as CurveTrack;
            if (track == null) return;

            Color temp = GUI.color;
            GUI.color = (track.Children.Count > 0) ? Color.green : Color.red;

            if (GUI.Button(position, string.Empty, TrackGroupControl.styles.addIcon))
            {
                addNewCurveItem(track);
            }
            GUI.color = temp;
        }

        private void addNewCurveItem(CurveTrack track)
        {
            Undo.RegisterCreatedObjectUndo(CutsceneItemFactory.CreateActorClipCurve(track), "Created Actor Clip Curve");
        }

        protected override void showBodyContextMenu(Event evt)
        {
            if (!state.IsInCurveMode)
            {
                base.showBodyContextMenu(evt);
                return;
            }
            CurveTrack itemTrack = TargetTrack.Behaviour as CurveTrack;
            if (itemTrack == null) return;

            var b = DirectorCopyPaste.Peek();

            PasteContext pasteContext = new PasteContext(evt.mousePosition, itemTrack);
            GenericMenu createMenu = new GenericMenu();
            if (b != null && DirectorHelper.IsTrackItemValidForTrack(b, itemTrack))
            {
                createMenu.AddItem(new GUIContent("Paste"), false, pasteItem, pasteContext);
            }
            else
            {
                createMenu.AddDisabledItem(new GUIContent("Paste"));
            }
            createMenu.ShowAsContext();
        }

        private void pasteItem(object userData)
        {
            PasteContext data = userData as PasteContext;
            if (data != null)
            {
                float firetime = (data.mousePosition.x - state.Translation.x) / state.Scale.x;
                var clone = DirectorCopyPaste.Paste(data.track);

                CinemaClipCurve clipCurve = clone as CinemaClipCurve;
                clipCurve.TranslateCurves(firetime - clipCurve.Firetime);

                Undo.RegisterCreatedObjectUndo(clone, "Pasted " + clone.name);
            }
        }

        protected override void Record()
        {
            if (!state.IsInPreviewMode)
                return;
            var time = state.ScrubberPosition - state.TickDistance / 5 * 2;
            foreach (var control in Controls)
            {
                var keyframe = control.Behaviour as TimelineItem;
                if (keyframe.ContainsTime(time))
                    return;
            }
            var itemTrack = TargetTrack.Behaviour as TimelineTrack;
            if (itemTrack == null)
                return;
            AddKeyframe(time);
        }

        private class PasteContext
        {
            public Vector2 mousePosition;
            public CurveTrack track;

            public PasteContext(Vector2 mousePosition, CurveTrack track)
            {
                this.mousePosition = mousePosition;
                this.track = track;
            }
        }
    }
    
}
