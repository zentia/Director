using System;
using Assets.Scripts.Framework.AssetService;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineRuntime
{
    [TimelineItem("Renderer", "Change Material KeyWords", TimelineItemGenre.ActorItem)]
    public class SetMaterialKeyWords : TimelineActorEvent, IRecoverableObject
    {
        public enum KeyWords
        {
            _USE_SHADOWMASK_REALTIME_SHADOW,
            _SCENE_EXTRA_LIGHT_ON
        }

        public class Params : Object
        {
            public string keyWords = "";
        }

        public bool EnableKeyWords = false;
        
        public KeyWords keyWords = KeyWords._USE_SHADOWMASK_REALTIME_SHADOW;
        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;
        
        public RevertInfo[] CacheState()
        {
            var actor = GetActor();
            List<RevertInfo> reverts = new List<RevertInfo>();

            if (actor != null)
            {
                var renderers = actor.GetComponent<Renderer>();
                reverts.Add(new RevertInfo(this, Revert, renderers.sharedMaterial, new Params(){ keyWords = this.keyWords.ToString() }));
            }
            
            return reverts.ToArray();
        }
        
        public void Revert(Object actor, object userData)
        {
            var mat = (actor as Material);
            var paramValues = userData as Params;
            if (mat != null && paramValues != null)
            {
                mat.EnableKeyword(paramValues.keyWords);
            }
        }

        public override void Trigger(GameObject actor)
        {
            var renderer = actor.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                if (EnableKeyWords)
                {
                    renderer.sharedMaterial.EnableKeyword(keyWords.ToString());
                }
                else
                {
                    renderer.sharedMaterial.DisableKeyword(keyWords.ToString());
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