using UnityEngine;
using TimelineRuntime;

namespace TimelineEditor
{
    [TimelineItemControl(typeof(TimelineActorAction))]
    public class TimelineActorActionControl : TimelineActionControl
    {
        public override void Draw(TimelineControlState state)
        {
            var action = Wrapper.timelineItem as TimelineActorAction;
            if (action == null) 
                return;
            GUI.Box(ControlPosition, GUIContent.none, IsSelected ? TimelineTrackControl.styles.ActorTrackItemSelectedStyle : TimelineTrackControl.styles.ActorTrackItemStyle);
            DrawRenameLabel(action.name, ControlPosition);
        }
    }
}
