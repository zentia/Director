using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Transitions", "SetSceneRoot", TimelineItemGenre.GlobalItem)]
    public class SetSceneRoot : TimelineGlobalEvent
    {
        public Vector3 localPosition;
        
        public override void Trigger()
        {
            if (timeline.sceneRoot)
            {
                timeline.sceneRoot.transform.localPosition = localPosition;
            }    
        }
    }
}