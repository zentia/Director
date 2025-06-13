using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TimelineRuntime;

namespace TimelineEditor
{
    [TimelineItemControl(typeof(TimelineCurveClip))]
    public class TimelineCurveClipControl : TimelineCurveClipItemControl
    {
        protected bool hasUndoRedoBeenPerformed = false;

        public TimelineCurveClipControl()
        {
            TranslateCurveClipItem += OnTranslateCurveClipItem;
            SnapScrubberEvent += OnControlSnapScrubber;
            CurvesChanged += OnCurvesChanged;

            actionIcon = Resources.Load("Director_CurvesIcon.png") as Texture;
        }

        protected override void ShowContextMenu(Behaviour behaviour)
        {
            var createMenu = new GenericMenu();
            createMenu.AddItem(new GUIContent("Rename"), false, RenameItem, behaviour);
            createMenu.AddItem(new GUIContent("Copy"), false, CopyItem, behaviour);
            createMenu.AddItem(new GUIContent("Delete"), false, DeleteItem, behaviour);
            createMenu.ShowAsContext();
        }

        protected virtual void OnCurvesChanged(object sender, CurveClipWrapperEventArgs e)
        {
            if (e.wrapper == null)
                return;
            TimelineClipCurveWrapper wrapper = e.wrapper;
            var curveClip = wrapper.timelineItem as TimelineCurveClip;
            if (curveClip == null)
                return;
            Undo.RecordObject(curveClip, string.Format("Changed {0}", curveClip.name));
            for (int i = 0; i < curveClip.CurveData.Count; i++)
            {
                var memberCurve = curveClip.CurveData[i];
                TimelineMemberCurveWrapper memberWrapper;
                if (wrapper.TryGetValue(memberCurve.Type, memberCurve.PropertyName, out memberWrapper))
                {
                    int showingCurves = UnityPropertyTypeInfo.GetCurveCount(memberCurve.PropertyType);

                    for (int j = 0; j < showingCurves; j++)
                    {
                        memberCurve.SetCurve(j, new AnimationCurve(memberWrapper.AnimationCurves[j].Curve.keys));
                    }
                }
            }
            curveClip.Firetime = wrapper.fireTime;
            curveClip.Duration = wrapper.Duration;
            EditorUtility.SetDirty(curveClip);
        }

        private void OnControlSnapScrubber(object sender, CurveClipScrubberEventArgs e)
        {
            if (!timelineControl.InPreviewMode)
            {
                timelineControl.InPreviewMode = true;
            }
            var curveClip = e.curveClipItem as TimelineCurveClip;
            if (curveClip == null)
                return;
            curveClip.timeline.EnterPreviewMode(e.time);
            curveClip.timeline.SetRunningTime(e.time);
        }

        void OnTranslateCurveClipItem(object sender, CurveClipItemEventArgs e)
        {
            var curveClip = e.curveClipItem as TimelineCurveClip;
            if (curveClip == null)
                return;
            Undo.RecordObject(e.curveClipItem, string.Format("Changed {0}", curveClip.name));
            curveClip.TranslateCurves(e.firetime - curveClip.Firetime);
            EditorUtility.SetDirty(e.curveClipItem);
        }
    }
}
