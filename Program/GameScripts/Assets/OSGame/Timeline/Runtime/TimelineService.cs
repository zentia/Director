using System;
using System.Collections.Generic;
using Assets.Plugins.Common;
using Assets.Scripts.Framework.AssetService;
using UnityEngine;

namespace TimelineRuntime
{
    public class TimelineAssetWrap
    {
        public ParticleAsset asset;
        public float expire;
        public int hostInstanceID;
        public bool markDelete
        {
            get => m_MarkDelete;
            set
            {
                if (m_MarkDelete == value)
                    return;
                m_MarkDelete = value;
                if (value)
                {
                    expire += Time.time;
                }
            }
        }
        public const float MaxTime = 2.0f;
        private bool m_MarkDelete;
    }

    [ExecuteInEditMode]
    public class TimelineService : MonoSingleton<TimelineService>
    {
        private readonly Dictionary<Timeline, InstantiatableAsset> m_TimelineAssets = new();
        public List<TimelineAssetWrap> timelineAssetWraps { get; private set; }

        private int m_CheckDelta;

        protected override void Init()
        {
            timelineAssetWraps = new List<TimelineAssetWrap>();
        }

        public TimelineAssetWrap LoadParticleAsset(string filename, float time, int hostInstanceID)
        {
            var asset = AssetService.instance.LoadParticleAsset(filename);
            if (asset == null || asset.Invalid())
            {
                return null;
            }
            if (time > TimelineAssetWrap.MaxTime)
            {
                time = TimelineAssetWrap.MaxTime;
            }
            var wrap = new TimelineAssetWrap { asset = asset, expire = time, hostInstanceID = hostInstanceID};
            timelineAssetWraps.Add(wrap);
            return wrap;
        }

        public void Update()
        {
            if (m_CheckDelta == 3)
            {
#if UNITY_EDITOR
                timelineAssetWraps ??= new List<TimelineAssetWrap>();
#endif
                ValidateAssetByExpireTime();
                m_CheckDelta = 0;
                return;
            }
            ++m_CheckDelta;
        }

        public void ValidateAssetByExpireTime()
        {
            var time = Time.time;
            for (var i = timelineAssetWraps.Count - 1; i >= 0; i--)
            {
                var item = timelineAssetWraps[i];
                if (!(item.markDelete && item.expire < time))
                {
                    continue;
                }
                if (item.asset != null && item.asset.Valid())
                    item.asset.Unload();
                timelineAssetWraps.RemoveAt(i);
            }
        }

        public void ValidateAssetByInstanceId(int instanceId)
        {
            for (var i = timelineAssetWraps.Count - 1; i >= 0; i--)
            {
                var item = timelineAssetWraps[i];
                if (item.hostInstanceID != instanceId)
                {
                    continue;
                }
                if (item.asset != null && item.asset.Valid())
                    item.asset.Unload();
                timelineAssetWraps.RemoveAt(i);
            }
        }


        private Timeline LoadTimeline(string path)
        {
            var asset = AssetService.instance.LoadInstantiateAsset(path);
            if (asset.Invalid())
            {
                return null;
            }

            var timeline = asset.Go.GetComponent<Timeline>();
            if (timeline == null)
            {
                AssetService.instance.Unload(asset);
                return null;
            }
            m_TimelineAssets.Add(timeline, asset);
            return timeline;
        }

        public Timeline PlayTimeline(string path,
            Dictionary<string, GameObject> bindGameObject,
            TimelineHandler finishedHandler = null,
            GameObject sceneRoot = null,
            ClipDataSampleDelegate sampleDelegate = null)
        {
            var timeline = LoadTimeline(path);
            if (timeline == null)
            {
                return null;
            }
#if UNITY_EDITOR
            timeline.name = path;
            timeline.inGame = true;
#endif
            if (bindGameObject != null)
            {
                var trackGroups = timeline.actorTrackGroups;
                foreach (var trackGroup in trackGroups)
                {
                    bindGameObject.TryGetValue(trackGroup.name, out var go);
                    if (go != null)
                    {
                        trackGroup.Actors.Add(go.transform);
                    }
                }
            }

            timeline.ClearFinishedHandle();
            timeline.TimelineFinished += finishedHandler;
            timeline.TimelineFinished += OnTimelineFinished;
            timeline.sceneRoot = sceneRoot;
            timeline.SampleDelegate = sampleDelegate;
            timeline.Play();
            return timeline;
        }

        public Timeline PlayTimelineWithMultiActor(string path, Dictionary<string, List<Transform>> bindGameObject, GameObject sceneRoot = null)
        {
            var timeline = LoadTimeline(path);
            if (timeline == null)
            {
                return null;
            }

            if (bindGameObject != null)
            {
                var trackGroups = timeline.actorTrackGroups;
                foreach (var trackGroup in trackGroups)
                {
                    bindGameObject.TryGetValue(trackGroup.name, out var actors);
                    if (actors != null)
                    {
                        trackGroup.Actors = actors;
                    }
                }
            }
            timeline.TimelineFinished += OnTimelineFinished;
            if (sceneRoot != null)
                timeline.sceneRoot = sceneRoot;
            timeline.Play();
            return timeline;
        }

        public void AddFinishedCallback(Timeline timeline, TimelineHandler callback)
        {
            if (timeline == null || callback == null)
                return;

            timeline.ClearFinishedHandle();
            timeline.TimelineFinished += OnTimelineFinished;
            timeline.TimelineFinished += callback;
        }

        public void StopTimelineNotCallBack(Timeline timeline)
        {
            if (timeline == null)
            {
                return;
            }
            timeline.ClearFinishedHandle();
            timeline.Stop();
        }

        public void StopTimeline(Timeline timeline)
        {
            if (timeline == null)
            {
                return;
            }
            timeline.Stop();
        }

        private void OnTimelineFinished(Timeline timeline, TimelineEventArgs e)
        {
            if (m_TimelineAssets.ContainsKey(timeline))
            {
                var asset = m_TimelineAssets[timeline];
                AssetService.instance.Unload(asset);
                m_TimelineAssets.Remove(timeline);
            }
            else
            {
                Log.LogE(LogTag.Timeline, "timeline {0} unload!", timeline.name);
            }
        }
    }
}
