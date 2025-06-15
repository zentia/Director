using UnityEngine;

namespace TimelineRuntime
{
    public class TimelineAction : TimelineItem
    {
        public GameObject Actor
        {
            get
            {
                if (timelineTrack == null)
                {
                    timelineTrack = transform.parent.GetComponent<TimelineTrack>();
#if UNITY_EDITOR
                    timelineTrack.OnValidate();
#endif
                }
                var actorTrackGroup = timelineTrack.trackGroup as ActorTrackGroup;
                if (actorTrackGroup != null && actorTrackGroup.Actors != null && actorTrackGroup.Actors.Count > 0)
                {
                    var actor = actorTrackGroup.Actors[0];
                    if (actor)
                    {
                        return actor.gameObject;
                    }
                }

                return null;
            }
        }

        [SerializeField]
        protected float duration = 0f;

        public float Duration
        {
            get { return duration; }
            set
            {
                duration = value;
            }
        }

        public float EndTime
        {
            get
            {
                return firetime + duration;
            }
        }

        public short endFrame => TimelineUtility.TimeToFrame(EndTime);

        public override void SetDefaults()
        {
            Duration = 5f;
        }

        public virtual void SampleTime(Transform actor, float time) { }
    }
}
