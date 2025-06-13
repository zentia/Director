using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Renderer", "Change Material", TimelineItemGenre.ActorItem)]
    public class ChangeMaterial : TimelineActorEvent, IRecoverableObject
    {
        private const string RuntimeDir = "Materials/";

        [Tag]
        public string m_Tag;

        public Material Material;
        public bool replaceMainTexture;
        public RevertMode RuntimeRevertMode
        {
            get
            {
                return RevertMode.Revert;
            }
            set { }
        }

        public Material GetMaterial()
        {
            return Material;
        }

        public RevertInfo[] CacheState()
        {
            var material = GetMaterial();
            if (material == null)
                return null;
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
                            reverts.Add(new RevertInfo(this, Revert, r, r.sharedMaterial));
                        }
                    }
                }
            }
            return reverts.ToArray();
        }

        public void Revert(Object actor, object userData)
        {
            var curRender = (actor as Renderer);
            if(curRender == null)
                return;
            var oldMtl = curRender.sharedMaterial;
            var recoverMtl = userData as Material;
            if(oldMtl == recoverMtl)
                return;

            curRender.sharedMaterial = recoverMtl;
        }

        public override void Trigger(GameObject actor)
        {
            var material = GetMaterial();
            var renderers = actor.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!r.tag.Equals(m_Tag))
                {
                    continue;
                }
                var sharedMaterial = r.sharedMaterial;
                if(sharedMaterial == null)
                    continue;
                var mat = material;
                if (replaceMainTexture && mat.HasTexture("_MainTex"))
                {
                    mat = Instantiate(material);
                    Texture mainTexture = null;
                    if (sharedMaterial.HasTexture("_MainTex"))
                    {
                        mainTexture = sharedMaterial.mainTexture;
                    }
                    else if (sharedMaterial.HasTexture("_AlbedoMap"))
                    {
                        mainTexture = sharedMaterial.GetTexture("_AlbedoMap");
                    }
                    mat.mainTexture = mainTexture;
                }
                r.sharedMaterial = mat;
            }
        }
    }
}
