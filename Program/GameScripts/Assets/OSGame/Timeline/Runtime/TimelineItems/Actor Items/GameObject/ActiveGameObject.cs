using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Game Object", "Active", TimelineItemGenre.ActorItem)]
    public class ActiveGameObject : TimelineActorEvent, IRecoverableObject
    {
        public bool active;

        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        public RevertInfo[] CacheState()
        {
            var actor = GetActor();
            List<RevertInfo> reverts = new List<RevertInfo>();
            if (actor != null && runtimeRevertMode == RevertMode.Revert)
            {
                reverts.Add(new RevertInfo(this, actor.gameObject, "SetActive", actor.gameObject.activeSelf));
            }

            return reverts.ToArray();
        }

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                actor.SetActive(active);
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                actor.SetActive(!active);
            }
        }

        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}