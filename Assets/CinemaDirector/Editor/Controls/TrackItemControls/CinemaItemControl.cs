﻿using UnityEditor;

namespace CinemaDirector
{
    [CutsceneItemControl(typeof(TimelineItem), 1)]
    public class CinemaItemControl : TrackItemControl
    {
        public CinemaItemControl()
        {
            AlterTrackItem += CinemaItemControl_AlterTrackItem;
        }

        void CinemaItemControl_AlterTrackItem(object sender, TrackItemEventArgs e)
        {
            TimelineItem item = e.item as TimelineItem;
            if (item == null) return;

            Undo.RecordObject(e.item, string.Format("Change {0}", item.name));
            item.Firetime = e.firetime;
        }
    }
}