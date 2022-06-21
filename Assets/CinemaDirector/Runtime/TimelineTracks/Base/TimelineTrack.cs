using System;
using System.Xml;
using System.Collections.Generic;
using AGE;
using Gear;
using UnityEngine;

namespace CinemaDirector
{
    public class TimelineTrack : Track
    {
        protected float elapsedTime = 0f;

        // A cache of the TimelineItems for optimization purposes.
        protected TimelineItem[] itemCache;

        // A list of the cutscene item types that this Track is allowed to contain.
        protected List<Type> allowedItemTypes;

        public override int CompareTo(DirectorObject directorObject)
        {
            var timelineTrack = directorObject as TimelineTrack;
            return trackIndex - timelineTrack.trackIndex;
        }

        public void OnValidate()
        {
            Dirty = true;
        }

        /// <summary>
        /// Perform any initialization before the cutscene begins a fresh playback
        /// </summary>
        public virtual void Initialize() 
        {
            if (!enabled)
            {
                return;
            }
            elapsedTime = 0f;
            foreach (TimelineItem item in Children)
            {
                item.Initialize();
            }
        }

        /// <summary>
        /// Update the track to the given time
        /// </summary>
        /// <param name="time"></param>
        public virtual void UpdateTrack(float runningTime, float deltaTime) 
        {
            if (!enabled)
            {
                return;
            }
            float previousTime = elapsedTime;
            elapsedTime = runningTime;

            foreach (TimelineItem item in GetTimelineItems())
            {
                if ((previousTime < item.Firetime) && (elapsedTime >= item.Firetime))
                {
                    item.Trigger();
                }
                else if ((previousTime >= item.Firetime) && (elapsedTime < item.Firetime))
                {
                    item.Reverse();
                }
            }

            foreach (TimelineItem item in Children)
            {
                var action = item as DurationEvent;
                if (action == null) continue;
                if (previousTime < action.Firetime && elapsedTime >= action.Firetime && elapsedTime < action.EndTime)
                {
                    action.Trigger();
                }
                else if ((previousTime < action.EndTime) && (elapsedTime >= action.EndTime))
                {
                    action.End();
                }
                else if (previousTime > action.Firetime && previousTime <= action.EndTime && elapsedTime < action.Firetime)
                {
                    action.ReverseTrigger();
                }
                else if (previousTime > (action.EndTime) && (elapsedTime > action.Firetime) && (elapsedTime <= action.EndTime))
                {
                    action.ReverseEnd();
                }
                else if ((elapsedTime > action.Firetime) && (elapsedTime < action.EndTime))
                {
                    float t = runningTime - action.Firetime;
                    action.UpdateTime(t, deltaTime);
                }
            }
        }

        /// <summary>
        /// Notify track items that the cutscene has been paused
        /// </summary>
        public virtual void Pause() { }

        /// <summary>
        /// Notify track items that the cutscene has been resumed from a paused state.
        /// </summary>
        public virtual void Resume() { }

        /// <summary>
        /// The cutscene has been set to an arbitrary time by the user.
        /// Processing must take place to catch up to the new time.
        /// </summary>
        /// <param name="time">The new cutscene running time</param>
        public virtual void SetTime(float time)
        {
            if (!enabled)
            {
                return;
            }
            float previousTime = elapsedTime;
            elapsedTime = time;

            foreach (TimelineItem item in GetTimelineItems())
            {
                if (item != null)
                {
                    if ((previousTime < item.Firetime) && (((elapsedTime >= item.Firetime))))
                    {
                        item.Trigger();
                    }
                    else if (((previousTime >= item.Firetime) && (elapsedTime < item.Firetime)))
                    {
                        item.Reverse();
                    }
                }

                var action = item as TimelineAction;
                if (action != null)
                {
                    action.UpdateTime((time - action.Firetime), time - previousTime);
                }
            }
        }

