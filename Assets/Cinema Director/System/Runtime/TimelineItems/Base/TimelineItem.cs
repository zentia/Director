using UnityEngine;
using System;
namespace CinemaDirector
{
    /// <summary>
    /// The basic building block of a Cutscene. Maintains a firetime 
    /// </summary>
    [ExecuteInEditMode]
    public abstract class TimelineItem : MonoBehaviour, IComparable<TimelineItem>
    {
        public int CompareTo(TimelineItem timelineItem)
        {
            if (timelineItem.firetime == firetime)
                return 0;
            if (timelineItem.firetime > firetime)
                return -1;
            return 1;
        }

        [SerializeField]
        protected float firetime;
        [NonSerialized]
        public float _time;
        /// <summary>
        /// The firetime for this timeline item. Cannot be negative.
        /// </summary>
        public float Firetime
        {
            get { return firetime; }
            set
            {
                firetime = value;
                if (firetime < 0f)
                {
                    firetime = 0f;
                }
            }
        }

        /// <summary>
        /// Called when a cutscene begins or enters preview mode.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when a cutscene ends or exits preview mode.
        /// </summary>
        public virtual void Stop() { }

        /// <summary>
        /// Called when a new timeline item is created from the Director panel.
        /// Override to set defaults to your timeline items.
        /// </summary>
        public virtual void SetDefaults() { }

        /// <summary>
        /// Called when a new timeline item is created from the Director panel with a paired item.
        /// Override to set defaults to your timeline items.
        /// </summary>
        /// <param name="PairedItem">The paired item of this timeline item.</param>
        public virtual void SetDefaults(UnityEngine.Object PairedItem) { }

        /// <summary>
        /// The cutscene that this timeline item is associated with. Can return null.
        /// </summary>
        public Cutscene Cutscene
        {
            get { return ((TimelineTrack == null) ? null : TimelineTrack.Cutscene); }
        }

        /// <summary>
        /// The track that this timeline item is associated with. Can return null.
        /// </summary>
        public TimelineTrack TimelineTrack
        {
            get
            {
                TimelineTrack track = null;
                if (transform.parent != null)
                {
                    track = transform.parent.GetComponentInParent<TimelineTrack>();
                    if (track == null)
                    {
                        Debug.LogError("No TimelineTrack found on parent!", this);
                    }
                }
                else
                {
                    Debug.LogError("Timeline Item has no parent!", this);
                }
                return track;
            }
        }
    }
}