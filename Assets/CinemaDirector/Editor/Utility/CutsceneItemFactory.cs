using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    public class CutsceneItemFactory
    {
        private const string DIRECTOR_GROUP_NAME = "DirectorGroup";
        private const string HLW_ACTOR_GROUP_NAME = "EntiyGroup";
        private const string MULTI_ACTOR_GROUP_NAME = "Multi Actor Group";
        private const string SHOT_NAME_DEFAULT = "Shot";
        private const string SHOT_TRACK_LABEL = "ShotTrack";
        private const string AUDIO_TRACK_LABEL = "AudioTrack";
        private const string CURVE_TRACK_LABEL = "曲线轨道";
        private const string EVENT_TRACK_LABEL = "ActorTrack";
        private const string CURVE_CLIP_NAME_DEFAULT = "曲线片段";
        
        private const float DEFAULT_SHOT_LENGTH = 5f;
        private const float DEFAULT_GLOBAL_ACTION_LENGTH = 5f;
        private const float DEFAULT_ACTOR_ACTION_LENGTH = 5f;
        private const float DEFAULT_CURVE_LENGTH = 5f;

        /// <summary>
        /// Create a new Track Group.
        /// </summary>
        /// <param name="cutscene">The cutscene that this Track Group will be attached to.</param>
        /// <param name="type">The type of the new track group.</param>
        /// <param name="label">The name of the new track group.</param>
        /// <returns>The new track group. Reminder: Register an Undo.</returns>
        public static TrackGroup CreateTrackGroup(Cutscene cutscene, Type type, string label)
        {
            return DirectorObject.Create(type, cutscene, label) as TrackGroup;
        }

        /// <summary>
        /// Create a new Track.
        /// </summary>
        /// <param name="trackGroup">The track group that this track will be attached to.</param>
        /// <param name="type">The type of the new track.</param>
        /// <param name="label">The name of the new track.</param>
        /// <returns>The newly created track. Reminder: Register an Undo.</returns>
        internal static TimelineTrack CreateTimelineTrack(TrackGroup trackGroup, Type type, string label)
        {
            return DirectorObject.Create(type, trackGroup, label) as TimelineTrack;
        }

        internal static TimelineItem CreateCutsceneItem(TimelineTrack timelineTrack, Type type, string label,
            float firetime)
        {
            TimelineItem ti = DirectorObject.Create(type, timelineTrack, label) as TimelineItem;
            ti.name = label;
            ti.SetDefaults();

            if (type.IsSubclassOf(typeof(TimelineActionFixed)))
            {
                TimelineActionFixed newAction = ti as TimelineActionFixed;

                SortedDictionary<float, TimelineActionFixed> sortedClips =
                    new SortedDictionary<float, TimelineActionFixed>();
                foreach (var directorObject in timelineTrack.GetTimelineItems())
                {
                    TimelineActionFixed action = directorObject as TimelineActionFixed;
                    if (action == null)
                    {
                        continue;
                    }
                    sortedClips.Add(action.Firetime, action);
                }

                float lastedTime = firetime;
                float length = newAction.ItemLength;
                foreach (var sortedClipsValue in sortedClips.Values)
                {
                    if (!(lastedTime < sortedClipsValue.Firetime && lastedTime + length <= sortedClipsValue.Firetime))
                    {
                        lastedTime = sortedClipsValue.Firetime + sortedClipsValue.Duration;
                    }
                }

                newAction.Firetime = lastedTime;
            }
            else if (type.IsSubclassOf(typeof(TimelineAction)))
            {
                TimelineAction newAction = ti as TimelineAction;

                SortedDictionary<float, TimelineAction> sortedActions = new SortedDictionary<float, TimelineAction>();
                foreach (var directorObject in timelineTrack.GetTimelineItems())
                {
                    TimelineAction action = directorObject as TimelineAction;
                    if (action == null)
                    {
                        continue;
                    }
                    sortedActions.Add(action.Firetime, action);
                }

                float lastedTime = firetime;
                float length = newAction.Duration;
                foreach (var sortedActionsValue in sortedActions.Values)
                {
                    if (lastedTime >= sortedActionsValue.Firetime)
                    {
                        lastedTime = Mathf.Max(lastedTime, sortedActionsValue.Firetime + sortedActionsValue.Duration);
                    }
                    else
                    {
                        length = sortedActionsValue.Firetime - lastedTime;
                        break;
                    }
                }

                newAction.Firetime = lastedTime;
                newAction.Duration = length;
            }
            else
            {
                ti.Firetime = firetime;
            }

            timelineTrack.Cutscene.recache();
            return ti;
        }
            
        internal static CinemaActorClipCurve CreateActorClipCurve(CurveTrack track)
        {
            string name = DirectorHelper.getCutsceneItemName(track, CURVE_CLIP_NAME_DEFAULT, typeof(CinemaActorClipCurve));

            CinemaActorClipCurve clip = DirectorObject.Create<CinemaActorClipCurve>(track, name);

            SortedDictionary<float, CinemaActorClipCurve> sortedItems = new SortedDictionary<float, CinemaActorClipCurve>();
            foreach (CinemaActorClipCurve c in track.Children)
            {
                sortedItems.Add(c.Firetime, c);
            }

            float latestTime = 0;
            float length = DEFAULT_CURVE_LENGTH;
            foreach (CinemaActorClipCurve c in sortedItems.Values)
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
            return clip;
        }
    }
}