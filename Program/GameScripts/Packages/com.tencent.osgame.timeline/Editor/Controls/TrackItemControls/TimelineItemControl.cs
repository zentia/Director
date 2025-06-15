using TimelineRuntime;
using UnityEditor;

namespace TimelineEditor
{
    [TimelineItemControl(typeof(TimelineItem), 1)]
    public class TimelineItemControl : TrackItemControl
    {
        public TimelineItemControl()
        {
            AlterTrackItem += OnAlterTrackItem;
        }

        void OnAlterTrackItem(object sender, TrackItemEventArgs e)
        {
            TimelineItem item = e.item as TimelineItem;
            if (item == null) return;

            Undo.RecordObject(e.item, string.Format("Change {0}", item.name));
            item.Firetime = e.FireTime;
        }
    }
}