        /// <summary>
        /// Retrieve a list of important times for this track within the given range.
        /// </summary>
        /// <param name="from">The starting point of the range.</param>
        /// <param name="to">The end point of the range.</param>
        /// <returns>A list of ordered milestone times within the given range.</returns>
        public virtual List<float> GetMilestones(float from, float to)
        {
            bool isReverse = from > to;
            
            List<float> times = new List<float>();
            foreach (TimelineItem item in GetTimelineItems())
            {
                if ((!isReverse && from < item.Firetime && to >= item.Firetime) || (isReverse && from > item.Firetime && to <= item.Firetime))
                {
                    if (!times.Contains(item.Firetime))
                    {
                        times.Add(item.Firetime);
                    }
                }

                if (item is TimelineAction)
                {
                    float endTime = (item as TimelineAction).EndTime;
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

        /// <summary>
        /// Notify the track items that the cutscene has been stopped
        /// </summary>
        public virtual void Stop() 
        {
            if (!enabled)
            {
                return;
            }
            foreach (TimelineItem item in GetTimelineItems())
            {
                item.Stop();
            }
        }

        public List<Type> GetAllowedCutsceneItems()
        {
            if (allowedItemTypes == null)
            {
                allowedItemTypes = DirectorRuntimeHelper.GetAllowedItemTypes(this);
            }

            return allowedItemTypes;
        }

        /// <summary>
        /// The Cutscene that this Track is associated with. Can return null.
        /// </summary>
        public Cutscene Cutscene
        {
            get { return ((TrackGroup == null) ? null : TrackGroup.Cutscene); }
        }

        /// <summary>
        /// The TrackGroup associated with this Track. Can return null.
        /// </summary>
        public TrackGroup TrackGroup
        {
            get
            {
                TrackGroup group = null;
                if (Parent != null)
                {
                    group = Parent as TrackGroup;
                }
                else
                {
                    Debug.LogError("Track has no parent.", this);
                }
                return group;
            }
        }

        public float length
        {
            get
            {
                return Cutscene.Duration;
            }
            set
            {
                Cutscene.Duration = value;
            }
        }

        /// <summary>
        /// Get all TimelineItems that are allowed to be in this Track.
        /// </summary>
        /// <returns>A filtered list of Timeline Items.</returns>
        public List<DirectorObject> GetTimelineItems()
        {
            return Children;
        }
        
        [HideInInspector]
        public TimelineItem Template;

        public Type ItemType { get; set; }

        public override DirectorObject CreateChild(DirectorObject directorObject = null)
        {
            var timelineItem = AddEvent(0, 1);
            timelineItem.SetParent(this);
            return timelineItem;
        }
        
        public override void Import(XmlElement xmlElement)
        {
            base.Import(xmlElement);
            var eventType = xmlElement.GetAttribute("eventType");
            var tpName = "AGE." + eventType;
            var tp = CSharpUtilities.GetType(tpName);
            var cutscene = Cutscene;
            cutscene.AddTrack(this);
            enabled = bool.Parse(xmlElement.GetAttribute("enabled"));
            trackName = name;
            bool.TryParse(xmlElement.GetAttribute("execOnActionCompleted"), out execOnActionCompleted);
            bool.TryParse(xmlElement.GetAttribute("execOnForceStopped"), out execOnForceStopped);
            bool.TryParse(xmlElement.GetAttribute("stopAfterLastEvent"), out stopAfterLastEvent);
            foreach (XmlElement evtElement in xmlElement.ChildNodes)
            {
                var timelineItem = CreateInstance(tp) as TimelineItem;
                var item = CreateChild(timelineItem) as TimelineItem;
                if (item == null)
                {
                    continue;
                }
                item.Import(evtElement);
            }
        }

        public override void Export(XmlElement xmlElement)
        {
            base.Export(xmlElement);
            var element = xmlElement.OwnerDocument.CreateElement("Track");
            element.SetAttribute("trackName", name);
            element.SetAttribute("group", Parent.name);
            element.SetAttribute("eventType", ItemType.Name);
            element.SetAttribute("enabled", enabled.ToString());
            element.SetAttribute("execOnForceStopped", execOnForceStopped.ToString());
            element.SetAttribute("execOnActionCompleted", execOnActionCompleted.ToString());
            element.SetAttribute("stopAfterLastEvent", stopAfterLastEvent.ToString());
            xmlElement.AppendChild(element);
            foreach (TimelineItem item in Children)
            {
                item.Export(element);
            }
        }

        public override void OnUninitialize()
        {
            if (Cutscene == null)
            {
                return;
            }
            Cutscene.tracks.Remove(this);            
        }

        protected override void OnRemoveChild(DirectorObject child)
        {
            var item = child as TimelineItem;
            trackEvents.Remove(item);
        }

        public override void UpdateRaw()
        {
            trackEvents.Clear();
            foreach (TimelineItem directorObject in Children)
            {
                trackEvents.Add(directorObject);
                trackEvents.Sort();
            }
        }
        public Transform GetActor(int id)
        {
            if (id < 0 || id >= Cutscene.m_templateObjectList.Count)
            {
                return null;
            }
            var template = Cutscene.m_templateObjectList[id];
            if (template == null || template.gameObject == null)
                return null;
            return template.gameObject.transform;
        }
    }
}