using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TimelineRuntime
{
    /// <summary>
    /// An Event for enabling any behaviour that has an "enabled" property.
    /// </summary>
    [TimelineItem("Game Object", "Enable Component", TimelineItemGenre.ActorItem)]
    public class EnableComponent : TimelineActorEvent, IRecoverableObject
    {
        [TimelineType]
        public string typeName;

        public bool enableBehaviour;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        /// <summary>
        /// Cache the state of all actors related to this event.
        /// </summary>
        /// <returns>All the revert info related to this event.</returns>
        public RevertInfo[] CacheState()
        {
            var actor = GetActor();
            List<RevertInfo> reverts = new List<RevertInfo>();
            if (actor != null)
            {
                Component b = actor.GetComponent(typeName) ;
                if (b != null)
                {
                    PropertyInfo pi = ReflectionHelper.GetProperty(b.GetType(), "enabled");
                    bool value = (bool)pi.GetValue(b, null);

                    reverts.Add(new RevertInfo(this, b, "enabled", value));
                }
            }

            return reverts.ToArray();
        }

        /// <summary>
        /// Trigger this event and Enable the chosen Behaviour.
        /// </summary>
        /// <param name="actor">The actor to perform the behaviour enable on.</param>
        public override void Trigger(GameObject actor)
        {
            Component b = actor.GetComponent(typeName);
            if (b != null)
            {
                PropertyInfo fieldInfo = ReflectionHelper.GetProperty(b.GetType(), "enabled");
                fieldInfo.SetValue(b, enableBehaviour, null);
            }
        }

        /// <summary>
        /// Reverse trigger this event and Disable the chosen Behaviour.
        /// </summary>
        /// <param name="actor">The actor to perform the behaviour disable on.</param>
        public override void Reverse(GameObject actor)
        {
            Component b = actor.GetComponent(typeName) as Component;
            if (b != null)
            {
                PropertyInfo fieldInfo = ReflectionHelper.GetProperty(b.GetType(), "enabled");
                fieldInfo.SetValue(b, !enableBehaviour, null);
            }
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
