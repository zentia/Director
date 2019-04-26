using CinemaDirector.Helpers;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    [ExecuteInEditMode, Serializable]
    public class Cutscene : MonoBehaviour, IOptimizable
    {
        [SerializeField]
        private float duration = 30f; // Duration of cutscene in seconds.
        
        [SerializeField]
        private float playbackSpeed = 1f; // Multiplier for playback speed.

        [SerializeField]
        private bool isSkippable = true;

        [SerializeField]
        private bool isLooping;

        [SerializeField]
        private bool canOptimize = true;

        [NonSerialized]
        private float runningTime = 0f; // Running time of the cutscene in seconds.

        private CutsceneState state = CutsceneState.Inactive;
        public static Camera camera;
        private static GameObject root;
       
        public static Transform Root
        {
            get
            {
                if (root == null)
                {
                    root = DirectorRuntimeHelper.FindSceneObject("CutsceneRoot");
                    if (root == null)
                    {
                        root = new GameObject("CutsceneRoot");
                    }
                }
                return root.transform;
            }
        }
        public static uint ID;

        public static float frameRate;

        public static int frame;
        // Keeps track of the previous time an update was made.
        //private float previousTime = 0;

        // Has the Cutscene been optimized yet.
        private bool hasBeenOptimized;

        // Has the Cutscene been initialized yet.
        private bool hasBeenInitialized;

        // The cache of Track Groups that this Cutscene contains.
        private TrackGroup[] trackGroupCache;

        // A list of all the Tracks and Items revert info, to revert states on Cutscene entering and exiting play mode.
        private List<RevertInfo> revertCache = new List<RevertInfo>();

        // Event fired when Cutscene's runtime reaches it's duration.
        public event CutsceneHandler CutsceneFinished;

        // Event fired when Cutscene has been paused.
        public event CutsceneHandler CutscenePaused;

        /// <summary>
        /// Optimizes the Cutscene by preparing all tracks and timeline items into a cache.
        /// Call this on scene load in most cases. (Avoid calling in edit mode).
        /// </summary>
        public void Optimize()
        {
            if (canOptimize)
            {
                trackGroupCache = GetTrackGroups();
                hasBeenOptimized = true;
            }
            foreach (TrackGroup tg in GetTrackGroups())
            {
                tg.Optimize();
            }
        }

        /// <summary>
        /// Plays/Resumes the cutscene from inactive/paused states using a Coroutine.
        /// </summary>
        public void Play()
        {
            if (state == CutsceneState.Inactive)
            {
                StartCoroutine(freshPlay());
            }
            else if (state == CutsceneState.Paused)
            {
                state = CutsceneState.Playing;
                StartCoroutine(updateCoroutine());
            }
        }

        private IEnumerator freshPlay()
        {
            yield return StartCoroutine(PreparePlay());
            // Wait one frame.
            yield return null;

            // Beging playing
            state = CutsceneState.Playing;
            StartCoroutine(updateCoroutine());
        }

        /// <summary>
        /// Pause the playback of this cutscene.
        /// </summary>
        public void Pause()
        {
            if (state == CutsceneState.Playing)
            {
                StopCoroutine("updateCoroutine");
            }
            if (state == CutsceneState.PreviewPlaying || state == CutsceneState.Playing || state == CutsceneState.Scrubbing)
            {
                foreach (TrackGroup trackGroup in GetTrackGroups())
                {
                    trackGroup.Pause();
                }
            }
            state = CutsceneState.Paused;

            if (CutscenePaused != null)
            {
                CutscenePaused(this, new CutsceneEventArgs());
            }
        }

        /// <summary>
        /// Skip the cutscene to the end and stop it
        /// </summary>
        public void Skip()
        {
            if (isSkippable)
            {
                SetRunningTime(Duration);
                state = CutsceneState.Inactive;
                Stop();
            }
        }
        
        public void Stop()
        {
            RunningTime = 0f;
            
            foreach (var trackGroup in GetTrackGroups())
            {
                trackGroup.Stop();
            }

            revert();

            if (state == CutsceneState.Playing)
            {
                StopCoroutine("updateCoroutine");
                if (state == CutsceneState.Playing && isLooping)
                {
                    state = CutsceneState.Inactive;
                    Play();
                }
                else
                {
                    state = CutsceneState.Inactive;
                }
            }
            else
            {
                state = CutsceneState.Inactive;
            }

            if (state == CutsceneState.Inactive)
            {
                if (CutsceneFinished != null)
                {
                    CutsceneFinished(this, new CutsceneEventArgs());
                }
            }
        }

        public void UpdateCutscene(float deltaTime)
        {
            if (deltaTime > 0)
            {
                frame++;
            }
            else if (deltaTime < 0)
            {
                frame--;
            }
            RunningTime += deltaTime * playbackSpeed;
            TrackGroup[] groups = GetTrackGroups();
            for (int i = 0; i < groups.Length; i++)
            {
                TrackGroup group = groups[i];
                group.UpdateTrackGroup(RunningTime, deltaTime * playbackSpeed);
            }
            DoSomethingWhilePlaying(RunningTime);
            if (state != CutsceneState.Scrubbing)
            {
                if (runningTime >= duration || runningTime < 0f)
                {
                    Stop();
                }
            }
        }

        private float DoTime = 0;
        private bool SomethingDone = false;
        public delegate void DoFunc();
        DoFunc DoFunction = null;
        public void DoSomethingWhilePlaying(float rTime)
        {
            if (!SomethingDone && rTime >= DoTime)
            {
                if (DoFunction != null)
                    DoFunction();
                SomethingDone = true;
            }
            
        }

        public void PlantingBomb(float time, DoFunc e)
        {
            DoTime = time;
            DoFunction += e;
        }

        /// <summary>
        /// Preview play readies the cutscene to be played in edit mode. Never use for runtime.
        /// This is necessary for playing the cutscene in edit mode.
        /// </summary>
        public void PreviewPlay()
        {
            if (state == CutsceneState.Inactive)
            {
                EnterPreviewMode();
            }
            else if (state == CutsceneState.Paused)
            {
                resume();
            }

            if (Application.isPlaying)
            {
                state = CutsceneState.Playing;
            }
            else
            {
                state = CutsceneState.PreviewPlaying;
#if UNITY_EDITOR

                if (!UnityEditor.AnimationMode.InAnimationMode())
                {
                    UnityEditor.AnimationMode.StartAnimationMode();
                }
#endif
            }
        }

        /// <summary>
        /// Play the cutscene from it's given running time to a new time
        /// </summary>
        /// <param name="newTime">The new time to make up for</param>
        public void ScrubToTime(float newTime)
        {
            float deltaTime = Mathf.Clamp(newTime, 0, Duration) - RunningTime;

            state = CutsceneState.Scrubbing;
            if (deltaTime != 0)
            {
                if (deltaTime > (1 / 30f))
                {
                    float prevTime = RunningTime;
                    foreach (float milestone in getMilestones(RunningTime + deltaTime))
                    {
                        float delta = milestone - prevTime;
                        UpdateCutscene(delta);
                        prevTime = milestone;
                    }
                }
                else
                {
                    UpdateCutscene(deltaTime);
                }
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// Set the cutscene to the state of a given new running time and do not proceed to play the cutscene
        /// </summary>
        /// <param name="time">The new running time to be set.</param>
        public void SetRunningTime(float time)
        {
            foreach (float milestone in getMilestones(time))
            {
                foreach (TrackGroup group in this.TrackGroups)
                {
                    group.SetRunningTime(milestone);
                }
            }

            RunningTime = time;
        }

        /// <summary>
        /// Set the cutscene into an active state.
        /// </summary>
        public void EnterPreviewMode()
        {
            if (state == CutsceneState.Inactive)
            {
                initialize();
                bake();
                SetRunningTime(RunningTime);
                state = CutsceneState.Paused;
            }
        }

        /// <summary>
        /// Set the cutscene into an inactive state.
        /// </summary>
        public void ExitPreviewMode()
        {
            Stop();
        }

        private void Start()
        {
           
        }
        protected void OnDestroy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Stop();
                if (root != null)
                {
                    Destroy(root);
                }
                return;
            }
#endif
            
            DirectorRuntimeHelper.DestroyRoot();
        }
#if UNITY_EDITOR
        public delegate void DirCallback(string path);
        public static void ForeachDir(string dir, DirCallback callback, bool ext = true)
        {
            dir = "Assets\\" + dir;
            if (Directory.Exists(dir))
            {
                DirectoryInfo directory = new DirectoryInfo(dir);
                FileInfo[] fileInfos = directory.GetFiles("*", SearchOption.AllDirectories);
                for (int i = 0; i < fileInfos.Length; i++)
                {
                    if (fileInfos[i].Name.EndsWith(".meta"))
                    {
                        continue;
                    }
                    if (!ext)
                    {
                        string path = fileInfos[i].DirectoryName + "\\" + Path.GetFileNameWithoutExtension(fileInfos[i].FullName);
                        callback(path.Substring(path.IndexOf(dir) + dir.Length + 1));
                    }
                    else
                    {
                        string path = fileInfos[i].FullName;
                        callback(path.Substring(path.IndexOf(dir) + dir.Length + 1));
                    }
                }
            }
        }
#endif
        /// <summary>
        /// Exit and Re-enter preview mode at the current running time.
        /// Important for Mecanim Previewing.
        /// </summary>
        public void Refresh()
        {
            if (state != CutsceneState.Inactive)
            {
                float tempTime = runningTime;
                Stop();
                EnterPreviewMode();
                SetRunningTime(tempTime);
            }
        }

        /// <summary>
        /// Bake all Track Groups who are Bakeable.
        /// This is necessary for things like mecanim previewing.
        /// </summary>
        private void bake()
        {
            if (Application.isEditor)
            {
                foreach (TrackGroup group in TrackGroups)
                {
                    if (group is IBakeable)
                    {
                        (group as IBakeable).Bake();
                    }
                }
            }
        }

        /// <summary>
        /// The duration of this cutscene in seconds.
        /// </summary>
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
                {
                    duration = 0.1f;
                }
            }
        }

        /// <summary>
        /// Returns true if this cutscene is currently playing.
        /// </summary>
        public CutsceneState State
        {
            get
            {
                return state;
            }
        }

        /// <summary>
        /// The current running time of this cutscene in seconds. Values are restricted between 0 and duration.
        /// </summary>
        public float RunningTime
        {
            get
            {
                return runningTime;
            }
            set
            {
                runningTime = Mathf.Clamp(value, 0, duration);
            }
        }

        /// <summary>
        /// Get all Track Groups in this Cutscene. Will return cache if possible.
        /// </summary>
        /// <returns></returns>
        public TrackGroup[] GetTrackGroups()
        {
            // Return the cache if possible
            if (hasBeenOptimized)
            {
                return trackGroupCache;
            }

            return TrackGroups;
        }

        /// <summary>
        /// Get all track groups in this cutscene.
        /// </summary>
        public TrackGroup[] TrackGroups
        {
            get
            {
                return GetComponentsInChildren<TrackGroup>();
            }
        }

        /// <summary>
        /// Get the director group of this cutscene.
        /// </summary>
        public DirectorGroup DirectorGroup
        {
            get
            {
                return GetComponentInChildren<DirectorGroup>();
            }
        }

        /// <summary>
        /// Cutscene state is used to determine if the cutscene is currently Playing, Previewing (In edit mode), paused, scrubbing or inactive.
        /// </summary>
        public enum CutsceneState
        {
            Inactive,
            Playing,
            PreviewPlaying,
            Scrubbing,
            Paused
        }

        /// <summary>
        /// Enable this if the Cutscene does not have TrackGroups added/removed during running.
        /// </summary>
        public bool CanOptimize
        {
            get { return canOptimize; }
            set { canOptimize = value; }
        }

        /// <summary>
        /// True if Cutscene can be skipped.
        /// </summary>
        public bool IsSkippable
        {
            get { return isSkippable; }
            set { isSkippable = value; }
        }

        /// <summary>
        /// Will the Cutscene loop upon completion.
        /// </summary>
        public bool IsLooping
        {
            get { return isLooping; }
            set { isLooping = value; }
        }

        /// <summary>
        /// An important call to make before sampling the cutscene, to initialize all track groups 
        /// and save states of all actors/game objects.
        /// </summary>
        private void initialize()
        {
            saveRevertData();
            
            // Initialize each track group.
            foreach (TrackGroup trackGroup in TrackGroups)
            {
                trackGroup.Initialize();
            }
            hasBeenInitialized = true;
        }

        /// <summary>
        /// Cache all the revert data.
        /// </summary>
        private void saveRevertData()
        {
            revertCache.Clear();
            // Build the cache of revert info.
            foreach (MonoBehaviour mb in this.GetComponentsInChildren<MonoBehaviour>())
            {
                IRevertable revertable = mb as IRevertable;
                if (revertable != null)
                {
                    RevertInfo[] ri = revertable.CacheState();
                    if(ri == null || ri.Length < 1)
                    {
                        Debug.Log(string.Format("Cinema Director tried to cache the state of {0}, but failed.", mb.name));
                    }
                    else
                    {
                        revertCache.AddRange(ri);
                    }
                }
            }
        }

        /// <summary>
        /// Revert all data that has been manipulated by the Cutscene.
        /// </summary>
        private void revert()
        {
            foreach (RevertInfo revertable in revertCache)
            {
                if (revertable != null)
                {
                    if ((revertable.EditorRevert == RevertMode.Revert && !Application.isPlaying) ||
                        (revertable.RuntimeRevert == RevertMode.Revert && Application.isPlaying))
                    {
                        revertable.Revert();
                    }
                }
            }
        }

        /// <summary>
        /// Get the milestones between the current running time and the given time.
        /// </summary>
        /// <param name="time">The time to progress towards</param>
        /// <returns>A list of times that state changes are made in the Cutscene.</returns>
        private List<float> getMilestones(float time)
        {
            // Create a list of ordered milestone times.
            List<float> milestoneTimes = new List<float>();
            milestoneTimes.Add(time);
            foreach (TrackGroup group in TrackGroups)
            {
                List<float> times = group.GetMilestones(RunningTime, time);
                foreach (float f in times)
                {
                    if (!milestoneTimes.Contains(f))
                        milestoneTimes.Add(f);
                }
            }

            milestoneTimes.Sort();
            if (time < RunningTime)
            {
                milestoneTimes.Reverse();
            }

            return milestoneTimes; 
        }

        private IEnumerator PreparePlay()
        {
            if (!hasBeenOptimized)
            {
                Optimize();
            }
            if (!hasBeenInitialized)
            {
                initialize();
            }
            yield return null;
        }

        /// <summary>
        /// Couroutine to call UpdateCutscene while the cutscene is in the playing state.
        /// </summary>
        /// <returns></returns>
        private IEnumerator updateCoroutine()
        {
            bool firstFrame = true;
            while (state == CutsceneState.Playing)
            {
                if(firstFrame)
                {
                    frame = 0;
                    firstFrame = false;
                }
                UpdateCutscene(Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Prepare all track groups for resuming from pause.
        /// </summary>
        private void resume()
        {
            foreach (TrackGroup group in this.TrackGroups)
            {
                group.Resume();
            }
        }
    }

    // Delegate for handling Cutscene Events
    public delegate void CutsceneHandler(Cutscene sender, CutsceneEventArgs e);

    /// <summary>
    /// Cutscene event arguments. Blank for now.
    /// </summary>
    public class CutsceneEventArgs : EventArgs
    {
        public CutsceneEventArgs()
        {
        }
    }
}
