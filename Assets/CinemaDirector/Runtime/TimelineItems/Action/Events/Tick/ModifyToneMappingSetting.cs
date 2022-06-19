using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;
using Yarp;

namespace AGE
{
    [EventCategory("ModifyToneMappingSetting")]
    public class ModifyToneMappingSetting : TickEvent
    {
        public struct ToneMappingSetting
        {
            public float ExposureValue;
            public float VFXBrightnessScale;
        }
        
        [Template]
        public int modifyTargetId = 0;//要修改的目标
        
        public float ExposureValue = 0;
        public float VFXBrightnessScale = 1;
        
        private ToneMappingSetting oriToneMappingSetting = new ToneMappingSetting();

        public override void Process(Action action, Track track)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);

            if (modifyGo == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy No modifyTargetId GameObject]</color> action:" + action.actionName);
                return;
            }

            var renderSettingsVolume = modifyGo.GetComponent<RenderSettingsVolume>();
            if (renderSettingsVolume == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }

            var postEffectSetting = renderSettingsVolume.Settings.PostEffect;
            oriToneMappingSetting.ExposureValue = postEffectSetting.ExposureValue;
            oriToneMappingSetting.VFXBrightnessScale = postEffectSetting.VFXBrightnessScale;

            postEffectSetting.ExposureValue = ExposureValue;
            postEffectSetting.VFXBrightnessScale = VFXBrightnessScale;
        }

        public override void ProcessBlend(Action action, Track track, TickEvent prevEvent, float blendWeight)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
            var _prevEvent = prevEvent as ModifyToneMappingSetting;
            if (modifyGo == null || _prevEvent == null)
                return;

            var renderSettingsVolume = modifyGo.GetComponent<RenderSettingsVolume>();
            if (renderSettingsVolume == null)
            {
                Log.LogE("AGE",
                    "<color=red>[ ModifyPointProxy dont have point light proxy]</color> action:" + action.actionName);
                return;
            }

            var postEffectSetting = renderSettingsVolume.Settings.PostEffect;
            
            postEffectSetting.ExposureValue = Mathf.Lerp(_prevEvent.ExposureValue,this.ExposureValue,
                blendWeight);
            postEffectSetting.VFXBrightnessScale = Mathf.Lerp(_prevEvent.VFXBrightnessScale,this.VFXBrightnessScale,
                blendWeight);
        }
        
        protected override void CopyData(BaseEvent src)
        {
            ModifyToneMappingSetting r = src as ModifyToneMappingSetting;

            if (r != null)
            {
                modifyTargetId = r.modifyTargetId;
                VFXBrightnessScale = r.VFXBrightnessScale;
                ExposureValue = r.ExposureValue;
            }
        }

        protected override void ClearData()
        {
            modifyTargetId = -1;
            ExposureValue = 0;
            VFXBrightnessScale = 1;
        }
    }
}