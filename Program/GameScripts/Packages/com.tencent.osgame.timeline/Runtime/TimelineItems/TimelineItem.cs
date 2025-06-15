using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineRuntime
{
    public class TimelineItem : MonoBehaviour
    {
        [SerializeField]
        protected float firetime;

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

        public short fireFrame => TimelineUtility.TimeToFrame(firetime);

        public virtual void Initialize() { }

        public virtual void Stop() { }

        public virtual void SetDefaults() { }

        public virtual void SetDefaults(Object pairedItem) { }

        public virtual void ScrubToTime(float time)
        {

        }

#if UNITY_EDITOR
        public virtual void Refresh()
        {

        }
#endif
        private Timeline m_Timeline;

        public Timeline timeline
        {
            get
            {
                if (!m_Timeline)
                    m_Timeline = GetComponentInParent<Timeline>(true);
                return m_Timeline;
            }
            set=> m_Timeline=value;
        }

        [NonSerialized]
        public TimelineTrack timelineTrack;
    }
}
