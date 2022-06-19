using UnityEngine;
using System.Collections;
using System;
using Assets.Plugins.Common;
using CinemaDirector;
using Yarp;

namespace AGE
{
    [EventCategory("ModifyVignetteSetting")]
    public class ModifyVignetteSetting : TickEvent
    {
        public struct VignetteSetting
        {
            public Color VignetteColor;
            public Vector2 VignetteCenter;
            public float VignetteIntensity;
            public float VignetteSmoothness;
            public bool VignetteRounded;
        }
        [Template]
        public int modifyTargetId = 0;//要修改的目标
        
        public Color VignetteColor = Color.black;
        public Vector2 VignetteCenter = new Vector2(0.5f, 0.5f);
        public float VignetteIntensity = 0;
        public float VignetteSmoothness = 0.2f;
        public bool VignetteRounded = false;
        
        private VignetteSetting oriVignetteSetting = new VignetteSetting();
        
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
            
            oriVignetteSetting.VignetteColor = postEffectSetting.VignetteColor;
            oriVignetteSetting.VignetteCenter = postEffectSetting.VignetteCenter;
            oriVignetteSetting.VignetteIntensity = postEffectSetting.VignetteIntensity;
            oriVignetteSetting.VignetteSmoothness = postEffectSetting.VignetteSmoothness;
            oriVignetteSetting.VignetteRounded = postEffectSetting.VignetteRounded;

            postEffectSetting.VignetteColor = VignetteColor;
            postEffectSetting.VignetteCenter = VignetteCenter;
            postEffectSetting.VignetteIntensity = VignetteIntensity;
            postEffectSetting.VignetteSmoothness = VignetteSmoothness;
            postEffectSetting.VignetteRounded = VignetteRounded;
        }
        
        public override void ProcessBlend(Action action, Track track, TickEvent prevEvent, float blendWeight)
        {
            GameObject modifyGo = action.GetGameObject(modifyTargetId);
            var _prevEvent = prevEvent as ModifyVignetteSetting;
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
            
            postEffectSetting.VignetteColor = Color.Lerp(_prevEvent.VignetteColor,this.VignetteColor,
                blendWeight);
            postEffectSetting.VignetteCenter = Vector2.Lerp(_prevEvent.VignetteCenter,this.VignetteCenter,
                blendWeight);
            postEffectSetting.VignetteIntensity = Mathf.Lerp(_prevEvent.VignetteIntensity,this.VignetteIntensity,
                blendWeight);
            postEffectSetting.VignetteSmoothness = Mathf.Lerp(_prevEvent.VignetteSmoothness,this.VignetteSmoothness,
                blendWeight);
        }
        
        protected override void CopyData(BaseEvent src)
        {
            ModifyVignetteSetting r = src as ModifyVignetteSetting;

            if (r != null)
            {
                modifyTargetId = r.modifyTargetId;
                VignetteColor = r.VignetteColor;
                VignetteCenter = r.VignetteCenter;
                VignetteIntensity = r.VignetteIntensity;
                VignetteSmoothness = r.VignetteSmoothness;
                VignetteRounded = r.VignetteRounded;
            }
        }

        protected override void ClearData()
        {
            modifyTargetId = -1;
        }
    }
}