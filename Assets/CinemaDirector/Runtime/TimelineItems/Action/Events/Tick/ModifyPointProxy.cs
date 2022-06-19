using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
    [EventCategory("ModifyPointProxy")]
    public class ModifyPointProxy : TickEvent
    {
        [Template]
        public int modifyTargetId = 0;//要修改的目标

        public float range = 0;
        public float intensity = 0;
        public float attenuation = 0;
        public Color color = Color.black;
        public Vector3 position = Vector3.zero;
        
        private float originRange = 0;
        private float originIntensity = 0;
        private float originAttenuation = 0;
        private Color originColor = Color.black;
        private Vector3 oriPosition = Vector3.zero;

        public override void Process(Action action, Track track)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
           
            if (modifyGo == null)
            {
                Log.LogE("AGE", "<color=red>[ ModifyPointProxy No modifyTargetId GameObject]</color> action:" + action.actionName);
                return;
            }

            var pointLightProxy = modifyGo.GetComponent<PointLightProxy>();
            if (pointLightProxy == null)
            {
                Log.LogE("AGE", "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }

            originRange = pointLightProxy.Range;
            originIntensity = pointLightProxy.Intensity;
            originAttenuation = pointLightProxy.Attenuation;
            originColor = pointLightProxy.Color;
            oriPosition = pointLightProxy.transform.position;
            
            pointLightProxy.Range = range;
            pointLightProxy.Intensity = intensity;
            pointLightProxy.Attenuation = attenuation;
            pointLightProxy.Color = color;
            pointLightProxy.transform.position = position;
        }

        public override void ProcessBlend(Action action, Track track, TickEvent prevEvent, float blendWeight)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
            var _prevEvent = prevEvent as ModifyPointProxy;
            if (modifyGo == null || _prevEvent == null)
                return;
            
            var pointLightProxy = modifyGo.GetComponent<PointLightProxy>();
            if (pointLightProxy == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }
            
            float curRange = Mathf.Lerp(_prevEvent.range,range,  blendWeight);
            float curIntensity = Mathf.Lerp(_prevEvent.intensity,intensity,  blendWeight);
            float curAttenuation = Mathf.Lerp(_prevEvent.attenuation,attenuation,  blendWeight);
            Color curColor = Color.Lerp(_prevEvent.color,color,  blendWeight);
            Vector3 curPosition = Vector3.Lerp(_prevEvent.position, position,  blendWeight);
            
            pointLightProxy.Range = curRange;
            pointLightProxy.Intensity = curIntensity;
            pointLightProxy.Attenuation = curAttenuation;
            pointLightProxy.Color = curColor;
            pointLightProxy.transform.position = curPosition;
        }

        protected override void CopyData(BaseEvent src)
        {
            ModifyPointProxy r = src as ModifyPointProxy;

            if (r != null)
            {
                modifyTargetId = r.modifyTargetId;
                range = r.range;
                attenuation = r.attenuation;
                intensity = r.intensity;
                color = r.color;
            }
        }

        protected override void ClearData()
        {
            modifyTargetId = -1;
            range = 0;
            attenuation = 0;
            intensity = 0;
            color = Color.black;
        }
    }
}