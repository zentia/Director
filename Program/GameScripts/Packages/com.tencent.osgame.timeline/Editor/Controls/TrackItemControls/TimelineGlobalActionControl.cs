using UnityEngine;
using TimelineRuntime;

namespace TimelineEditor
{
    [TimelineItemControl(typeof(TimelineGlobalAction))]
    public class TimelineGlobalActionControl : TimelineActionControl
    {
        public override void Draw(TimelineControlState state)
        {
            var action = Wrapper.timelineItem as TimelineGlobalAction;
            if (action == null) return;

            if (IsSelected)
            {
                GUI.Box(ControlPosition, GUIContent.none, TimelineTrackControl.styles.GlobalTrackItemSelectedStyle);
            }
            else
            {
                GUI.Box(ControlPosition, GUIContent.none, TimelineTrackControl.styles.GlobalTrackItemStyle);
            }

            DrawRenameLabel(action.name, ControlPosition);
        }
    }
}
