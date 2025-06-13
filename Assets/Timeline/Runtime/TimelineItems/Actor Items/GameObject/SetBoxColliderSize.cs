using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// An event for calling the game object send message method.
    /// Cannot be reversed.
    /// </summary>
    [TimelineItem("Game Object", "Set BoxCollider Size", TimelineItemGenre.ActorItem)]
    public class SetBoxColliderSize : TimelineActorEvent, IRecoverableObject
    {
        public Vector3 Size;
        
        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        private Vector3 previousSize;
        
        /// <summary>
        /// Cache the state of all actors related to this event.
        /// </summary>
        /// <returns></returns>
        public RevertInfo[] CacheState()
        {
            var actors = GetActor();
            List<RevertInfo> reverts = new List<RevertInfo>();
            if (actors != null )
            {
                BoxCollider boxCollider = actors.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    reverts.Add(new RevertInfo(this, boxCollider, "size", boxCollider.size));
                }
            }
            return reverts.ToArray();
        }
        
        /// <summary>
        /// Initialize and save the original colour state.
        /// </summary>
        /// <param name="actor">The actor to initialize the light colour with.</param>
        public override void Initialize(GameObject actor)
        {
            BoxCollider boxCollider = actor.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                previousSize = boxCollider.size;
            }
        }
        
        /// <summary>
        /// Trigger this event and change the Color of a given actor's light component.
        /// </summary>
        /// <param name="actor">The actor with the light component to be altered.</param>
        public override void Trigger(GameObject actor)
        {
            if (actor != null)
            {
                BoxCollider boxCollider = actor.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    previousSize = boxCollider.size;
                    boxCollider.size = Size;
                }
            }
        }

        /// <summary>
        /// Reverse setting the light colour.
        /// </summary>
        /// <param name="actor">The actor to reverse the light setting on.</param>
        public override void Reverse(GameObject actor)
        {
            if (actor != null)
            {
                BoxCollider boxCollider = actor.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    boxCollider.size = previousSize;
                }
            }
        }

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