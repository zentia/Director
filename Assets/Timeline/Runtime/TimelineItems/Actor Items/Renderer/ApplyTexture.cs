using UnityEngine;
using System.Collections.Generic;

namespace TimelineRuntime
{
    /// <summary>
    /// An Event for applying a texture to an actor.
    /// </summary>
    [TimelineItem("Renderer", "Apply Texture", TimelineItemGenre.ActorItem)]
    public class ApplyTexture : TimelineActorEvent, IRecoverableObject
    {
        public Texture texture;
        private Texture initialTexture;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        /// <summary>
        /// Trigger this event and apply the texture.
        /// </summary>
        /// <param name="actor">The actor to apply the texture to.</param>
        public override void Trigger(GameObject actor)
        {
            Renderer r = actor.GetComponent<Renderer>();
            if (r != null && texture != null)
            {
                initialTexture = r.sharedMaterial.mainTexture;
                r.sharedMaterial.mainTexture = texture;
            }
        }

        /// <summary>
        /// Reverse trigger this event and revert to the initial texture.
        /// </summary>
        /// <param name="actor">The actor to apply the texture to.</param>
        public override void Reverse(GameObject actor)
        {
            Renderer r = actor.GetComponent<Renderer>();
            if (r != null && texture != null)
            {
                r.sharedMaterial.mainTexture = initialTexture;
            }
        }

        public RevertInfo[] CacheState()
        {
            var actors = (GetActor());
            List<RevertInfo> reverts = new List<RevertInfo>();
                Renderer r = actors.GetComponent<Renderer>();
                if (r != null)
                {
                    reverts.Add(new RevertInfo(this, r.sharedMaterial, "mainTexture", r.sharedMaterial.mainTexture));
                }
            return reverts.ToArray();
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
