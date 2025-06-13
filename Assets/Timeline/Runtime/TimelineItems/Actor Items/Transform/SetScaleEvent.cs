using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// Detaches all children in hierarchy from this Parent.
    /// </summary>
    [TimelineItem("Transform", "Set Scale", TimelineItemGenre.ActorItem)]
    public class SetScaleEvent : TimelineActorEvent, IRecoverableObject
    {

        public Vector3 Scale = Vector3.one;
        private Vector3 InitialScale;

        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                InitialScale = actor.transform.localScale;
                actor.transform.localScale = Scale;
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                actor.transform.localScale = InitialScale;
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
                    reverts.Add(new RevertInfo(this, go.gameObject.transform, "localScale", go.gameObject.transform.localScale));
                }
            }

            return reverts.ToArray();
        }

        /// <summary>
        /// Option for choosing when this curve curveClip will Revert to initial state in Editor.
        /// </summary>
        public RevertMode EditorRevertMode
        {
            get { return editorRevertMode; }
            set { editorRevertMode = value; }
        }

        /// <summary>
        /// Option for choosing when this curve curveClip will Revert to initial state in Runtime.
        /// </summary>
        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}