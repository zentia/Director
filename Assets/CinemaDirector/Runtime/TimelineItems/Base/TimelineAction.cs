using System.Xml;
using AGE;
using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// An action that has some firetime and duration.
    /// </summary>
    public class TimelineAction : BaseEvent
    {
        public DurationEvent durationEvent => this as DurationEvent;
        
        /// <summary>
        /// The duration of the action
        /// </summary>
        public float Duration
        {
            get
            {
                return durationEvent.length;
            }
            set 
            {
                if (Mathf.Approximately(durationEvent.length, value))
                    return;
                Dirty = true;
                SetDuration(value);
            }
        }

        public override bool IsDuration()
        {
            return true;
        }

        public override bool IsCondition()
        {
            return false;
        }

        

        private void SetDuration(float value)
        {
            durationEvent.length = value;
        }

        /// <summary>
        /// The end time of this action. (Firetime + Duration).
        /// </summary>
        public float EndTime
        {
            get
            {
                return Firetime + Duration;
            }
        }

        /// <summary>
        /// Set a default duration of 5 seconds for most actions.
        /// </summary>
        public override void SetDefaults()
        {
            Duration = 5f;
        }
        
        public override void Import(XmlElement xmlElement)
        {
            base.Import(xmlElement);
            SetDuration(float.Parse(xmlElement.GetAttribute("length")));
        }

        public override void Export(XmlElement xmlElement)
        {
            base.Export(xmlElement);
            XmlElement xml = xmlElement.ChildNodes[xmlElement.ChildNodes.Count - 1] as XmlElement;
            xml.SetAttribute("length", Duration.ToString());
        }
        
        public override void Trigger()
        {
            durationEvent.Enter(Cutscene, TimelineTrack);
        }
        
        public virtual void UpdateTime(float time, float deltaTime)
        {
            durationEvent.Process(Cutscene, TimelineTrack, time);
        }

        public void End()
        {
            durationEvent.Leave(Cutscene, TimelineTrack);
        }

        public void ReverseTrigger()
        {
            durationEvent.ReverseTrigger(Cutscene, TimelineTrack);
        }

        public void ReverseEnd()
        {
            durationEvent.ReverseEnd(Cutscene, TimelineTrack);
        }

        public override void Initialize()
        {
            if (!TimelineTrack.enabled)
            {
                return;
            }

            if (Cutscene.RunningTime > Firetime && Cutscene.RunningTime < Firetime + Duration)
            {
                durationEvent.OnActionStart(Cutscene);
                durationEvent.Enter(Cutscene, track);
                durationEvent.Process(Cutscene, track, Cutscene.RunningTime - track.Length + Firetime);
            }
        }

        protected override void CopyData(BaseEvent src)
        {
            throw new System.NotImplementedException();
        }

        protected override void ClearData()
        {
            throw new System.NotImplementedException();
        }
    }
}