using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    public class TimelineTrack : MonoBehaviour
    {
        [SerializeField]
        private int ordinal = -1;
        [NonSerialized]
        public float elapsedTime;

        protected List<Type> allowedItemTypes;

        public virtual void Initialize()
        {
            elapsedTime = 0f;
            foreach (var timelineItem in timelineItems)
            {
                timelineItem.timelineTrack = this;
                timelineItem.timeline = timeline;
                timelineItem.Initialize();
            }
        }

        public virtual void ScrubToTime(float time)
        {
            foreach (var timelineItem in timelineItems)
            {
                timelineItem.ScrubToTime(time);
            }
        }

        public virtual void UpdateTrack(float runningTime, float deltaTime)
        {
            float previousTime = elapsedTime;
            elapsedTime = runningTime;

            for (int i = 0; i < timelineItems.Count; i++)
            {
                TimelineGlobalEvent timelineEvent = timelineItems[i] as TimelineGlobalEvent;
                if (timelineEvent == null)
                {
                    TimelineGlobalAction action = timelineItems[i] as TimelineGlobalAction;
                    if (action == null)
                        continue;
                    if (((previousTime < action.Firetime || previousTime <= 0f) && elapsedTime >= action.Firetime) && elapsedTime < action.EndTime)
                    {
                        action.Trigger();
                    }
                    else if ((previousTime <= action.EndTime) && (elapsedTime >= action.EndTime))
                    {
                        action.End();
                    }
                    else if (previousTime > action.Firetime && previousTime <= action.EndTime && elapsedTime <= action.Firetime)
                    {
                        action.ReverseTrigger();
                    }
                    else if ((previousTime > action.EndTime || previousTime >= timeline.Duration) && (elapsedTime > action.Firetime) && (elapsedTime <= action.EndTime))
                    {
                        action.ReverseEnd();
                    }
                    else if ((elapsedTime > action.Firetime) && (elapsedTime < action.EndTime))
                    {
                        float t = runningTime - action.Firetime;
                        action.UpdateTime(t, deltaTime);
                    }
                    continue;
                }

                if ((previousTime < timelineEvent.Firetime || previousTime <= 0f) && elapsedTime >= timelineEvent.Firetime)
                {
                    timelineEvent.Trigger();
                }
                else if (previousTime >= timelineEvent.Firetime && elapsedTime <= timelineEvent.Firetime)
                {
                    timelineEvent.Reverse();
                }
            }
        }

        public virtual void Pause() { }

        public virtual void Resume() { }

        public virtual void SetTime(float time)
        {
            float previousTime = elapsedTime;
            elapsedTime = time;

            for (int i = 0; i < timelineItems.Count; i++)
            {
                TimelineGlobalEvent timelineEvent = timelineItems[i] as TimelineGlobalEvent;
                if (timelineEvent != null)
                {
                    if ((previousTime < timelineEvent.Firetime && time >= timelineEvent.Firetime) || (timelineEvent.Firetime == 0f && previousTime <= timelineEvent.Firetime && time > timelineEvent.Firetime))
                    {
                        timelineEvent.Trigger();
                    }
                    else if (previousTime > timelineEvent.Firetime && elapsedTime <= timelineEvent.Firetime)
                    {
                        timelineEvent.Reverse();
                    }
                }

                TimelineGlobalAction action = timelineItems[i] as TimelineGlobalAction;
                action?.SetTime((time - action.Firetime), time - previousTime);
            }
        }

        public virtual List<float> GetMilestones(float from, float to)
        {
            bool isReverse = from > to;

            List<float> times = new List<float>();
            for (int i = 0; i < timelineItems.Count; i++)
            {
                if ((!isReverse && from < timelineItems[i].Firetime && to >= timelineItems[i].Firetime) || (isReverse && from > timelineItems[i].Firetime && to <= timelineItems[i].Firetime))
                {
                    if (!times.Contains(timelineItems[i].Firetime))
                    {
                        times.Add(timelineItems[i].Firetime);
                    }
                }

                if (timelineItems[i] is TimelineAction)
                {
                    float endTime = (timelineItems[i] as TimelineAction).EndTime;
                    if ((!isReverse && from < endTime && to >= endTime) || (isReverse && from > endTime && to <= endTime))
                    {
                        if (!times.Contains(endTime))
                        {
                            times.Add(endTime);
                        }
                    }
                }
            }
            times.Sort();
            return times;
        }

        public virtual void Stop()
        {
            elapsedTime = 0f;
            foreach (var timelineItem in timelineItems)
            {
                if (timelineItem.gameObject.activeSelf)
                {
                    timelineItem.Stop();
                }
            }
        }

        public List<Type> GetAllowedTimelineItems()
        {
            if (allowedItemTypes == null)
            {
                allowedItemTypes = TimelineRuntimeHelper.GetAllowedItemTypes(this);
            }
            return allowedItemTypes;
        }

        public TrackGroup trackGroup;
        private Timeline m_Timeline;

        public Timeline timeline
        {
            get
            {
                if (m_Timeline == null)
                    m_Timeline = GetComponentInParent<Timeline>(true);
                return m_Timeline;
            }
            set => m_Timeline = value;
        }

        public int Ordinal
        {
            get
            {
                return ordinal;
            }
            set
            {
                ordinal = value;
            }
        }

        [ReadOnly]
        public List<TimelineItem> timelineItems = new List<TimelineItem>();
        public TimelineAction[] timelineActions;

#if UNITY_EDITOR
        public virtual void OnValidate()
        {
            var parent = transform.parent;
            if (parent == null)
                return;
            trackGroup = parent.GetComponent<TrackGroup>();
            GetComponentsInChildren<TimelineItem>(true, timelineItems);
            timelineActions = GetComponentsInChildren<TimelineAction>(true);
        }
#endif
    }
}
