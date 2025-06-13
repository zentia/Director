using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// Attaches actor as child of target in hierarchy
    /// </summary>
    [TimelineItem("Transform", "Set Parent", TimelineItemGenre.ActorItem)]
    public class SetParent : TimelineActorEvent, IRecoverableObject
    {
        public GameObject parent;
        private Transform originalParent;

        public override void Trigger(GameObject actor)
        {
            if (actor != null && parent != null)
            {
                originalParent = actor.transform.parent;
                actor.transform.SetParent(parent.transform, true);
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                actor.transform.SetParent(originalParent, true);
            }
        }

        public RevertInfo[] CacheState()
        {
            var actors = (GetActor());
            List<RevertInfo> reverts = new List<RevertInfo>();
            {
                Transform go = actors;
                if (go != null)
                {
                    reverts.Add(new RevertInfo(this, go, "parent", go.parent));
                    reverts.Add(new RevertInfo(this, go, "localPosition", go.localPosition));
                    reverts.Add(new RevertInfo(this, go, "localRotation", go.localRotation));
                    reverts.Add(new RevertInfo(this, go, "localScale", go.localScale));
                }
            }

            return reverts.ToArray();
        }

        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Editor.
        /// </summary>
        public RevertMode EditorRevertMode
        {
            get { return editorRevertMode; }
            set { editorRevertMode = value; }
        }

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Runtime.
        /// </summary>
        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}
