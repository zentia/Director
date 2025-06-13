using UnityEditor;

namespace TimelineEditor
{
    public class TimelineBehaviourControl
    {
        public TimelineControl timelineControl;

        protected virtual void RequestDelete()
        {
            timelineControl?.ControlDeleteRequest(this, new TimelineBehaviourControlEventArgs(behaviour, this));
        }

        public virtual UnityEngine.Behaviour behaviour { get; }

        public virtual bool IsSelected => ((behaviour != null) && Selection.Contains(behaviour.gameObject));
    }
}

