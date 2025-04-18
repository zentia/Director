using CinemaDirector.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// Attaches actor as child of target in hierarchy
    /// </summary>
    [CutsceneItemAttribute("Transform", "Set Parent", CutsceneItemGenre.ActorItem)]
    public class SetParent : CinemaActorEvent, IRevertable
    {
        public GameObject parent;
        private Transform originalParent;        

        public override void Trigger(GameObject actor)
        {
            if (actor != null && parent != null)
            {
                originalParent = actor.transform.parent;
                actor.transform.parent = parent.transform;                
            }
        }

        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {                
                actor.transform.parent = originalParent;
            }
        }

        public RevertInfo[] CacheState()
        {
            List<Transform> actors = new List<Transform>(GetActors());
            List<RevertInfo> reverts = new List<RevertInfo>();
            for (int i = 0; i < actors.Count; i++)
            {
                Transform go = actors[i];
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