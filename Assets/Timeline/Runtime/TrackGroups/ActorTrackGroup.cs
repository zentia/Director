using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Framework.AssetService;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TimelineRuntime
{
    public interface ITrackScreenFitQuery
    {
        ScreenFitConfig.TimelineTrackType trackType { get; }
    }

    [TimelineTrackGroup("Actor Track Group", TimelineTrackGenre.ActorTrack)]
    public class ActorTrackGroup : TrackGroup, ITrackScreenFitQuery
    {
        private static Dictionary<int, ScreenFitConfig.CameraCategory> s_CameraCategoryMap = new()
        {
            { (int)ScreenFitConfig.CameraCategory.Square, ScreenFitConfig.CameraCategory.Standard },
            { (int)ScreenFitConfig.CameraCategory.Wide, ScreenFitConfig.CameraCategory.UltraWide },
        };

        [ShowInInspector, OnValueChanged("OnActorChanged")]
        private List<Transform> m_Actors = new();
        private InstantiatableAsset m_Asset;
        private List<InstantiatableAsset> m_ItemAsset = new();
        [Asset, OnValueChanged("OnPathChanged")]
        public string path;

        public string parentName;
        public Vector3 localPosition;
        public Quaternion localRotation;

        private ScreenFitConfig.TimelineTrackType mTrackType = ScreenFitConfig.TimelineTrackType.Default;

        public ScreenFitConfig.TimelineTrackType trackType=> mTrackType;

        public ScreenFitConfig.CameraCategory cameraCategory => GetCameraTypeByAspectRatio();

#if UNITY_EDITOR
        private void OnPathChanged()
        {
            AssetService.instance.Unload(m_Asset);
            m_Asset = null;
            m_Actors.Clear();

            if (string.IsNullOrEmpty(path))
                return;

            m_Asset = AssetService.instance.LoadInstantiateAsset(path);
            if (m_Asset != null && !m_Asset.Invalid())
            {
                m_Actors.Add(m_Asset.Tf);
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
                AssetService.instance.Unload(m_Asset);
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

        private ScreenFitConfig.CameraCategory GetCameraTypeByAspectRatio()
        {
            return ScreenFitConfig.GetInstance().GetCameraCategory();
        }

        private void WhetherScreenFitActor()
        {
            mTrackType = ScreenFitConfig.TimelineTrackType.Default;
            ScreenFitConfig.CameraCategory category = ScreenFitConfig.CameraCategory.UltraWide;

            foreach (ScreenFitConfig.TimelineTrackType type in Enum.GetValues(typeof(ScreenFitConfig.TimelineTrackType)))
            {
                var ret = ScreenFitConfig.GetTimelineTrackNameDict(type);
                if (null != ret)
                {
                    foreach (var kv in ret)
                    {
                        if (kv.Value == gameObject.name)
                        {
                            mTrackType = type;
                            category = (ScreenFitConfig.CameraCategory)kv.Key;
                            break;
                        }
                    }
                }
            }

            if(mTrackType == ScreenFitConfig.TimelineTrackType.Default)
                return;

            gameObject.SetActive(category == cameraCategory);
        }

        public override void Initialize()
        {
            WhetherScreenFitActor();
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
                m_Asset = AssetService.instance.LoadInstantiateAsset(path, LifeType.GameState);
                if (!m_Asset.Valid())
                {
                    PostInitialize();
                    return;
                }
                if (timeline.sceneRoot)
                {
                    if (string.IsNullOrEmpty(parentName))
                    {
                        m_Asset.Tf.SetParent(timeline.sceneRoot.transform);
                    }
                    else
                    {
                        var parent = timeline.sceneRoot.FindChildBFS(parentName);
                        m_Asset.Tf.SetParent(parent != null ? parent.transform : timeline.sceneRoot.transform);
                    }
                }
                Actors.Add(m_Asset.RootTf);
                Actors[0].localPosition = localPosition;
                Actors[0].localRotation = localRotation;
            }
            else if (timeline.sceneRoot != null)
            {
                var go = timeline.sceneRoot.FindChildBFS(name);
                if (go == null)
                {
                    // TODO
                    // 这里是个trick的做法：如果是作用于棋盘的timeline，sceneRoot业务侧传入的是美术场景的根节点，但如果美术场景里没有新增的相机节点，逻辑就会失效，所以这里只是为了不让逻辑被跳过
                    foreach (var kv in ScreenFitConfig.GetTimelineTrackNameDict(ScreenFitConfig.TimelineTrackType.Camera))
                    {
                        if (kv.Value == gameObject.name)
                        {
                            if (s_CameraCategoryMap.TryGetValue(kv.Key, out var rawCameraType))
                            {
                                var rawName = ScreenFitConfig.GetTimelineSceneCameraTrackName(rawCameraType);
                                go = timeline.sceneRoot.FindChildBFS(rawName);
                            }
                            break;
                        }
                    }

                    if (go == null)
                    {
                        PostInitialize();
                        return;
                    }
                }
                var cam = go.GetComponent<Camera>();
                if (cam != null)
                {
                    cam = CameraSystem.instance.mainCamera;
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
            if (m_Asset != null && m_Asset.Tf != null)
            {
                m_Actors.Remove(m_Asset.Tf);
            }

            if (m_Asset != null && m_Asset.Valid())
            {
                AssetService.instance.Unload(m_Asset);
            }
        }

        public InstantiatableAsset LoadInstantiatableAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }
            var asset = AssetService.instance.LoadInstantiateAsset(assetName);
            if (asset == null || asset.Invalid())
                return null;
            m_ItemAsset.Add(asset);
            return asset;
        }

        public void UnloadInstantiatableAsset(ref InstantiatableAsset asset)
        {
            if (!m_ItemAsset.Contains(asset))
            {
                return;
            }
            m_ItemAsset.Remove(asset);
            AssetService.instance.Unload(asset);
            asset = null;
        }

        public List<Renderer> GetRenderers()
        {
            var renderers = new List<Renderer>();
            m_Actors.ForEach(o => renderers.AddRange(o.GetComponentsInChildren<Renderer>()));
            m_ItemAsset.ForEach(o=>renderers.AddRange(o.Go.GetComponentsInChildren<Renderer>()));
            return renderers;
        }
    }
}
