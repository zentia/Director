using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Game Object", "Layer", TimelineItemGenre.ActorItem)]
    public class ChangeLayer : TimelineActorEvent, IRecoverableObject
    {
        public string layer;

        public RevertMode runtimeRevertMode;

        public RevertInfo[] CacheState()
        {
            var actor = GetActor();
            if (actor == null || runtimeRevertMode == RevertMode.Finalize)
            {
                return null;    
            }
            var go = actor.gameObject;
            return new []{new RevertInfo(this, Revert, go, go.layer)};
        }

        private static void Revert(Object actor, object l)
        {
            (actor as GameObject).SetLayer(l as string);
        }

        public override void Trigger(GameObject actor)
        {
            actor.SetLayer(layer);
        }

        public override void Reverse(GameObject actor)
        {
            actor.SetLayer(layer);
        }

        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}