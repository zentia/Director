using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemControl(typeof(TimelineAction))]
    public class TimelineActionControl : ActionItemControl
    {
        public TimelineActionControl()
        {
            AlterAction += TimelineActionControl_AlterAction;
        }

        void TimelineActionControl_AlterAction(object sender, ActionItemEventArgs e)
        {
            TimelineAction action = e.actionItem as TimelineAction;
            if (action == null) return;

            if (e.duration <= 0)
            {
                deleteItem(e.actionItem);
            }
            else
            {
                Undo.RecordObject(e.actionItem, string.Format("Change {0}", action.name));
                action.Firetime = e.firetime;
                action.Duration = e.duration;
            }
        }
        
        public override void Draw(DirectorControlState state)
        {
            TimelineAction action = Wrapper.TimelineItem as TimelineAction;
            if (action == null) return;

            if (IsSelected)
            {
                GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.ActorTrackItemSelectedStyle);
            }
            else
            {
                GUI.Box(controlPosition, GUIContent.none, TimelineTrackControl.styles.ActorTrackItemStyle);
            }

            DrawRenameLabel(action.name, controlPosition);
        }
    }
}