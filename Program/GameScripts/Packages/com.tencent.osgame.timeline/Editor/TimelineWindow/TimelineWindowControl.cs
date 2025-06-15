#define ENABLE_PLAYABLE_PREVIEW

using System;
using UnityEngine;
using UnityEditor;
using TimelineEditor;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using Object = UnityEngine.Object;

using UnityEngine.Playables;
using UnityEngine.Animations;

using UnityEngine.Experimental.Animations;

using Unity.Profiling;

namespace TimelineEditorInternal
{
    internal class TimelineWindowControl : ITimelineWindowControl, ITimelineContextualResponder
    {
        class CandidateRecordingState : ITimelineRecordingState
        {
            public GameObject activeGameObject { get; private set; }
            public GameObject activeRootGameObject { get; private set; }
            public AnimationClip activeAnimationClip { get; private set; }
            public int currentFrame { get { return 0; } }

            public bool addZeroFrame { get { return false; } }

            public CandidateRecordingState(TimelineWindowState state, AnimationClip candidateClip)
            {
                activeGameObject = state.activeGameObject;
                activeRootGameObject = state.activeRootGameObject;
                activeAnimationClip = candidateClip;
            }

            public bool DiscardModification(PropertyModification modification)
            {
                return !AnimationMode.IsPropertyAnimated(modification.target, modification.propertyPath);
            }

            public void SaveCurve(TimelineWindowCurve curve)
            {
                Undo.RegisterCompleteObjectUndo(curve.clip, "Edit Candidate Curve");
                TimelineWindowUtility.SaveCurve(curve.clip, curve);
            }

            public void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride)
            {
                AnimationMode.AddCandidate(binding, propertyModification, keepPrefabOverride);
            }
        }

        enum RecordingStateMode
        {
            ManualKey,
            AutoKey
        }

        class RecordingState : ITimelineRecordingState
        {
            private TimelineWindowState m_State;
            private RecordingStateMode m_Mode;

            public GameObject activeGameObject { get { return m_State.activeGameObject; } }
            public GameObject activeRootGameObject { get { return m_State.activeRootGameObject; } }
            public AnimationClip activeAnimationClip { get { return m_State.activeAnimationClip; } }
            public int currentFrame { get { return m_State.currentFrame; } }

            public bool addZeroFrame { get { return (m_Mode == RecordingStateMode.AutoKey); } }
            public bool addPropertyModification { get { return m_State.previewing; } }

            public RecordingState(TimelineWindowState state, RecordingStateMode mode)
            {
                m_State = state;
                m_Mode = mode;
            }

            public bool DiscardModification(PropertyModification modification)
            {
                return false;
            }

            public void SaveCurve(TimelineWindowCurve curve)
            {
                m_State.SaveCurve(curve.clip, curve);
            }

            public void AddPropertyModification(EditorCurveBinding binding, PropertyModification propertyModification, bool keepPrefabOverride)
            {
                AnimationMode.AddPropertyModification(binding, propertyModification, keepPrefabOverride);
            }
        }

        [Flags]
        enum ResampleFlags
        {
            None                = 0,

            RebuildGraph        = 1 << 0,
            RefreshViews        = 1 << 1,
            FlushUndos          = 1 << 2,

            Default             = RefreshViews | FlushUndos
        }

        private static bool HasFlag(ResampleFlags flags, ResampleFlags flag)
        {
            return (flags & flag) != 0;
        }

        [SerializeField] private TimelineKeyTime m_Time;

        [NonSerialized] private float m_PreviousUpdateTime;

        [NonSerialized] public TimelineWindowState state;
        public TimeEditor animEditor { get { return state.animEditor; } }

        [SerializeField] private AnimationClip m_CandidateClip;
        [SerializeField] private AnimationClip m_DefaultPose;

        [SerializeField] private AnimationModeDriver m_Driver;
        [SerializeField] private AnimationModeDriver m_CandidateDriver;

        private PlayableGraph m_Graph;
        private Playable m_GraphRoot;
        private AnimationClipPlayable m_ClipPlayable;
        private AnimationClipPlayable m_CandidateClipPlayable;
        private AnimationClipPlayable m_DefaultPosePlayable;
        private bool m_UsesPostProcessComponents = false;
        HashSet<Object> m_ObjectsModifiedDuringAnimationMode = new HashSet<Object>();

        private static ProfilerMarker s_ResampleAnimationMarker = new ProfilerMarker("AnimationWindowControl.ResampleAnimation");

