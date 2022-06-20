using System;
using System.Collections;
using System.Collections.Generic;
using AGE;
using UnityEngine;
using Sirenix.OdinInspector;
using Action = AGE.Action;

namespace CinemaDirector
{
    public partial class Cutscene : Action
    {
        [SerializeField]
        private bool isLooping = true;
        private CutsceneState state = CutsceneState.Inactive;
        [TableList]
        public List<TemplateObject> m_templateObjectList = new List<TemplateObject>();
        
        // Has the Cutscene been initialized yet.
        private bool hasBeenInitialized;
        
        // A list of all the Tracks and Items revert info, to revert states on Cutscene entering and exiting play mode.
        private List<RevertInfo> revertCache = new List<RevertInfo>();

        // Event fired when Cutscene's runtime reaches it's duration.
        public event CutsceneHandler CutsceneFinished;

        // Event fired when Cutscene has been paused.
        public event CutsceneHandler CutscenePaused;

        private GameObject Find(string name)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var go in rootGameObjects)
            {
                var obj = go.FindChildBFS(name);
                if (obj != null)
                {
                    return obj;
                }
            }
            return null;
        }

        private void RefreshTemplateObject()
        {
            gameObjects.Clear();
            foreach (var templateObject in m_templateObjectList)
            {
                if (templateObject.gameObject == null)
                {
                    templateObject.gameObject = Find(templateObject.templateObject.name);
                }
                gameObjects.Add(templateObject.gameObject);
            }
        }

        public void OnValidate()
        {

            RefreshTemplateObject();
            Dirty = true;
        }

        public override void OnUninitialize()
        {
            Stop();
        }

        public void Play()
        {
            if (state == CutsceneState.Inactive)
            {
                DirectorEvent.StartCoroutine.Invoke(FreshPlay);
            }
            else if (state == CutsceneState.Paused)
            {
                state = CutsceneState.PreviewPlaying;
                DirectorEvent.StartCoroutine.Invoke(UpdateCoroutine);
            }
        }

        private IEnumerator FreshPlay()
        {
            DirectorEvent.StartCoroutine.Invoke(PreparePlay);
            // Wait one frame.
            yield return null;

            state = CutsceneState.PreviewPlaying;
            DirectorEvent.StartCoroutine.Invoke(UpdateCoroutine);
        }

        public override void Pause()
        {
            if (state == CutsceneState.PreviewPlaying)
            {
                DirectorEvent.StopCoroutine.Invoke(UpdateCoroutine);
            }
            if (state == CutsceneState.PreviewPlaying || state == CutsceneState.Scrubbing)
            {
                foreach (TrackGroup trackGroup in Children)
                {
                    trackGroup.Pause();
                }
            }
            state = CutsceneState.Paused;

            if (CutscenePaused != null)
            {
                CutscenePaused(this, new CutsceneEventArgs());
            }
            base.Pause();
        }
        
        public override void Stop(bool forceStop = false)
        {
            RunningTime = 0f;
            
            foreach (TrackGroup trackGroup in Children)
            {
                trackGroup.Stop();
            }

            if (state != CutsceneState.Inactive)
                revert();

            if (state == CutsceneState.Playing)
            {
                DirectorEvent.StopCoroutine.Invoke(UpdateCoroutine);
                if (state == CutsceneState.PreviewPlaying && isLooping)
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
            RefreshTemplateObject();
            if (Application.isPlaying && state == CutsceneState.Playing)
            {
                RunningTime = CurrentTime;
            }
            else
            {
                var lastTime = RunningTime;
                foreach (TrackGroup group in Children)
                {
                    group.UpdateTrackGroup(RunningTime + deltaTime, deltaTime);
                }

                foreach (var track in tracks)
                {
                    if (!track.enabled)
                    {
                        continue;
                    }
                    track.started = true;
                }
                ForceUpdate(RunningTime + deltaTime);
                //RunningTime += deltaTime;
                UpdateTempObjectForPreview(lastTime, RunningTime);
            }
            DoSomethingWhilePlaying(RunningTime);
            if (state != CutsceneState.Scrubbing)
            {
                if (RunningTime >= Duration || RunningTime < 0f)
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

        public void PreviewPlay()
        {
            if (state == CutsceneState.Inactive)
            {
                EnterPreviewMode();
            }
            else if (state == CutsceneState.Paused)
            {
                Resume();
            }

            state = CutsceneState.PreviewPlaying;
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
                UpdateCutscene(deltaTime);
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
            UpdateCutscene(time - RunningTime);
        }

        /// <summary>
        /// Set the cutscene into an active state.
        /// </summary>
        public void EnterPreviewMode()
        {
            if (state == CutsceneState.Inactive)
            {
                Initialize();
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

        /// <summary>
        /// The duration of this cutscene in seconds.
        /// </summary>
        public float Duration
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }

        /// <summary>
        /// Returns true if this cutscene is currently playing.
        /// </summary>
        public CutsceneState State => state;
        

        /// <summary>
        /// The current running time of this cutscene in seconds. Values are restricted between 0 and duration.
        /// </summary>
        public float RunningTime
        {
            get
            {
                return time;
            }
            set
            {
                time = Mathf.Clamp(value, 0, length);
            }
        }

        public bool IsLooping
        {
            get { return isLooping; }
            set { isLooping = value; }
        }

        /// <summary>
        /// An important call to make before sampling the cutscene, to initialize all track groups 
        /// and save states of all actors/game objects.
        /// </summary>
        private void Initialize()
        {
            saveRevertData();
            
            foreach (TrackGroup directorObject in Children)
            {
                directorObject.Initialize();
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
            foreach (var mb in Children)
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

        private IEnumerator PreparePlay()
        {
            if (!hasBeenInitialized)
            {
                Initialize();
            }
            yield return null;
        }

        /// <summary>
        /// Couroutine to call UpdateCutscene while the cutscene is in the playing state.
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateCoroutine()
        {
            while (state == CutsceneState.PreviewPlaying)
            {
                UpdateCutscene(Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Prepare all track groups for resuming from pause.
        /// </summary>
        private void Resume()
        {
            foreach (TrackGroup group in Children)
            {
                group.Resume();
            }
        }

        public override DirectorObject CreateChild(DirectorObject directorObject = null)
        {
            return Create<TrackGroup>(this, "Action");
        }

        private void OnDestroy()
        {
            state = CutsceneState.Playing;
        }

        public void recache()
        {
            if (State != CutsceneState.Inactive)
            {
                float runningTime = RunningTime;
                ExitPreviewMode();
                EnterPreviewMode();
                ScrubToTime(runningTime);
            }
        }
    }

    public delegate void CutsceneHandler(Cutscene sender, CutsceneEventArgs e);

    public class CutsceneEventArgs : EventArgs
    {
        public CutsceneEventArgs()
        {
        }
    }
}
