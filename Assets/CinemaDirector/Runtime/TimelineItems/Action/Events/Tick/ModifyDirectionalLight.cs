using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
    [EventCategory("ModifyDirectionalLight")]
    public class ModifyDirectionalLight : TickEvent
    {
        [Template] 
        public int modifyTargetId = 0; //要修改的目标

        public float intensity = 0.0f;
        public Vector3 eulerAngle = Vector3.zero;
        public Color color = Color.white;
        
        private float oriIntensity = 0.0f;
        private Vector3 oriEulerAngle = Vector3.zero;
        private Color oriColor = Color.white;

        public override void Process(Action action, Track track)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);

            if (modifyGo == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy No modifyTargetId GameObject]</color> action:" + action.actionName);
                return;
            }

            var directionalLight = modifyGo.GetComponent<Light>();
            if (directionalLight == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }

            
            oriIntensity = directionalLight.intensity;
            oriEulerAngle = directionalLight.transform.eulerAngles;
            oriColor = directionalLight.color;

            directionalLight.intensity = intensity;
            directionalLight.transform.rotation = Quaternion.Euler(eulerAngle);
            directionalLight.color = color;
        }

        public override void ProcessBlend(Action action, Track track, TickEvent prevEvent, float blendWeight)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
            var _prevEvent = prevEvent as ModifyDirectionalLight;
            if (modifyGo == null || _prevEvent == null)
                return;
            
            var directionalLight = modifyGo.GetComponent<Light>();
            if (directionalLight == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }
            
            float curIntensity = Mathf.Lerp(_prevEvent.intensity, intensity, blendWeight);
            Color curColor = Color.Lerp(_prevEvent.color,color,  blendWeight);
            Vector3 curEulerAngle = Vector3.Lerp(_prevEvent.eulerAngle,eulerAngle,  blendWeight);

            directionalLight.intensity = curIntensity;
            directionalLight.color = curColor;
            directionalLight.transform.rotation = Quaternion.Euler(curEulerAngle);
        }
        
        protected override void CopyData(BaseEvent src)
        {
            ModifyDirectionalLight r = src as ModifyDirectionalLight;

            if (r != null)
            {
                modifyTargetId = r.modifyTargetId;
                intensity = r.intensity;
                eulerAngle = r.eulerAngle;
                color = r.color;
            }
        }

        protected override void ClearData()
        {
            modifyTargetId = -1;
            intensity = 0;
            eulerAngle = Vector3.zero;
            color = Color.white;
        }
        
    }
}