using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimelineRuntime
{
    public delegate bool ClipDataSampleDelegate(MemberCurveClipData clipData, Transform actor, float time, Transform space);

    public delegate Transform LoadAssetDelegate(string path);

    public delegate void UnLoadAssetDelegate(Transform go);

    public delegate GameObject FillCameraActor(ActorTrackGroup actorTrackGroup);

    public delegate Camera GetMainCamera();

    [ExecuteInEditMode]
    public class Timeline : MonoBehaviour
    {
        public enum TimelineState
        {
            Inactive,
            Playing,
            PreviewPlaying,
            Scrubbing,
            Paused
        }

        public float Duration
        {
            get
            {
                return duration;
            }
            set
            {
                duration = value;
                if (duration <= 0f)
                    duration = 0.4f;
            }
        }

        public float RunningTime
        {
            get { return m_RunningTime; }
            set { m_RunningTime = Mathf.Clamp(value, 0, duration); }
        }

        public ClipDataSampleDelegate SampleDelegate;
        public static LoadAssetDelegate LoadAsset;
        public static UnLoadAssetDelegate UnLoad;
        public static GetMainCamera OnGetMainCamera;

        [HideInInspector]
        public List<TrackGroup> trackGroups = new List<TrackGroup>();

        [HideInInspector]
        public ActorTrackGroup[] actorTrackGroups;

        [SerializeField, HideInInspector]
        private MonoBehaviour[] m_RecoverableObjects;

#if UNITY_EDITOR
        public void OnValidate()
        {
            GetComponentsInChildren(true, trackGroups);
            actorTrackGroups = GetComponentsInChildren<ActorTrackGroup>(true);
            var recoverableObjects = GetComponentsInChildren<IRecoverableObject>(true);
            m_RecoverableObjects = new MonoBehaviour[recoverableObjects.Length];
            for (var i = 0; i < m_RecoverableObjects.Length; i++)
            {
                m_RecoverableObjects[i] = recoverableObjects[i] as MonoBehaviour;
            }
        }
#endif

        private void Update()
        {
            if (state is TimelineState.Playing or TimelineState.PreviewPlaying)
            {
                UpdateTimeline(Time.deltaTime);
            }
        }

        public event TimelineHandler TimelineFinished;

        public void ClearFinishedHandle()
        {
            TimelineFinished = null;
        }

        public void Play(bool isPreview = false)
        {
            gameObject.SetActive(true);
            if (state == TimelineState.Inactive)
                FreshPlay();
            else if (state == TimelineState.Paused)
                state = isPreview ? TimelineState.PreviewPlaying : TimelineState.Playing;

            OnPlay?.Invoke(this);
        }

        public static Action<Timeline> OnPlay;
        public static Action<ActorTrackGroup> WhetherScreenFitActor;
        public static FillCameraActor OnFillCameraActor;

        private void FreshPlay()
        {
            PreparePlay();

            state = TimelineState.Playing;
            UpdateTimeline(Time.deltaTime);
        }

        public void Pause()
        {
            if (state == TimelineState.PreviewPlaying || state == TimelineState.Playing || state == TimelineState.Scrubbing)
            {
                for (var i = 0; i < trackGroups.Count; i++)
                    trackGroups[i].Pause();
            }

            state = TimelineState.Paused;
        }

        public void Skip()
        {
            SetRunningTime(Duration);
            Stop();
        }

        public void Stop()
        {
            if (state != TimelineState.PreviewPlaying)
            {
                gameObject.SetActive(false);
            }
            var isFinished = m_RunningTime >= Duration;
            m_RunningTime = 0f;
            foreach (var trackGroup in trackGroups)
            {
                if (trackGroup.gameObject.activeSelf)
                {
                    trackGroup.Stop();
                }
            }

            if (state != TimelineState.Inactive)
                Revert();

            state = TimelineState.Inactive;
            TimelineFinished?.Invoke(this, new TimelineEventArgs { isFinished = isFinished });
            hasBeenInitialized = false;

            OnStop?.Invoke(this);
        }

        public Action<Timeline> OnStop;
        public void UpdateTimeline(float deltaTime)
        {
            RunningTime += deltaTime * playbackSpeed;

            foreach (var trackGroup in trackGroups)
            {
                if (!trackGroup.gameObject.activeSelf)
                {
                    continue;
                }
                trackGroup.UpdateTrackGroup(RunningTime, deltaTime * playbackSpeed);
            }

            if (state != TimelineState.Scrubbing)
            {
                if (duration < deltaTime)
                {
                    isLooping = false;
                }

                if (m_RunningTime >= duration || m_RunningTime < 0f)
                {
                    if (isLooping)
                    {
                        m_RunningTime = 0;
                        ResetElapsedTime();
                        return;
                    }
                    Stop();
                }
            }
        }

        private void ResetElapsedTime()
        {
            foreach (var trackGroup in trackGroups)
            {
                if (!trackGroup.gameObject.activeSelf)
                {
                    continue;
                }

                foreach (var track in trackGroup.timelineTracks)
                {
                    if (!track.gameObject.activeSelf)
                    {
                        continue;
                    }

                    track.elapsedTime = 0;
                }
            }
        }

#if UNITY_EDITOR
        public void UpdateScrub()
        {
            foreach (var trackGroup in trackGroups)
            {
                if (!hasBeenInitialized)
                    trackGroup.Initialize();
                if (trackGroup.gameObject.activeSelf)
                {
                    trackGroup.ScrubToTime(RunningTime);
                }
            }
        }

        public void PreviewPlay()
        {
            gameObject.SetActive(true);
            if (state == TimelineState.Inactive)
                EnterPreviewMode();
            else if (state == TimelineState.Paused) Resume();
            state = TimelineState.PreviewPlaying;
        }
#endif

        public void ScrubToTime(float newTime)
        {
            var deltaTime = Mathf.Clamp(newTime, 0, Duration) - RunningTime;

            state = TimelineState.Scrubbing;
            if (deltaTime != 0)
            {
                if (deltaTime > 1 / 30f)
                {
                    var prevTime = RunningTime;
                    var milestones = GetMilestones(RunningTime + deltaTime);
                    for (var i = 0; i < milestones.Count; i++)
                    {
                        var delta = milestones[i] - prevTime;
                        UpdateTimeline(delta);
#if UNITY_EDITOR
                        UpdateScrub();
#endif
                        prevTime = milestones[i];
                    }
                }
                else
                {
                    UpdateTimeline(deltaTime);
#if UNITY_EDITOR
                    UpdateScrub();
#endif
                }
            }
            else
            {
                Pause();
            }
        }

        public void SetRunningTime(float time)
        {
            var milestones = GetMilestones(time);
            foreach (var milestone in milestones)
            {
                foreach (var trackGroup in trackGroups)
                {
                    if (trackGroup.gameObject.activeSelf)
                    {
                        trackGroup.SetRunningTime(milestone);
                    }
                }
            }
            RunningTime = time;
        }

        public void EnterPreviewMode()
        {
            EnterPreviewMode(m_RunningTime);
        }

        public void EnterPreviewMode(float time)
        {
            if (state != TimelineState.Inactive)
            {
                return;
            }
            gameObject.SetActive(true);
            var instanceID = GetInstanceID();
            OnValidateAssetByInstanceId?.Invoke(instanceID);
            Initialize();
            SetRunningTime(time);
            state = TimelineState.Paused;
        }

        public static Action<int> OnValidateAssetByInstanceId;

        public void ExitPreviewMode()
        {
            Stop();
        }

        public Transform GetActor(string actorName)
        {
            var trackGroup = actorTrackGroups.FirstOrDefault(o => o.name == actorName);
            if (trackGroup.Actors == null)
                return null;
            if (trackGroup.Actors.Count > 0)
                return trackGroup.Actors[0];
            return null;
        }

        public Transform GetActor(ObjectSpace objectSpace)
        {
            if (objectSpace.group)
            {
                var actors = objectSpace.group.Actors;
                foreach (var actor in actors)
                {
                    return actor.Find(objectSpace.path);
                }
            }

            if (sceneRoot)
            {
                return sceneRoot.transform.Find(objectSpace.path);
            }

            return null;
        }

        public void Initialize()
        {
            foreach (var trackGroup in trackGroups)
            {
                trackGroup.timeline = this;
                trackGroup.Initialize();
            }
            hasBeenInitialized = true;
            SaveRevertData();
        }

        public void SaveRevertData()
        {
            revertCache.Clear();
            foreach (var child in m_RecoverableObjects)
            {
                SaveRevertData(child as IRecoverableObject);
            }
        }

        private void Revert()
        {
            foreach (var element in revertCache)
            {
                Revert(element);
            }
            revertCache.Clear();

            mRevertInfoDic.Clear();
        }

        public void SaveRevertData(IRecoverableObject recoverable)
        {
            if (recoverable == null || recoverable.RuntimeRevertMode == RevertMode.Finalize)
            {
                return;
            }
            var ri = recoverable.CacheState();
            if (ri != null && ri.Length > 0)
            {
                mRevertInfoDic.Add(recoverable, ri);
                revertCache.AddRange(ri);
            }
        }

        public void Revert(RevertInfo element)
        {
            var behaviour = element.MonoBehaviour;
            if (behaviour == null)
                return;
            var timelineTrack = behaviour.timelineTrack;
            if (timelineTrack == null)
                return;
            var group = timelineTrack.trackGroup as ActorTrackGroup;
            if (group == null)
                return;
            element.Revert();
        }

        private List<float> GetMilestones(float time)
        {
            var milestoneTimes = new List<float>();
            milestoneTimes.Add(time);
            for (var i = 0; i < trackGroups.Count; i++)
            {
                var times = trackGroups[i].GetMilestones(RunningTime, time);
                for (var j = 0; j < times.Count; j++)
                    if (!milestoneTimes.Contains(times[j]))
                        milestoneTimes.Add(times[j]);
            }

            milestoneTimes.Sort();
            if (time < RunningTime) milestoneTimes.Reverse();

            return milestoneTimes;
        }

        private void PreparePlay()
        {
            if (!hasBeenInitialized)
                Initialize();
        }

        private void Resume()
        {
            for (var i = 0; i < trackGroups.Count; i++) trackGroups[i].Resume();
        }

        public void Recache()
        {
            if (state != TimelineState.Inactive)
            {
                var time = RunningTime;
                ExitPreviewMode();
                EnterPreviewMode();
                ScrubToTime(time);
            }
        }
        [SerializeField]
        private float duration = 10f; // Duration of timeline in seconds.

        public float playbackSpeed = 1f; // Multiplier for playback speed.

        public bool isLooping;

        private float m_RunningTime; // Running time of the timeline in seconds.

        public short GetFrameCount()
        {
            return TimelineUtility.TimeToFrame(RunningTime);
        }

        public TimelineState state { get; private set; }

        [NonSerialized]
        public bool hasBeenInitialized;

        public List<RevertInfo> revertCache = new();

        public GameObject sceneRoot;
#if UNITY_EDITOR
        [NonSerialized]
        public bool inGame;
#endif
        [NonSerialized]
        public List<int> trackScreenFitIDs = new();

        public Dictionary<IRecoverableObject, RevertInfo[]> mRevertInfoDic = new();

    }

    public delegate void TimelineHandler(Timeline sender, TimelineEventArgs e);

    public class TimelineEventArgs : EventArgs
    {
        public bool isFinished;
    }
}