        [NonSerialized] private float m_LastTime = -1.0f;
        [NonSerialized] private bool m_EnableFireEvent = false;

        public override void OnEnable()
        {
            base.OnEnable();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void OnDisable()
        {
            StopPreview();
            StopPlayback();

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        public void OnDestroy()
        {
            if (m_Driver != null)
                DestroyImmediate(m_Driver);
        }

        public override void OnSelectionChanged()
        {
            // Set back time at beginning and stop recording.
            if (state != null)
                m_Time = TimelineKeyTime.Time(0f, state.frameRate);

            StopPreview();
            StopPlayback();
            m_LastTime = -1.0f;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.ExitingEditMode)
            {
                StopPreview();
                StopPlayback();
            }
        }

        public override TimelineKeyTime time
        {
            get
            {
                return m_Time;
            }
        }

        public override void GoToTime(float time)
        {
            SetCurrentTime(time);
        }

        public override void GoToFrame(int frame)
        {
            SetCurrentFrame(frame);
        }

        public override void StartScrubTime()
        {
            // nothing to do...
        }

        public override void ScrubTime(float time)
        {
            SetCurrentTime(time);
        }

        public override void EndScrubTime()
        {
            // nothing to do...
        }

        public override void GoToPreviousFrame()
        {
            SetCurrentFrame(time.frame - 1);
        }

        public override void GoToNextFrame()
        {
            SetCurrentFrame(time.frame + 1);
        }

        public override void GoToPreviousKeyframe()
        {
            List<TimelineWindowCurve> curves = (state.showCurveEditor && state.activeCurves.Count > 0) ? state.activeCurves : state.allCurves;

            float newTime = TimelineWindowUtility.GetPreviousKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, TimelineWindowState.SnapMode.SnapToFrame));
        }

        public void GoToPreviousKeyframe(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = TimelineWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return;

            List<TimelineWindowCurve> curves = new List<TimelineWindowCurve>();
            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                TimelineWindowCurve curve = state.allCurves[i];
                if (Array.Exists(bindings, binding => curve.binding.Equals(binding)))
                    curves.Add(curve);
            }

            float newTime = TimelineWindowUtility.GetPreviousKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, TimelineWindowState.SnapMode.SnapToFrame));

            state.Repaint();
        }

        public override void GoToNextKeyframe()
        {
            List<TimelineWindowCurve> curves = (state.showCurveEditor && state.activeCurves.Count > 0) ? state.activeCurves : state.allCurves;

            float newTime = TimelineWindowUtility.GetNextKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, TimelineWindowState.SnapMode.SnapToFrame));
        }

        public void GoToNextKeyframe(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = TimelineWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return;

            List<TimelineWindowCurve> curves = new List<TimelineWindowCurve>();
            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                TimelineWindowCurve curve = state.allCurves[i];
                if (Array.Exists(bindings, binding => curve.binding.Equals(binding)))
                    curves.Add(curve);
            }

            float newTime = TimelineWindowUtility.GetNextKeyframeTime(curves.ToArray(), time.time, state.clipFrameRate);
            SetCurrentTime(state.SnapToFrame(newTime, TimelineWindowState.SnapMode.SnapToFrame));

            state.Repaint();
        }

        public override void GoToFirstKeyframe()
        {
            if (state.activeAnimationClip)
                SetCurrentTime(state.activeAnimationClip.startTime);
        }

        public override void GoToLastKeyframe()
        {
            if (state.activeAnimationClip)
                SetCurrentTime(state.activeAnimationClip.stopTime);
        }

        private void SnapTimeToFrame()
        {
            float newTime = state.FrameToTime(time.frame);
            SetCurrentTime(newTime);
        }

        private void SetCurrentTime(float value)
        {
            if (!Mathf.Approximately(value, time.time))
            {
                m_Time = TimelineKeyTime.Time(value, state.frameRate);
                StartPreview();
                ClearCandidates();
                ResampleAnimation();
            }
        }

        private void SetCurrentFrame(int value)
        {
            if (value != time.frame)
            {
                m_Time = TimelineKeyTime.Frame(value, state.frameRate);
                StartPreview();
                ClearCandidates();
                ResampleAnimation();
            }
        }

        public override bool canPlay
        {
            get
            {
                return canPreview;
            }
        }

        public override bool playing
        {
            get
            {
                return AnimationMode.InAnimationPlaybackMode() && previewing;
            }
        }

        public override bool StartPlayback()
        {
            if (!canPlay)
                return false;

            if (!playing)
            {
                AnimationMode.StartAnimationPlaybackMode();

                m_PreviousUpdateTime = Time.realtimeSinceStartup;

                // Auto-Preview when start playing
                StartPreview();
                ClearCandidates();
            }

            return true;
        }

        public override void StopPlayback()
        {
            if (AnimationMode.InAnimationPlaybackMode())
            {
                AnimationMode.StopAnimationPlaybackMode();

                // Snap to frame when playing stops
                SnapTimeToFrame();
            }
        }

        public override bool PlaybackUpdate()
        {
            if (!playing)
                return false;

            float deltaTime = Time.realtimeSinceStartup - m_PreviousUpdateTime;
            m_PreviousUpdateTime = Time.realtimeSinceStartup;

            float newTime = time.time + deltaTime;

            // looping
            if (newTime > state.maxTime)
                newTime = state.minTime;

            m_Time = TimelineKeyTime.Time(Mathf.Clamp(newTime, state.minTime, state.maxTime), state.frameRate);

            m_EnableFireEvent = true;
            ResampleAnimation();
            m_EnableFireEvent = false;
            m_LastTime = time.time;

            return true;
        }

        public override bool canPreview
        {
            get
            {
                if (!state.selection.canPreview)
                    return false;

                var driver = GetAnimationModeDriverNoAlloc();

                return (driver != null && AnimationMode.InAnimationMode(driver)) || !AnimationMode.InAnimationMode();
            }
        }

        public override bool previewing
        {
            get
            {
                var driver = GetAnimationModeDriverNoAlloc();
                if (driver == null)
                    return false;

                return AnimationMode.InAnimationMode(driver);
            }
        }

        public override bool StartPreview()
        {
            if (previewing)
                return true;

            if (!canPreview)
                return false;

            AnimationMode.StartAnimationMode(GetAnimationModeDriver());
            TimelinePropertyContextualMenu.Instance.SetResponder(this);
            Undo.postprocessModifications += PostprocessAnimationRecordingModifications;
            PrefabUtility.allowRecordingPrefabPropertyOverridesFor += AllowRecordingPrefabPropertyOverridesFor;
            DestroyGraph();
            CreateCandidateClip();

            //If a hierarchy was created and array reorder happen in the inspector prior
            //to the preview being started we will need to ensure that the display name
            //reflects the binding path on an array element.
            state.UpdateCurvesDisplayName();

            IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
            m_UsesPostProcessComponents = previewComponents != null && previewComponents.Length > 0;
            if (previewComponents != null)
            {
                // Animation preview affects inspector values, so make sure we ignore constrain proportions
                ConstrainProportionsTransformScale.m_IsAnimationPreview = true;
                foreach (var component in previewComponents)
                {
                    component.StartPreview();
                }
            }

            return true;
        }

        public override void StopPreview()
        {
            if (previewing)
                OnExitingAnimationMode();

            StopPlayback();
            StopRecording();

            ConstrainProportionsTransformScale.m_IsAnimationPreview = false;

            ClearCandidates();
            DestroyGraph();
            DestroyCandidateClip();

            AnimationMode.StopAnimationMode(GetAnimationModeDriver());

            // reset responder only if we have set it
            if (TimelinePropertyContextualMenu.Instance.IsResponder(this))
            {
                TimelinePropertyContextualMenu.Instance.SetResponder(null);
            }

            if (m_UsesPostProcessComponents)
            {
                IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
                if (previewComponents != null)
                {
                    foreach (var component in previewComponents)
                    {
                        component.StopPreview();
                    }

                    if (!Application.isPlaying)
                    {
                        var animator = state.activeAnimationPlayer as Animator;
                        if (animator != null)
                        {
                            animator.UnbindAllHandles();
                        }
                    }
                }

                m_UsesPostProcessComponents = false;
            }
        }

        public override bool canRecord
        {
            get
            {
                if (!state.selection.canRecord)
                    return false;

                return canPreview;
            }
        }

        public override bool recording
        {
            get
            {
                if (previewing)
                    return AnimationMode.InAnimationRecording();
                return false;
            }
        }

        public override bool StartRecording(Object targetObject)
        {
            return StartRecording();
        }

        private bool StartRecording()
        {
            if (recording)
                return true;

            if (!canRecord)
                return false;

            if (StartPreview())
            {
                AnimationMode.StartAnimationRecording();
                ClearCandidates();
                return true;
            }

            return false;
        }

        public override void StopRecording()
        {
            if (recording)
            {
                AnimationMode.StopAnimationRecording();
            }
        }

        private void StartCandidateRecording()
        {
            AnimationMode.StartCandidateRecording(GetCandidateDriver());
        }

        private void StopCandidateRecording()
        {
            AnimationMode.StopCandidateRecording();
        }

        private void DestroyGraph()
        {
            if (!m_Graph.IsValid())
                return;

            m_Graph.Destroy();
            m_GraphRoot = Playable.Null;
        }

        private void RebuildGraph(Animator animator)
        {
            DestroyGraph();

            m_Graph = PlayableGraph.Create("PreviewGraph");
            m_Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            m_ClipPlayable = AnimationClipPlayable.Create(m_Graph, state.activeAnimationClip);
            m_ClipPlayable.SetOverrideLoopTime(true);
            m_ClipPlayable.SetLoopTime(false);
            m_ClipPlayable.SetApplyFootIK(false);

            m_CandidateClipPlayable = AnimationClipPlayable.Create(m_Graph, m_CandidateClip);
            m_CandidateClipPlayable.SetApplyFootIK(false);

            IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
            bool requiresDefaultPose = previewComponents != null && previewComponents.Length > 0;
            int nInputs = requiresDefaultPose ? 3 : 2;

            // Create a layer mixer if necessary, we'll connect playable nodes to it after having populated AnimationStream.
            AnimationLayerMixerPlayable mixer = AnimationLayerMixerPlayable.Create(m_Graph, nInputs);
            m_GraphRoot = (Playable)mixer;

            // Populate custom playable preview graph.
            if (previewComponents != null)
            {
                foreach (var component in previewComponents)
                {
                    m_GraphRoot = component.BuildPreviewGraph(m_Graph, m_GraphRoot);
                }
            }

            // Finish hooking up mixer.
            int inputIndex = 0;

            if (requiresDefaultPose)
            {
                AnimationMode.RevertPropertyModificationsForGameObject(state.activeRootGameObject);

                EditorCurveBinding[] streamBindings = AnimationUtility.GetAnimationStreamBindings(state.activeRootGameObject);

                m_DefaultPose = new AnimationClip() { name = "DefaultPose" };

                TimelineWindowUtility.CreateDefaultCurves(state, m_DefaultPose, streamBindings);

                m_DefaultPosePlayable = AnimationClipPlayable.Create(m_Graph, m_DefaultPose);
                m_DefaultPosePlayable.SetApplyFootIK(false);

                mixer.ConnectInput(inputIndex++, m_DefaultPosePlayable, 0, 1.0f);
            }

            mixer.ConnectInput(inputIndex++, m_ClipPlayable, 0, 1.0f);
            mixer.ConnectInput(inputIndex++, m_CandidateClipPlayable, 0, 1.0f);

            if (animator.applyRootMotion)
            {
                var motionX = AnimationMotionXToDeltaPlayable.Create(m_Graph);
                motionX.SetAbsoluteMotion(true);
                motionX.SetInputWeight(0, 1.0f);

                m_Graph.Connect(m_GraphRoot, 0, motionX, 0);

                m_GraphRoot = (Playable)motionX;
            }

            var output = AnimationPlayableOutput.Create(m_Graph, "ouput", animator);
            output.SetSourcePlayable(m_GraphRoot);
            output.SetWeight(0.0f);
        }

        private IAnimationWindowPreview[] FetchPostProcessComponents()
        {
            if (state.activeRootGameObject != null)
            {
                return state.activeRootGameObject.GetComponents<IAnimationWindowPreview>();
            }

            return null;
        }

        public override void ResampleAnimation()
        {
            ResampleAnimation(ResampleFlags.Default);
        }

        private void ResampleAnimation(ResampleFlags flags)
        {
            if (state.disabled)
                return;

            if (previewing == false)
                return;
            if (canPreview == false)
                return;

            s_ResampleAnimationMarker.Begin();
            if (state.activeAnimationClip != null)
            {
                var animationPlayer = state.activeAnimationPlayer;
#if ENABLE_PLAYABLE_PREVIEW
                bool usePlayableGraph = animationPlayer is Animator;
#else
                bool usePlayableGraph = false;
#endif

                if (usePlayableGraph)
                {
                    var isValidGraph = m_Graph.IsValid();
                    if (isValidGraph)
                    {
                        var playableOutput = (AnimationPlayableOutput)m_Graph.GetOutput(0);
                        isValidGraph = playableOutput.GetTarget() == (Animator)animationPlayer;
                    }

                    if (HasFlag(flags, ResampleFlags.RebuildGraph) || !isValidGraph)
                    {
                        RebuildGraph((Animator)animationPlayer);
                    }
                }

                AnimationMode.BeginSampling();

                if (HasFlag(flags, ResampleFlags.FlushUndos))
                    Undo.FlushUndoRecordObjects();

                if (usePlayableGraph)
                {
                    if (m_UsesPostProcessComponents)
                    {
                        IAnimationWindowPreview[] previewComponents = FetchPostProcessComponents();
                        if (previewComponents != null)
                        {
                            foreach (var component in previewComponents)
                            {
                                component.UpdatePreviewGraph(m_Graph);
                            }
                        }
                    }

                    if (!m_CandidateClip.empty)
                        AnimationMode.AddCandidates(state.activeRootGameObject, m_CandidateClip);

                    m_ClipPlayable.SetSampleRate(playing ? -1 : state.activeAnimationClip.frameRate);

                    AnimationMode.SamplePlayableGraph(m_Graph, 0, time.time);

                    // This will cover euler/quaternion matching in basic playable graphs only (animation clip + candidate clip).
                    AnimationUtility.SampleEulerHint(state.activeRootGameObject, state.activeAnimationClip, time.time, WrapMode.Clamp);
                    if (!m_CandidateClip.empty)
                        AnimationUtility.SampleEulerHint(state.activeRootGameObject, m_CandidateClip, time.time, WrapMode.Clamp);
                }
                else
                {
                    AnimationMode.SampleAnimationClip(state.activeRootGameObject, state.activeAnimationClip, time.time);
                    if (m_EnableFireEvent && m_LastTime > 0)
                    {
                        FireEvent(state.activeRootGameObject, state.activeAnimationClip, m_LastTime, time.time);
                    }

                    if (!m_CandidateClip.empty)
                        AnimationMode.SampleCandidateClip(state.activeRootGameObject, m_CandidateClip, 0f);
                }

                AnimationMode.EndSampling();

                if (HasFlag(flags, ResampleFlags.RefreshViews))
                {
                    SceneView.RepaintAll();
                    InspectorWindow.RepaintAllInspectors();

                    // Particle editor needs to be manually repainted to refresh the animated properties
                    var particleSystemWindow = ParticleSystemWindow.GetInstance();
                    if (particleSystemWindow)
                        particleSystemWindow.Repaint();
                }
            }
            s_ResampleAnimationMarker.End();
        }

        private AnimationModeDriver GetAnimationModeDriver()
        {
            if (m_Driver == null)
            {
                m_Driver = CreateInstance<AnimationModeDriver>();
                m_Driver.hideFlags = HideFlags.HideAndDontSave;
                m_Driver.name = "AnimationWindowDriver";
                m_Driver.isKeyCallback += (Object target, string propertyPath) =>
                {
                    if (AnimationMode.IsPropertyAnimated(target, propertyPath))
                    {
                        var modification = new PropertyModification();
                        modification.target = target;
                        modification.propertyPath = propertyPath;

                        return KeyExists(new PropertyModification[] {modification});
                    }

                    return false;
                };
            }

            return m_Driver;
        }

        private AnimationModeDriver GetAnimationModeDriverNoAlloc()
        {
            return m_Driver;
        }

        private AnimationModeDriver GetCandidateDriver()
        {
            if (m_CandidateDriver == null)
            {
                m_CandidateDriver = CreateInstance<AnimationModeDriver>();
                m_CandidateDriver.name = "AnimationWindowCandidateDriver";
            }

            return m_CandidateDriver;
        }

        private bool AllowRecordingPrefabPropertyOverridesFor(UnityEngine.Object componentOrGameObject)
        {
            if (componentOrGameObject == null)
                throw new ArgumentNullException(nameof(componentOrGameObject));

            GameObject inputGameObject = null;
            if (componentOrGameObject is Component)
            {
                inputGameObject = ((Component)componentOrGameObject).gameObject;
            }
            else if (componentOrGameObject is GameObject)
            {
                inputGameObject = (GameObject)componentOrGameObject;
            }
            else
            {
                return true;
            }

            var rootOfAnimation = state.activeRootGameObject;
            if (rootOfAnimation == null)
                return true;

            // If the input object is a child of the current root of animation then disallow recording of prefab property overrides
            // since the input object is currently being setup for animation recording
            return inputGameObject.transform.IsChildOf(rootOfAnimation.transform) == false;
        }

        void OnExitingAnimationMode()
        {
            Undo.postprocessModifications -= PostprocessAnimationRecordingModifications;
            PrefabUtility.allowRecordingPrefabPropertyOverridesFor -= AllowRecordingPrefabPropertyOverridesFor;

            // Ensures Prefab instance overrides are recorded for properties that was changed while in AnimationMode
            foreach (var obj in m_ObjectsModifiedDuringAnimationMode)
            {
                if (obj != null)
                    EditorUtility.SetDirty(obj);
            }

            m_ObjectsModifiedDuringAnimationMode.Clear();
        }

        private UndoPropertyModification[] PostprocessAnimationRecordingModifications(UndoPropertyModification[] modifications)
        {
            //Fix for case 751009: The animationMode can be changed outside the AnimationWindow, and callbacks needs to be unregistered.
            if (!AnimationMode.InAnimationMode(GetAnimationModeDriver()))
            {
                OnExitingAnimationMode();
                return modifications;
            }

            if (recording)
                modifications = ProcessAutoKey(modifications);
            else if (previewing)
                modifications = RegisterCandidates(modifications);

            RefreshDisplayNamesOnArrayTopologicalChange(modifications);

            // Only resample when playable graph has been customized with post process nodes.
            if (m_UsesPostProcessComponents)
                ResampleAnimation(ResampleFlags.None);

            foreach (var mod in modifications)
            {
                m_ObjectsModifiedDuringAnimationMode.Add(mod.currentValue.target);
            }

            return modifications;
        }

        private void RefreshDisplayNamesOnArrayTopologicalChange(UndoPropertyModification[] modifications)
        {
            if (modifications.Length >= 2)
            {
                if (modifications[0].currentValue.propertyPath.EndsWith("]") &&
                    modifications[0].currentValue.propertyPath.Contains(".Array.data[") &&
                    modifications[1].currentValue.propertyPath.EndsWith("]") &&
                    modifications[1].currentValue.propertyPath.Contains(".Array.data["))
                {
                    //Array reordering might affect curves display name
                    state.UpdateCurvesDisplayName();
                }
                else if (modifications[0].currentValue.propertyPath.EndsWith(".Array.size") &&
                         Convert.ToInt64(modifications[0].currentValue.value) <
                         Convert.ToInt64(modifications[0].previousValue.value))
                {
                    //Array shrinking might affect curves display name
                    state.UpdateCurvesDisplayName();
                }
            }
        }

        private UndoPropertyModification[] ProcessAutoKey(UndoPropertyModification[] modifications)
        {
            BeginKeyModification();

            RecordingState recordingState = new RecordingState(state, RecordingStateMode.AutoKey);
            UndoPropertyModification[] discardedModifications = AnimationRecording.Process(recordingState, modifications);

            EndKeyModification();

            return discardedModifications;
        }

        private UndoPropertyModification[] RegisterCandidates(UndoPropertyModification[] modifications)
        {
            bool hasCandidates = AnimationMode.IsRecordingCandidates();

            if (!hasCandidates)
                StartCandidateRecording();

            CandidateRecordingState recordingState = new CandidateRecordingState(state, m_CandidateClip);
            UndoPropertyModification[] discardedModifications = AnimationRecording.Process(recordingState, modifications);

            // No modifications were added to the candidate clip, stop recording candidates.
            if (!hasCandidates && discardedModifications.Length == modifications.Length)
                StopCandidateRecording();

            // Make sure inspector is repainted after adding new candidates to get appropriate feedback.
            InspectorWindow.RepaintAllInspectors();

            return discardedModifications;
        }

        private void RemoveFromCandidates(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = TimelineWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, m_CandidateClip);
            if (bindings.Length == 0)
                return;

            // Remove entry from candidate clip.
            Undo.RegisterCompleteObjectUndo(m_CandidateClip, "Edit Candidate Curve");

            for (int i = 0; i < bindings.Length; ++i)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.isPPtrCurve)
                    AnimationUtility.SetObjectReferenceCurve(m_CandidateClip, binding, null);
                else
                    AnimationUtility.SetEditorCurve(m_CandidateClip, binding, null);
            }

            // Clear out candidate clip if it's empty.
            if (AnimationUtility.GetCurveBindings(m_CandidateClip).Length == 0 && AnimationUtility.GetObjectReferenceCurveBindings(m_CandidateClip).Length == 0)
                ClearCandidates();
        }

        private void CreateCandidateClip()
        {
            m_CandidateClip = new AnimationClip();
            m_CandidateClip.name = "CandidateClip";
        }

        private void DestroyCandidateClip()
        {
            m_CandidateClip = null;
        }

        public override void ClearCandidates()
        {
            StopCandidateRecording();

            if (m_CandidateClip != null)
                m_CandidateClip.ClearCurves();
        }

        public override void ProcessCandidates()
        {
            BeginKeyModification();

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(m_CandidateClip);
            EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(m_CandidateClip);

            List<TimelineWindowCurve> curves = new List<TimelineWindowCurve>();

            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                TimelineWindowCurve curve = state.allCurves[i];
                EditorCurveBinding remappedBinding = TimelineEditor.RotationCurveInterpolation.RemapAnimationBindingForRotationCurves(curve.binding, m_CandidateClip);
                if (Array.Exists(bindings, binding => remappedBinding.Equals(binding)) || Array.Exists(objectCurveBindings, binding => remappedBinding.Equals(binding)))
                    curves.Add(curve);
            }

            TimelineWindowUtility.AddKeyframes(state, curves, time);

            EndKeyModification();

            ClearCandidates();
        }

        private List<TimelineWindowKeyframe> GetKeys(PropertyModification[] modifications)
        {
            var keys = new List<TimelineWindowKeyframe>();

            EditorCurveBinding[] bindings = TimelineWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return keys;

            for (int i = 0; i < state.allCurves.Count; ++i)
            {
                TimelineWindowCurve curve = state.allCurves[i];
                if (Array.Exists(bindings, binding => curve.binding.Equals(binding)))
                {
                    int keyIndex = curve.GetKeyframeIndex(state.time);
                    if (keyIndex >= 0)
                    {
                        keys.Add(curve.m_Keyframes[keyIndex]);
                    }
                }
            }

            return keys;
        }

        public bool IsAnimatable(PropertyModification[] modifications)
        {
            for (int i = 0; i < modifications.Length; ++i)
            {
                var modification = modifications[i];
                if (TimelineWindowUtility.PropertyIsAnimatable(modification.target, modification.propertyPath, state.activeRootGameObject))
                    return true;
            }

            return false;
        }

        public bool IsEditable(Object targetObject)
        {
            if (state.selection.disabled)
                return false;

            if (previewing == false)
                return false;

            GameObject gameObject = null;
            if (targetObject is Component)
                gameObject = ((Component)targetObject).gameObject;
            else if (targetObject is GameObject)
                gameObject = (GameObject)targetObject;

            if (gameObject != null)
            {
                Component animationPlayer = TimelineWindowUtility.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
                if (state.selection.animationPlayer == animationPlayer)
                {
                    return state.selection.animationIsEditable;
                }
            }

            return false;
        }

        public bool KeyExists(PropertyModification[] modifications)
        {
            return (GetKeys(modifications).Count > 0);
        }

        public bool CandidateExists(PropertyModification[] modifications)
        {
            if (!HasAnyCandidates())
                return false;

            for (int i = 0; i < modifications.Length; ++i)
            {
                var modification = modifications[i];
                if (AnimationMode.IsPropertyCandidate(modification.target, modification.propertyPath))
                    return true;
            }

            return false;
        }

        public bool CurveExists(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = TimelineWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return false;

            EditorCurveBinding[] clipBindings = AnimationUtility.GetCurveBindings(state.activeAnimationClip);
            if (clipBindings.Length == 0)
                return false;

            if (Array.Exists(bindings, binding => Array.Exists(clipBindings, clipBinding => clipBinding.Equals(binding))))
                return true;

            EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(state.activeAnimationClip);
            if (objectCurveBindings.Length == 0)
                return false;

            return Array.Exists(objectCurveBindings, binding => Array.Exists(clipBindings, clipBinding => clipBinding.Equals(binding)));
        }

        public bool HasAnyCandidates()
        {
            return !m_CandidateClip.empty;
        }

        public bool HasAnyCurves()
        {
            return (state.allCurves.Count > 0);
        }

        public void AddKey(SerializedProperty property)
        {
            AddKey(TimelineWindowUtility.SerializedPropertyToPropertyModifications(property));
        }

        public void AddKey(PropertyModification[] modifications)
        {
            var undoModifications = new UndoPropertyModification[modifications.Length];
            for (int i = 0; i < modifications.Length; ++i)
            {
                var modification = modifications[i];
                undoModifications[i].previousValue = modification;
                undoModifications[i].currentValue = modification;
            }

            BeginKeyModification();

            var recordingState = new RecordingState(state, RecordingStateMode.ManualKey);
            AnimationRecording.Process(recordingState, undoModifications);

            EndKeyModification();

            RemoveFromCandidates(modifications);

            ResampleAnimation();
            state.Repaint();
        }

        public void RemoveKey(SerializedProperty property)
        {
            RemoveKey(TimelineWindowUtility.SerializedPropertyToPropertyModifications(property));
        }

        public void RemoveKey(PropertyModification[] modifications)
        {
            BeginKeyModification();

            List<TimelineWindowKeyframe> keys = GetKeys(modifications);
            state.DeleteKeys(keys);

            RemoveFromCandidates(modifications);

            EndKeyModification();

            ResampleAnimation();
            state.Repaint();
        }

        public void RemoveCurve(SerializedProperty property)
        {
            RemoveCurve(TimelineWindowUtility.SerializedPropertyToPropertyModifications(property));
        }

        public void RemoveCurve(PropertyModification[] modifications)
        {
            EditorCurveBinding[] bindings = TimelineWindowUtility.PropertyModificationsToEditorCurveBindings(modifications, state.activeRootGameObject, state.activeAnimationClip);
            if (bindings.Length == 0)
                return;

            BeginKeyModification();

            Undo.RegisterCompleteObjectUndo(state.activeAnimationClip, "Remove Curve");

            for (int i = 0; i < bindings.Length; ++i)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.isPPtrCurve)
                    AnimationUtility.SetObjectReferenceCurve(state.activeAnimationClip, binding, null);
                else
                    AnimationUtility.SetEditorCurve(state.activeAnimationClip, binding, null);
            }

            EndKeyModification();

            RemoveFromCandidates(modifications);

            ResampleAnimation();
            state.Repaint();
        }

        public void AddCandidateKeys()
        {
            ProcessCandidates();

            ResampleAnimation();
            state.Repaint();
        }

        public void AddAnimatedKeys()
        {
            BeginKeyModification();

            TimelineWindowUtility.AddKeyframes(state, state.allCurves, time);
            ClearCandidates();

            EndKeyModification();

            ResampleAnimation();
            state.Repaint();
        }

        private void BeginKeyModification()
        {
            if (animEditor != null)
                animEditor.BeginKeyModification();
        }

        private void EndKeyModification()
        {
            if (animEditor != null)
                animEditor.EndKeyModification();
        }

        private void FireEvent(GameObject gameObject, AnimationClip clip, float lastTime, float curTime)
        {
            var events = clip.events;
            if (null == events)
                return;

            foreach (var clipEvent in clip.events)
            {
                if (null == clipEvent
                    || string.IsNullOrEmpty(clipEvent.functionName))
                {
                    continue;
                }

                var eventTime = clipEvent.time;

                bool enableFire = false;
                if (lastTime < curTime)
                {
                    if (lastTime <= eventTime && eventTime < curTime)
                    {
                        enableFire = true;
                    }
                }
                else if (lastTime > curTime)
                {
                    enableFire = eventTime == 0 || eventTime > lastTime;
                }

                if (enableFire)
                {
                    FireEvent(gameObject, clipEvent);
                }
            }
        }

        private void FireEvent(GameObject gameObject, AnimationEvent evt)
        {
            foreach (var behaviour in gameObject.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null)
                    continue;

                MethodInfo method = null;
                try
                {
                    method = behaviour.GetType().GetMethod(evt.functionName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                catch (AmbiguousMatchException)
                {
                }

                if (method == null)
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 1)
                {
                    continue;
                }

                object[] argumens = null;
                if (parameters.Length == 1)
                {
                    Type parameterType = parameters[0].ParameterType;

                    if (parameterType == typeof(string))
                    {
                        argumens = new[] { (object)evt.stringParameter };
                    }
                    else if (parameterType == typeof(float))
                    {
                        argumens = new[] { (object)evt.floatParameter };
                    }
                    else if (parameterType == typeof(int))
                    {
                        argumens = new[] { (object)evt.intParameter };
                    }
                    else if (parameterType == typeof(UnityEngine.Object)
                             || parameterType.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        argumens = new[] { (object)evt.objectReferenceParameter };
                    }
                }
                try
                {
                    method.Invoke(behaviour, argumens);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
