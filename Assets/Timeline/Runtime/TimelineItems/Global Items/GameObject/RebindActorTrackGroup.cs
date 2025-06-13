using Sirenix.OdinInspector;
using UnityEngine;
using Yarp;

namespace TimelineRuntime
{
    [TimelineItem("Game Object", "RebinActorTrackGroup", TimelineItemGenre.GlobalItem)]
    public class RebinActorTrackGroup : TimelineGlobalEvent
    {
        [SerializeField, LabelText("需要被重新初始化的轨道组")]
        private ActorTrackGroup actorTrackGroup;

        public override void Trigger()
        {
            if (actorTrackGroup == null)
            {
                return;
            }
            actorTrackGroup.Initialize();
        }

    }
}
