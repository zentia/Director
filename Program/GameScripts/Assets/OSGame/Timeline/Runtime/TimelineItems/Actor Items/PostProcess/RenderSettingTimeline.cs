using System;
using UnityEngine;
using Yarp;

namespace TimelineRuntime
{
    [ExecuteInEditMode]
    public class RenderSettingTimeline : MonoBehaviour
    {
         [NonSerialized]
        public bool EnableControlDepthOfField = false;

        [NonSerialized]
        public bool EnableControlBloom = false;

        [NonSerialized]
        public bool EnableControlFog = false;

        [NonSerialized]
        public bool EnableControlVignette = false;

        [NonSerialized]
        public bool EnableDepthOfField = false;

        [NonSerialized]
        public bool EnableBloom = false;

        [NonSerialized]
        public bool EnableFog = false;

        [NonSerialized]
        public bool EnableVignette = false;
        [Header("DepthOfField")]
        public float FocalDistance = 7;
        public float FocalRegion   = 4;
        public float NearRegion    = 1;
        public float FarRegion     = 1;
        [Range(0.01f,10)]
        public float     BlurRadius    = 2;

        [Header("Bloom")]
        [Range(0.01f, 4f)]
        public float BloomThreshold = 0.9f;

        [Range(0f, 1f)]
        public float BloomSoftKnee = 0.5f;

        [Range(0f, 4f)]
        public float BloomIntensity = 0f;

        [Range(0f, 1f)]
        public float BloomDiffusion = 0.7f;

        [Range(0.1f, 2f)]
        public float BloomFireflyRemovalStrength = 1.0f;

        // [ShowIf("@this.SelectBloom == BloomMethod.Origin")]
        public float BloomClamp = 65472f;

        // [ShowIf("@this.SelectBloom == BloomMethod.Origin")]
        public Color BloomTint = Color.white;

        [Header("Fog")]
        [ColorUsage(true, true, 0, 4.0f, 0, 4.0f)]
        public Color FogColor = new(0.75f, 0.69f, 1.0f, 1.0f);

        public float FogColorIntensity = 1;

        [Range(0.0f, 400.0f)]
        public float FogDistance = 30.0f;

        [Range(0.0f, 200.0f)]
        public float FogDistanceFadeRange = 10.0f;

        [Range(-80.0f, 80.0f)]
        public float FogHeight = 0.0f;

        [Range(-400.0f, 400.0f)]
        public float FogHeightFadeRange = 10.0f;

        public bool EnableNoFogDomain = false;
        public float NoFogDomain = 5f;
        public float NoFogDomainFadeRange = 10.0f;

        [Header("Vignette")]
        public PostEffectSetting.VignetteMethod SelectVignette = PostEffectSetting.VignetteMethod.PostProcess;
        public Color   VignetteColor      = Color.black;
        public Vector2 VignetteCenter     = new Vector2(0.5f, 0.5f);
        public float   VignetteIntensity  = 0;
        public float   VignetteSmoothness = 0.2f;
        public bool    VignetteRounded    = false;

        private RenderSettingsVolume currentRenderSettingsVolume;

        public void OnEnable()
        {
            currentRenderSettingsVolume = GetComponent<RenderSettingsVolume>();
        }

        public void LateUpdate()
        {
            if (EnableControlDepthOfField && currentRenderSettingsVolume != null && currentRenderSettingsVolume.Settings != null)
            {
                if (EnableDepthOfField)
                {
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.Enable = EnableDepthOfField;
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.FarRegion = FarRegion;
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.FocalDistance = FocalDistance;
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.FocalRegion = FocalRegion;
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.NearRegion = NearRegion;
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.BlurRadius = BlurRadius;
                }
                else
                {
                    currentRenderSettingsVolume.Settings.PostEffect.DOFSetting.Enable = EnableDepthOfField;
                }
            }

            if (EnableControlBloom && currentRenderSettingsVolume != null && currentRenderSettingsVolume.Settings != null)
            {
                if (EnableBloom)
                {
                    currentRenderSettingsVolume.Settings.PostEffect.EnableBloom = EnableBloom;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomThreshold = BloomThreshold;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomSoftKnee = BloomSoftKnee;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomIntensity = BloomIntensity;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomDiffusion = BloomDiffusion;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomFireflyRemovalStrength = BloomFireflyRemovalStrength;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomClamp = BloomClamp;
                    currentRenderSettingsVolume.Settings.PostEffect.BloomTint = BloomTint;
                }
                else
                {
                    currentRenderSettingsVolume.Settings.PostEffect.EnableBloom = EnableBloom;
                }
            }

            if (EnableControlFog && currentRenderSettingsVolume != null && currentRenderSettingsVolume.Settings != null)
            {
                if (EnableFog)
                {
                    currentRenderSettingsVolume.Settings.Fog.activate = EnableFog;
                    currentRenderSettingsVolume.Settings.Fog.FogColor = FogColor;
                    currentRenderSettingsVolume.Settings.Fog.FogColorIntensity = FogColorIntensity;
                    currentRenderSettingsVolume.Settings.Fog.FogHeight = FogHeight;
                    currentRenderSettingsVolume.Settings.Fog.FogDistance = FogDistance;
                    currentRenderSettingsVolume.Settings.Fog.FogDistanceFadeRange = FogDistanceFadeRange;
                    currentRenderSettingsVolume.Settings.Fog.FogHeightFadeRange = FogHeightFadeRange;
                    currentRenderSettingsVolume.Settings.Fog.EnableNoFogDomain = EnableNoFogDomain;
                    currentRenderSettingsVolume.Settings.Fog.NoFogDomain = NoFogDomain;
                    currentRenderSettingsVolume.Settings.Fog.NoFogDomainFadeRange = NoFogDomainFadeRange;
                }
                else
                {
                    currentRenderSettingsVolume.Settings.Fog.activate = EnableFog;
                }
            }

            if (EnableControlVignette && currentRenderSettingsVolume != null && currentRenderSettingsVolume.Settings != null)
            {
                if (EnableVignette)
                {
                    currentRenderSettingsVolume.Settings.PostEffect.EnableVignette = EnableVignette;
                    currentRenderSettingsVolume.Settings.PostEffect.SelectVignette = SelectVignette;
                    currentRenderSettingsVolume.Settings.PostEffect.VignetteColor = VignetteColor;
                    currentRenderSettingsVolume.Settings.PostEffect.VignetteCenter = VignetteCenter;
                    currentRenderSettingsVolume.Settings.PostEffect.VignetteIntensity = VignetteIntensity;
                    currentRenderSettingsVolume.Settings.PostEffect.VignetteSmoothness = VignetteSmoothness;
                    currentRenderSettingsVolume.Settings.PostEffect.VignetteRounded = VignetteRounded;
                }
                else
                {
                    currentRenderSettingsVolume.Settings.PostEffect.EnableVignette = EnableVignette;
                }
            }
        }
    }
}
