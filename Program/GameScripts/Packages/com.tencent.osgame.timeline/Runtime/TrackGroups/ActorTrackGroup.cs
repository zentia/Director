using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineTrackGroup("Actor Track Group", TimelineTrackGenre.ActorTrack)]
    public class ActorTrackGroup : TrackGroup
    {

        [ShowInInspector, OnValueChanged("OnActorChanged")]
        private List<Transform> m_Actors = new();
        private Transform m_Asset;
        private List<Transform> m_ItemAsset = new();
        [Asset, OnValueChanged("OnPathChanged")]
        public string path;

        public string parentName;
        public Vector3 localPosition;
        public Quaternion localRotation;

        [NonSerialized]
        public int trackType = 0;

#if UNITY_EDITOR
        private void OnPathChanged()
        {
            Timeline.UnLoad?.Invoke(m_Asset);
            m_Asset = null;
            m_Actors.Clear();

            if (string.IsNullOrEmpty(path))
                return;

            m_Asset = Timeline.LoadAsset?.Invoke(path);
            if (m_Asset != null)
            {
                m_Actors.Add(m_Asset);
            }
            else
            {
                m_Asset = null;
            }
        }

        private void OnActorChanged()
        {
            if (m_Actors == null || m_Actors.Count == 0)
            {
                Timeline.UnLoad(m_Asset);
                m_Asset = null;
            }
        }
#endif

        public List<Transform> Actors
        {
            get { return m_Actors; }
            set
            {
                m_Actors = value;
            }
        }

        public override void Initialize()
        {
            Timeline.WhetherScreenFitActor?.Invoke(this);
            if (!gameObject.activeSelf)
            {
                return;
            }
            if (m_Actors != null)
            {
                m_Actors = m_Actors.Where(o => o != null).ToList();
                if (m_Actors.Count > 0)
                {
                    PostInitialize();
                    return;
                }
            }
            Actors.Clear();
            if (!string.IsNullOrEmpty(path))
            {
                m_Asset = Timeline.LoadAsset(path);
                if (!m_Asset)
                {
                    PostInitialize();
                    return;
                }
                if (timeline.sceneRoot)
                {
                    if (string.IsNullOrEmpty(parentName))
                    {
                        m_Asset.SetParent(timeline.sceneRoot.transform);
                    }
                    else
                    {
                        var parent = TimelineRuntimeHelper.FindChildBFS(timeline.sceneRoot,parentName);
                        m_Asset.SetParent(parent != null ? parent.transform : timeline.sceneRoot.transform);
                    }
                }
                Actors.Add(m_Asset);
                Actors[0].localPosition = localPosition;
                Actors[0].localRotation = localRotation;
            }
            else if (timeline.sceneRoot != null)
            {
                var go = TimelineRuntimeHelper.FindChildBFS(timeline.sceneRoot,name);
                if (go == null)
                {
                    go = Timeline.OnFillCameraActor?.Invoke(this);

                    if (go == null)
                    {
                        PostInitialize();
                        return;
                    }
                }
                var cam = go.GetComponent<Camera>();
                if (cam != null)
                {
                    cam = Timeline.OnGetMainCamera?.Invoke();
                    Actors.Add(cam.transform);
                }
                else
                    Actors.Add(go.transform);
            }
            PostInitialize();
        }

        private void PostInitialize()
        {
            foreach (var timelineTrack in timelineTracks)
            {
                if (!timelineTrack.gameObject.activeSelf)
                {
                    continue;
                }
                timelineTrack.trackGroup = this;
                timelineTrack.timeline = timeline;
                timelineTrack.Initialize();
            }
        }

        public override void Stop()
        {
            base.Stop();
            if (m_Asset != null)
            {
                m_Actors.Remove(m_Asset);
                Timeline.UnLoad(m_Asset);
            }

        }

        public Transform LoadInstantiatableAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }
            var asset = Timeline.LoadAsset(assetName);
            if (asset == null)
                return null;
            m_ItemAsset.Add(asset);
            return asset;
        }

        public void UnloadInstantiatableAsset(ref Transform asset)
        {
            if (!m_ItemAsset.Contains(asset))
            {
                return;
            }
            m_ItemAsset.Remove(asset);
            Timeline.UnLoad(asset);
            asset = null;
        }

        public List<Renderer> GetRenderers()
        {
            var renderers = new List<Renderer>();
            m_Actors.ForEach(o => renderers.AddRange(o.GetComponentsInChildren<Renderer>()));
            m_ItemAsset.ForEach(o=>renderers.AddRange(o.GetComponentsInChildren<Renderer>()));
            return renderers;
        }
    }
}
