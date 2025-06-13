using Assets.Scripts.Framework.AssetService;
using NOAH.VFX;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Particle System", "PlayAction", TimelineItemGenre.ActorItem)]
    public class PlayParticleAction : TimelineActorAction
    {
        [Asset]
        public string resourcePath;
        public string mountPoint;
        public bool virtualParentObject;
        public bool virtualSceneRootParent;
        private InstantiatableAsset m_Asset;
#if UNITY_EDITOR
        [ShowInInspector, ReadOnly]
        private GameObject m_Preview;
#endif

        public override void Initialize()
        {
            if (m_Asset != null && m_Asset.Valid())
                return;
            m_Asset = (timelineTrack.trackGroup as ActorTrackGroup).LoadInstantiatableAsset(resourcePath);
            if (m_Asset == null || m_Asset.Invalid())
            {
                return;
            }
            m_Asset.RootGo.SetActive(false);
#if UNITY_EDITOR
            m_Preview = m_Asset.RootGo;
#endif
        }

        private Transform GetParent(Transform t)
        {
            if (virtualSceneRootParent)
            {
                return timeline.sceneRoot == null ? null : timeline.sceneRoot.transform;
            }

            if (virtualParentObject)
            {
                return null;
            }
            if (string.IsNullOrEmpty(mountPoint))
            {
                return t;
            }

            var p = t.gameObject.FindChildBFS(mountPoint);
            if (p != null)
            {
                return p.transform;
            }
            return null;
        }

        public override void Trigger(GameObject actor)
        {
            if (m_Asset == null || m_Asset.Invalid())
            {
                return;
            }
            m_Asset.RootGo.SetActive(true);
            m_Asset.RootTf.SetParent(GetParent(actor.transform));
            if (virtualParentObject)
            {
                m_Asset.RootTf.position = actor.transform.position;
                m_Asset.RootTf.rotation = actor.transform.rotation;
            }
            else
            {
                m_Asset.RootTf.localPosition = transform.localPosition;
                m_Asset.RootTf.localRotation = transform.localRotation;
                m_Asset.RootTf.localScale = transform.localScale;
            }
            var vfxEffectHub = m_Asset.RootTf.GetComponent<VFXEffectHub>();
            if (vfxEffectHub)
            {
                vfxEffectHub.Reactivate();
            }
        }

        public override void UpdateTime(GameObject Actor, float time, float deltaTime)
        {
            if (virtualSceneRootParent)
            {
                m_Asset.RootTf.position = Actor.transform.position;
                m_Asset.RootTf.rotation = Actor.transform.rotation;
            }
        }

        public override void ReverseEnd(GameObject actor)
        {
            (timelineTrack.trackGroup as ActorTrackGroup).UnloadInstantiatableAsset(ref m_Asset);
        }

        public override void ReverseTrigger(GameObject actor)
        {
            Trigger(actor);
        }

#if UNITY_EDITOR
        public override void SetTime(GameObject actor, float time, float deltaTime)
        {
            Initialize();
        }
#endif

        public override void End(GameObject actor)
        {
            if (m_Asset == null || m_Asset.Invalid())
            {
                return;
            }
            m_Asset.RootGo.SetActive(false);
        }

        public override void Stop(GameObject Actor)
        {
            if (m_Asset == null)
                return;
            if (m_Asset.Invalid())
            {
                m_Asset = null;
                return;
            }
            (timelineTrack.trackGroup as ActorTrackGroup).UnloadInstantiatableAsset(ref m_Asset);
        }
    }
}
