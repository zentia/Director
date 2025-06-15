using UnityEditor;
using TimelineRuntime;

namespace TimelineEditor
{
    [TimelineItemControl(typeof(TimelineAction))]
    public class TimelineActionControl : ActionItemControl
    {
        public TimelineActionControl()
        {
            AlterAction += OnAlterAction;
        }

        private void OnAlterAction(object sender, ActionItemEventArgs e)
        {
            TimelineAction action = e.actionItem as TimelineAction;
            if (action == null) return;

            if (e.duration <= 0)
            {
                DeleteItem(e.actionItem);
            }
            else
            {
                Undo.RecordObject(e.actionItem, string.Format("Change {0}", action.name));
                action.Firetime = e.firetime;
                action.Duration = e.duration;
            }
        }
    }
}
