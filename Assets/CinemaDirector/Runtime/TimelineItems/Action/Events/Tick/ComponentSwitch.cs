using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{

    [EventCategory("Utility")]
    public class ComponentSwitch : TickEvent
    {
        public string componentType = "";
        public bool enabled = true;

        public override bool SupportEditMode()
        {
            return true;
        }

        [Template]
        public int targetId = 0;

        public override void Process(Action _action, Track _track)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null) return;

            Component component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                Log.LogE("AGE", targetObject.name + " doesn't have a " + componentType + " !");
                return;
            }

            Collider colliderComp = component as Collider;
            Behaviour behaviour = component as Behaviour;
            Renderer renderComp = component as Renderer;
            if (colliderComp != null)
            {
                colliderComp.enabled = enabled;
            }
            else if (behaviour != null)
            {
                behaviour.enabled = enabled;
            }
            else if (renderComp != null)
            {
                renderComp.enabled = enabled;
            }
            else if (component.GetType() == typeof(Rigidbody))
            {
                //enable/disable gravity only
                (component as Rigidbody).isKinematic = !enabled;
            }
        }

        protected override void CopyData(BaseEvent src)
        {
            var copySrc = src as ComponentSwitch;
            componentType = copySrc.componentType;
            enabled = copySrc.enabled;
            targetId = copySrc.targetId;
        }

        protected override void ClearData()
        {
            componentType = "";
            enabled = true;
            targetId = -1;
        }
    }
}
