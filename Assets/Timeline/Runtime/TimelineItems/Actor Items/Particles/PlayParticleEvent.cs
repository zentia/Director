using Assets.Scripts.Framework.AssetService;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Particle System", "PlayEvent", TimelineItemGenre.ActorItem)]
    public class PlayParticleEvent : TimelineActorEvent
    {
        [Asset]
        public string resourcePath;
        public string mountPoint;
        [LabelText("延期删除")]
        public bool delayDelete;
        [LabelText("有效期")]
        [ShowIf("delayDelete")]
        public float expire;
        private InstantiatableAsset m_Asset;
#if UNITY_EDITOR
        [ShowInInspector, ReadOnly]
        private GameObject m_Preview;
#endif
        private TimelineAssetWrap m_TimelineAssetWrap;

        private Transform GetParent(Transform t)
        {
            if (string.IsNullOrEmpty(mountPoint))
            {
                return t;
            }

            var p = t.Find(mountPoint);
            return p != null ? p : t;
        }

        public override void Initialize(GameObject actor)
        {
            if (m_Asset != null && m_Asset.Valid())
            {
                if (m_TimelineAssetWrap != null)
                {
                    m_TimelineAssetWrap.markDelete = false;
                    m_Asset.RootGo.SetActive(false);
                }
                return;
            }
            if (delayDelete && expire > 0)
            {
                m_TimelineAssetWrap = TimelineService.instance.LoadParticleAsset(resourcePath, expire, timeline.GetInstanceID());
                if (m_TimelineAssetWrap != null)
                {
                    m_Asset = m_TimelineAssetWrap.asset;
                }
            }
            else
            {
                m_Asset = AssetService.instance.LoadParticleAsset(resourcePath);
            }
            if (m_Asset == null || m_Asset.Invalid())
                return;
#if UNITY_EDITOR
            m_Preview = m_Asset.RootGo;
#endif
            m_Asset.RootGo.SetActive(false);
        }

        public override void Trigger(GameObject actor)
        {
            if (m_Asset == null || m_Asset.Invalid())
            {
                return;
            }
            m_Asset.RootGo.SetActive(true);
            m_Asset.RootTf.SetParent(GetParent(actor.transform));
            var t = transform;
            m_Asset.RootTf.localPosition = t.localPosition;
            m_Asset.RootTf.localRotation = t.localRotation;
            m_Asset.RootTf.localScale = t.localScale;
        }

        public override void Stop(GameObject actor)
        {
            if (delayDelete && m_TimelineAssetWrap != null)
            {
                m_TimelineAssetWrap.markDelete = true;
                return;
            }
            if (m_Asset == null || m_Asset.Invalid())
            {
                return;
            }
            m_Asset.Unload();
            m_Asset = null;
        }
    }
}
