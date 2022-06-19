using UnityEngine;
using Assets.Plugins.Common;
using CinemaDirector;
using Yarp;

namespace AGE
{
    [EventCategory("ModifyBloomSetting")]
    public class ModifyBloomSetting : TickEvent
    {
        public struct BloomSetting
        {
            public bool EnableBloom;

            public float BloomThreshold;

            public float BloomSoftKnee;

            public float BloomIntensity;

            public float BloomDiffusion;

            public float BloomFireflyRemovalStrength;

            public float BloomClamp;

            public Color BloomTint;
        }
        
        [Template]
        public int modifyTargetId = 0;//要修改的目标

        public bool EnableBloom;

        public float BloomThreshold;

        public float BloomSoftKnee;

        public float BloomIntensity;

        public float BloomDiffusion;

        public float BloomFireflyRemovalStrength;

        public float BloomClamp;

        public Color BloomTint;

        private BloomSetting oriBloomSetting = new BloomSetting();

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
            
            oriBloomSetting.EnableBloom = postEffectSetting.EnableBloom;
            oriBloomSetting.BloomThreshold = postEffectSetting.BloomThreshold;
            oriBloomSetting.BloomSoftKnee = postEffectSetting.BloomSoftKnee;
            oriBloomSetting.BloomIntensity = postEffectSetting.BloomIntensity;
            oriBloomSetting.BloomDiffusion = postEffectSetting.BloomDiffusion;
            oriBloomSetting.BloomFireflyRemovalStrength = postEffectSetting.BloomFireflyRemovalStrength;
            oriBloomSetting.BloomClamp = postEffectSetting.BloomClamp;
            oriBloomSetting.BloomTint = postEffectSetting.BloomTint;
            
            postEffectSetting.EnableBloom = EnableBloom;
            postEffectSetting.BloomThreshold = BloomThreshold;
            postEffectSetting.BloomSoftKnee = BloomSoftKnee;
            postEffectSetting.BloomIntensity = BloomIntensity;
            postEffectSetting.BloomDiffusion = BloomDiffusion;
            postEffectSetting.BloomFireflyRemovalStrength = BloomFireflyRemovalStrength;
            postEffectSetting.BloomClamp = BloomClamp;
            postEffectSetting.BloomTint = BloomTint;
        }


        public override void ProcessBlend(Action action, Track track, TickEvent prevEvent, float blendWeight)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
            var _prevEvent = prevEvent as ModifyBloomSetting;
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
            
            postEffectSetting.BloomSoftKnee = Mathf.Lerp(_prevEvent.BloomSoftKnee,this.BloomSoftKnee,
                 blendWeight);
            postEffectSetting.BloomIntensity = Mathf.Lerp(_prevEvent.BloomIntensity,this.BloomIntensity,
                 blendWeight);
            postEffectSetting.BloomDiffusion = Mathf.Lerp(_prevEvent.BloomDiffusion,this.BloomDiffusion,
                 blendWeight);
            postEffectSetting.BloomFireflyRemovalStrength = Mathf.Lerp(_prevEvent.BloomFireflyRemovalStrength,this.BloomFireflyRemovalStrength,
                 blendWeight);
            postEffectSetting.BloomClamp = Mathf.Lerp(_prevEvent.BloomClamp,this.BloomClamp,
                 blendWeight);
            postEffectSetting.BloomTint = Color.Lerp(_prevEvent.BloomTint, this.BloomTint,
                 blendWeight);
        }
        
        protected override void CopyData(BaseEvent src)
        {
            ModifyBloomSetting r = src as ModifyBloomSetting;

            if (r != null)
            {
                modifyTargetId = r.modifyTargetId;
                EnableBloom = r.EnableBloom;
                BloomThreshold = r.BloomThreshold;
                BloomSoftKnee = r.BloomSoftKnee;
                BloomIntensity = r.BloomIntensity;
                BloomDiffusion = r.BloomDiffusion;
                BloomFireflyRemovalStrength = r.BloomFireflyRemovalStrength;
                BloomClamp = r.BloomClamp;
                BloomTint = r.BloomTint;
            }
        }

        protected override void ClearData()
        {
            modifyTargetId = -1;
        }
    }
}