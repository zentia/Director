using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// Detaches all children in hierarchy from this Parent.
    /// </summary>
    [TimelineItem("Transform", "Detach Children", TimelineItemGenre.ActorItem)]
    public class DetachChildrenEvent : TimelineActorEvent, IRecoverableObject
    {
        private Transform[] Children;
        public override void Trigger(GameObject actor)
        {
            Children = new Transform[actor.transform.childCount];
            for (int i = 0; i < actor.transform.childCount; i++)
            {
                Children[i] = actor.transform.GetChild(i);
            }

            if (actor != null)
            {
                actor.transform.DetachChildren();
            }
        }

        public override void Reverse(GameObject actor)
        {
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i].SetParent(actor.transform, true);
            }
        }

        public RevertInfo[] CacheState()
        {
            var actors = (GetActor());
            List<RevertInfo> reverts = new List<RevertInfo>();
                if (actors != null)
                {
                    for (int j = 0; j < actors.transform.childCount; j++)
                    {
                        Transform child = actors.transform.GetChild(j);
                        reverts.Add(new RevertInfo(this, child, "parent", child.parent));
                        reverts.Add(new RevertInfo(this, child, "localPosition", child.localPosition));
                        reverts.Add(new RevertInfo(this, child, "localRotation", child.localRotation));
                        reverts.Add(new RevertInfo(this, child, "localScale", child.localScale));
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
