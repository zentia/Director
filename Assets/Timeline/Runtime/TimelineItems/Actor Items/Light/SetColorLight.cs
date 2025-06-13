using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// Set the colour of a light component of a given actor.
    /// </summary>
    [TimelineItem("Light", "Set Light Colour", TimelineItemGenre.ActorItem)]
    public class SetLightColour : TimelineActorEvent, IRecoverableObject
    {
        // The new color.
        public Color Color;

        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        private Color previousColor;

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
                Light light = actors.GetComponent<Light>();
                if (light != null)
                {
                    reverts.Add(new RevertInfo(this, light, "color", light.color));
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
            Light light = actor.GetComponent<Light>();
            if (light != null)
            {
                previousColor = light.color;
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
                Light light = actor.GetComponent<Light>();
                if (light != null)
                {
                    previousColor = light.color;
                    light.color = Color;
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
                Light light = actor.GetComponent<Light>();
                if (light != null)
                {
                    light.color = previousColor;
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