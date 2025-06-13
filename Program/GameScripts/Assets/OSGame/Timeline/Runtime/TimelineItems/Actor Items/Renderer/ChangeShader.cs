using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Renderer", "Change Shader", TimelineItemGenre.ActorItem)]
    public class ChangeShader : TimelineActorEvent, IRecoverableObject
    {
        private const string EditorDir = "Assets/CustomResources/Shaders/Hero/";
        private const string RuntimeDir = "Shaders/Hero/";
        [FilePath(ParentFolder = EditorDir, Extensions = ".shader")]
        public string path;
        [Tag]
        public string m_Tag;
        public RevertMode RuntimeRevertMode
        {
            get
            {
                return RevertMode.Revert;
            }
            set{}
        }

        public RevertInfo[] CacheState()
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            var actors = GetActors();
            List<RevertInfo> reverts = new List<RevertInfo>();
            foreach (var actor in actors)
            {
                if (actor != null)
                {
                    var renderers = actor.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        if (r.tag == m_Tag)
                        {
                            reverts.Add(new RevertInfo(this, Revert, r, r.sharedMaterial.shader));    
                        }
                    }
                }
            }
            return reverts.ToArray();
        }

        public void Revert(Object actor, object userData)
        {
            (actor as Renderer).sharedMaterial.shader = userData as Shader;
        }


        public override void Trigger(GameObject actor)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var shader = ShaderCache.Load(RuntimeDir + path);
            if (shader == null)
            {
                return;
            }

            var renderers = actor.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.tag == m_Tag)
                {
                    r.sharedMaterial.shader = shader;    
                }
            }
        }
    }
}