using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yarp;

namespace TimelineRuntime
{
    /// <summary>
    /// An event for calling the game object send message method.
    /// Cannot be reversed.
    /// </summary>
    [TimelineItem("Render Setting", "Set Postprocess Feature", TimelineItemGenre.ActorItem)]
    public class SetPostProcessFeature : TimelineActorEvent, IRecoverableObject
    {
        [FormerlySerializedAs("EnableControlPostProcess")]
        public bool EnableControlDepthOfField;

        public bool EnableDepthOfField;

        public bool EnableControlBloom;
        public bool EnableBloom;

        public bool EnableControlFog;
        public bool EnableFog;

        public bool EnableControlVignette;
        public bool EnableVignette;
        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        private bool previousEnableBloom;
        private bool previousEnableControlBloom;

        private bool previousEnableDepthOfField;
        private bool previousEnableControlDepthOfField;

        private bool previousEnableFog;
        private bool previousEnableControlFog;

        private bool previousEnableVignette;
        private bool previousEnableControlVignette;
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
                var renderSettingsTimeline = actors.GetComponent<RenderSettingTimeline>();
                var renderSettingsVolume = actors.GetComponent<RenderSettingsVolume>();
                if (renderSettingsTimeline != null)
                {
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableControlDepthOfField", renderSettingsTimeline.EnableControlDepthOfField));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableDepthOfField", renderSettingsTimeline.EnableDepthOfField));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableControlBloom", renderSettingsTimeline.EnableControlBloom));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableBloom", renderSettingsTimeline.EnableBloom));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableControlFog", renderSettingsTimeline.EnableControlFog));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableFog", renderSettingsTimeline.EnableFog));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableControlVignette", renderSettingsTimeline.EnableControlVignette));
                    reverts.Add(new RevertInfo(this, renderSettingsTimeline, "EnableVignette", renderSettingsTimeline.EnableVignette));
                    reverts.Add(new RevertInfo(this, RevertDof, renderSettingsVolume, renderSettingsVolume.Settings.PostEffect.DOFSetting.Enable));
                    reverts.Add(new RevertInfo(this, RevertBloom, renderSettingsVolume, renderSettingsVolume.Settings.PostEffect.EnableBloom));
                    reverts.Add(new RevertInfo(this, RevertFog, renderSettingsVolume, renderSettingsVolume.Settings.Fog.activate));
                    reverts.Add(new RevertInfo(this, RevertVignette, renderSettingsVolume, renderSettingsVolume.Settings.PostEffect.EnableVignette));
                }
            }
            return reverts.ToArray();
        }

        public void RevertDof(Object actor, object userData)
        {
            var renderSetting = actor as RenderSettingsVolume;
            var enable = (bool)userData;
            if (renderSetting)
            {
                renderSetting.Settings.PostEffect.DOFSetting.Enable = enable;
            }
        }

        public void RevertBloom(Object actor, object userData)
        {
            var renderSetting = actor as RenderSettingsVolume;
            var enable = (bool)userData;
            if (renderSetting)
            {
                renderSetting.Settings.PostEffect.EnableBloom = enable;
            }
        }

        public void RevertFog(Object actor, object userData)
        {
            var renderSetting = actor as RenderSettingsVolume;
            var enable = (bool)userData;
            if (renderSetting)
            {
                renderSetting.Settings.Fog.activate = enable;
            }
        }

        public void RevertVignette(Object actor, object userData)
        {
            var renderSetting = actor as RenderSettingsVolume;
            var enable = (bool)userData;
            if (renderSetting)
            {
                renderSetting.Settings.PostEffect.EnableVignette = enable;
            }
        }

        /// <summary>
        /// Initialize and save the original colour state.
        /// </summary>
        /// <param name="actor">The actor to initialize the light colour with.</param>
        public override void Initialize(GameObject actor)
        {
            var renderSettingsVolume = actor.GetComponent<RenderSettingTimeline>();
            if (renderSettingsVolume != null)
            {
                previousEnableControlDepthOfField = renderSettingsVolume.EnableControlDepthOfField;
                previousEnableDepthOfField = renderSettingsVolume.EnableDepthOfField;
                previousEnableControlBloom = renderSettingsVolume.EnableControlBloom;
                previousEnableBloom = renderSettingsVolume.EnableBloom;
                previousEnableControlFog = renderSettingsVolume.EnableControlFog;
                previousEnableFog = renderSettingsVolume.EnableFog;
                previousEnableControlVignette = renderSettingsVolume.EnableControlVignette;
                previousEnableVignette = renderSettingsVolume.EnableVignette;
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
                var renderSettingsVolume = actor.GetComponent<RenderSettingTimeline>();
                if (renderSettingsVolume != null)
                {
                    previousEnableControlDepthOfField = renderSettingsVolume.EnableControlDepthOfField;
                    previousEnableDepthOfField = renderSettingsVolume.EnableDepthOfField;

                    previousEnableControlBloom = renderSettingsVolume.EnableControlBloom;
                    previousEnableBloom = renderSettingsVolume.EnableBloom;

                    previousEnableControlFog = renderSettingsVolume.EnableControlFog;
                    previousEnableFog = renderSettingsVolume.EnableFog;

                    renderSettingsVolume.EnableDepthOfField = EnableDepthOfField;
                    renderSettingsVolume.EnableControlDepthOfField = EnableControlDepthOfField;

                    renderSettingsVolume.EnableBloom = EnableBloom;
                    renderSettingsVolume.EnableControlBloom = EnableControlBloom;

                    renderSettingsVolume.EnableFog = EnableFog;
                    renderSettingsVolume.EnableControlFog = EnableControlFog;

                    renderSettingsVolume.EnableVignette = EnableVignette;
                    renderSettingsVolume.EnableControlVignette = EnableControlVignette;
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
                var renderSettingsVolume = actor.GetComponent<RenderSettingTimeline>();
                if (renderSettingsVolume != null)
                {
                    renderSettingsVolume.EnableControlDepthOfField = previousEnableControlDepthOfField;
                    renderSettingsVolume.EnableDepthOfField = previousEnableDepthOfField;

                    renderSettingsVolume.EnableControlBloom = previousEnableControlBloom;
                    renderSettingsVolume.EnableBloom = previousEnableBloom;

                    renderSettingsVolume.EnableControlFog = previousEnableControlFog;
                    renderSettingsVolume.EnableFog = previousEnableFog;

                    renderSettingsVolume.EnableControlVignette = previousEnableControlVignette;
                    renderSettingsVolume.EnableVignette = previousEnableVignette;
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
