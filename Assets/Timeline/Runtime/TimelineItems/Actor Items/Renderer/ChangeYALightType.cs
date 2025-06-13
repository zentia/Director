using System.Collections.Generic;
using UnityEngine;
using Yarp;

namespace TimelineRuntime
{
    [TimelineItem("Renderer", "ChangeYALightType", TimelineItemGenre.ActorItem)]
    public class ChangeYALightType : TimelineActorEvent, IRecoverableObject
    {
        public YALightType lightType;
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;
        public RevertInfo[] CacheState()
        {
            var actor = (GetActor());
            if (actor == null)
            {
                return null;
            }
            List<RevertInfo> reverts = new List<RevertInfo>();
            var r = actor.GetComponent<YALight>();
            if (r != null)
            {
                reverts.Add(new RevertInfo(this, r, "type", r.type));
            }
            return reverts.ToArray();
        }
        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }

        public override void Trigger(GameObject actor)
        {
            var yaLight = actor.GetComponent<YALight>();
            if (yaLight == null) 
                return;
            yaLight.type = lightType;
        }
    }
}