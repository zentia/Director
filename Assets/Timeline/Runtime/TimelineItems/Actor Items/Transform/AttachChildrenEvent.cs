using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// Attaches all objects as children of actor in hierarchy
    /// </summary>
    [TimelineItem("Transform", "Attach Children", TimelineItemGenre.ActorItem)]
    public class AttachChildrenEvent : TimelineActorEvent, IRecoverableObject
    {
        public GameObject[] Children;
        private Transform[] Parents;
        public override void Trigger(GameObject actor)
        {
            Parents = new Transform[Children.Length];

            if (actor != null && Children != null)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    GameObject child = Children[i];
                    Parents[i] = child.transform.parent;
                    child.transform.SetParent(actor.transform, true);
                }
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null && Children != null && Parents != null)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    GameObject child = Children[i];
                    child.transform.parent = Parents[i].transform;
                }
            }
        }

        public RevertInfo[] CacheState()
        {
            List<RevertInfo> reverts = new List<RevertInfo>();
            for (int i = 0; i < Children.Length; i++)
            {
                Transform go = Children[i].transform;
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
