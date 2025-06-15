using TimelineRuntime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineFactory
    {
        private const string CURVE_NAME_DEFAULT = "Curve";

        public static TrackGroup CreateTrackGroup(Timeline timeline, Type type, string label)
        {
            var trackGroup = new GameObject(label, type).GetComponent<TrackGroup>();
            trackGroup.transform.SetParent(timeline.transform, true);
            trackGroup.OnValidate();
            timeline.GetComponentsInChildren<TrackGroup>(true, timeline.trackGroups);
            return trackGroup;
        }

        internal static TimelineTrack CreateTimelineTrack(TrackGroup trackGroup, Type type, string label)
        {
            var timelineTrack = new GameObject(label, type).GetComponent<TimelineTrack>();
            timelineTrack.transform.SetParent(trackGroup.transform, true);
            timelineTrack.OnValidate();
            trackGroup.GetComponentsInChildren(true, trackGroup.timelineTracks);
            timelineTrack.timeline = trackGroup.timeline;
            return timelineTrack;
        }

        internal static TimelineItem CreateTimelineItem(TimelineTrack timelineTrack, Type type, string label, int frame)
        {
            var gameObject = new GameObject(label, type);
            var ti = gameObject.GetComponent<TimelineItem>();
            ti.timelineTrack = timelineTrack;
            ti.timeline = timelineTrack.timeline;
            gameObject.transform.SetParent(timelineTrack.transform, true);
            if (type.IsSubclassOf(typeof(TimelineActionFixed)))
            {
                var newAction = ti as TimelineActionFixed;

                var sortedClips = new SortedDictionary<float, TimelineActionFixed>();
                foreach (var current in timelineTrack.timelineItems)
                {
                    var action = current as TimelineActionFixed;
                    if (action == null) continue;
                    sortedClips.Add(action.Firetime, action);
                }

                float latestTime = TimelineUtility.FrameToTime(frame);
                float length = newAction.ItemLength;
                foreach (TimelineActionFixed a in sortedClips.Values)
                {
                    if (!(latestTime < a.Firetime && latestTime + length <= a.Firetime))
                    {
                        latestTime = a.Firetime + a.Duration;
                    }
                }

                newAction.Firetime = latestTime;
            }
            else if (type.IsSubclassOf(typeof(TimelineAction)))
            {
                TimelineAction newAction = ti as TimelineAction;

                SortedDictionary<float, TimelineAction> sortedActions = new SortedDictionary<float, TimelineAction>();
                foreach (TimelineItem current in timelineTrack.timelineItems)
                {
                    TimelineAction action = current as TimelineAction;
                    if (action == null) continue;
                    sortedActions.Add(action.Firetime, action);
                }

                float latestTime = TimelineUtility.FrameToTime(frame);
                float length = newAction.Duration;
                foreach (TimelineAction a in sortedActions.Values)
                {
                    if (latestTime >= a.Firetime)
                    {
                        latestTime = Mathf.Max(latestTime, a.Firetime + a.Duration);
                    }
                    else
                    {
                        length = a.Firetime - latestTime;
                        break;
                    }
                }

                newAction.Firetime = latestTime;
                newAction.Duration = length;
            }
            else
            {
                ti.Firetime = TimelineUtility.FrameToTime(frame);
            }
            timelineTrack.GetComponentsInChildren(true, timelineTrack.timelineItems);
            timelineTrack.timeline.Recache();
            ti.SetDefaults();
            return ti;
        }

        internal static TimelineItem CreateTimelineItem(TimelineTrack timelineTrack, Type type, UnityEngine.Object pairedObject, float fireTime)
        {
            var gameObject = new GameObject(pairedObject.name, type);

            var ti = gameObject.GetComponent<TimelineItem>();
            ti.SetDefaults(pairedObject);

            if (type.IsSubclassOf(typeof(TimelineActionFixed)))
            {
                var newAction = ti as TimelineActionFixed;

                var sortedClips = new SortedDictionary<float, TimelineActionFixed>();
                foreach (TimelineItem current in timelineTrack.timelineItems)
                {
                    TimelineActionFixed action = current as TimelineActionFixed;
                    if (action == null) continue;
                    sortedClips.Add(action.Firetime, action);
                }

                float latestTime = fireTime;
                float length = newAction.ItemLength;
                foreach (TimelineActionFixed a in sortedClips.Values)
                {
                    if (!(latestTime < a.Firetime && latestTime + length <= a.Firetime))
                    {
                        latestTime = Mathf.Max(a.Firetime + a.Duration, latestTime);
                    }
                }

                newAction.Firetime = latestTime;
            }
            else if (type.IsSubclassOf(typeof(TimelineAction)))
            {
                TimelineAction newAction = ti as TimelineAction;

                SortedDictionary<float, TimelineAction> sortedActions = new SortedDictionary<float, TimelineAction>();
                foreach (TimelineItem current in timelineTrack.timelineItems)
                {
                    TimelineAction action = current as TimelineAction;
                    if (action == null) continue;
                    sortedActions.Add(action.Firetime, action);
                }

                float latestTime = fireTime;
                float length = newAction.Duration;
                foreach (TimelineAction a in sortedActions.Values)
                {
                    if (latestTime >= a.Firetime)
                    {
                        latestTime = Mathf.Max(latestTime, a.Firetime + a.Duration);
                    }
                    else
                    {
                        length = a.Firetime - latestTime;
                        break;
                    }
                }

                newAction.Firetime = latestTime;
                newAction.Duration = length;
            }
            else
            {
                ti.Firetime = fireTime;
            }

            gameObject.transform.SetParent(timelineTrack.transform, true);
            timelineTrack.GetComponentsInChildren<TimelineItem>(true, timelineTrack.timelineItems);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            timelineTrack.timeline.Recache();
            return ti;
        }

        internal static T CreateActorClipCurve<T>(TimelineTrack track, float duration) where T : TimelineAction
        {
            string name = TimelineHelper.GetTimelineItemName(track.gameObject, CURVE_NAME_DEFAULT, typeof(T));
            var item = new GameObject(name);
            T clip = item.AddComponent<T>();
            clip.timelineTrack = track;
            item.transform.SetParent(track.transform, true);
            track.OnValidate();
            float latestTime = 0;
            float length = duration;
            foreach (T c in track.timelineItems)
            {
                if (latestTime >= c.Firetime)
                {
                    latestTime = Mathf.Max(latestTime, c.Firetime + c.Duration);
                }
                else
                {
                    length = c.Firetime - latestTime;
                    break;
                }
            }
            clip.Firetime = latestTime;
            clip.Duration = length;
            clip.timeline = track.timeline;
            return clip;
        }
    }
}
