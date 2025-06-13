using System.Collections.Generic;
using Assets.Scripts.Framework;
using Assets.Scripts.Framework.AssetService;
using Assets.Scripts.GameLogic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Light System", "RegisterLightGroup", TimelineItemGenre.ActorItem)]
    public class RegisterLightGroup : TimelineActorEvent
    {
        public bool active;

        private VirtualScene virtualScene;
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        public RevertInfo[] CacheState()
        {
            var actor = GetActor();
            List<RevertInfo> reverts = new List<RevertInfo>();
            if (actor != null && runtimeRevertMode == RevertMode.Revert)
            {
                reverts.Add(new RevertInfo(this, RevertDof, null, null));
            }

            return reverts.ToArray();
        }
        
        public void RevertDof(Object actor, object userData)
        {
            if (virtualScene)
            {
                Project8VirtualSceneGroup.GetInstance().UnRegisterProject8VirtualScene(virtualScene);
            }
        }

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                virtualScene = actor.GetComponent<VirtualScene>();
                if (virtualScene)
                {
                    Project8VirtualSceneGroup.GetInstance().RegistProject8VirtualScene(virtualScene);
                    Project8VirtualSceneGroup.GetInstance().ActiveVirtualScene(virtualScene, virtualScene.transform.position, false);
                }
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor == null) return;
            if (virtualScene)
            {
                Project8VirtualSceneGroup.GetInstance().UnRegisterProject8VirtualScene(virtualScene);
            }
        }

        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}