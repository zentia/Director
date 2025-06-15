using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TimelineEditor;
using System.Collections.Generic;
using TimelineRuntime;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    [System.Serializable]
    internal class TimelineWindowState : ScriptableObject, TimelineEditor.ICurveEditorState
    {
        public enum RefreshType
        {
            None = 0,
            CurvesOnly = 1,
            Everything = 2
        }

        public enum SnapMode
        {
            Disabled = 0,
            SnapToFrame = 1,
            [Obsolete("SnapToClipFrame has been made redundant with SnapToFrame, SnapToFrame will behave the same.")]
            SnapToClipFrame = 2
        }

        [SerializeField] public TimelineWindowHierarchyState hierarchyState; // Persistent state of treeview on the left side of window
        [SerializeField] public TimeEditor animEditor; // Reference to owner of this state. Used to trigger repaints.
        [SerializeField] public bool showCurveEditor; // Do we show dopesheet or curves
        [SerializeField] public bool linkedWithSequencer; // Toggle Sequencer selection mode.
        [SerializeField] private bool m_RippleTime; // Toggle ripple time option for curve editor and dopesheet.
        private bool m_RippleTimeClutch; // Toggle ripple time option for curve editor and dopesheet.
        [SerializeField] private UnityEditor.TimeArea m_TimeArea; // Either curveeditor or dopesheet depending on which is selected
        [SerializeField] private TimelineWindowSelectionItem m_EmptySelection;
        [SerializeField] private TimelineWindowSelectionItem m_Selection; // Internal selection
        [SerializeField] private TimelineWindowKeySelection m_KeySelection; // What is selected. Hashes persist cache reload, because they are from keyframe time+value
        [SerializeField] private int m_ActiveKeyframeHash; // Which keyframe is active (selected key that user previously interacted with)
        [SerializeField] private float m_FrameRate = kDefaultFrameRate;
        [SerializeField] private TimelineWindowControl m_ControlInterface;
        [SerializeField] private ITimelineWindowControl m_OverrideControlInterface;
        [SerializeField] private int[] m_SelectionFilter;

        [NonSerialized] public Action onStartLiveEdit;
        [NonSerialized] public Action onEndLiveEdit;
        [NonSerialized] public Action<float> onFrameRateChange;

        private static List<TimelineWindowKeyframe> s_KeyframeClipboard; // For copy-pasting keyframes

        [NonSerialized] public TimelineWindowHierarchyDataSource hierarchyData;

        private List<TimelineWindowCurve> m_AllCurvesCache;
        private List<TimelineWindowCurve> m_ActiveCurvesCache;
        private List<DopeLine> m_dopelinesCache;
        private List<TimelineWindowKeyframe> m_SelectedKeysCache;
        private Bounds? m_SelectionBoundsCache;
        private TimelineEditor.CurveWrapper[] m_ActiveCurveWrappersCache;
        private TimelineWindowKeyframe m_ActiveKeyframeCache;
        private HashSet<int> m_ModifiedCurves = new HashSet<int>();
        private EditorCurveBinding? m_lastAddedCurveBinding;

        // Hash of all the things that require animationWindow to refresh if they change
        private int m_PreviousRefreshHash;

        // Changing m_Refresh means you are ordering a refresh at the next OnGUI().
        // CurvesOnly means that there is no need to refresh the hierarchy, since only the keyframe data changed.
        private RefreshType m_Refresh = RefreshType.None;

        private struct LiveEditKeyframe
        {
            public TimelineWindowKeyframe keySnapshot;
            public TimelineWindowKeyframe key;
        }

        private class LiveEditCurve
        {
            public TimelineWindowCurve curve;
            public List<LiveEditKeyframe> selectedKeys = new List<LiveEditKeyframe>();
            public List<LiveEditKeyframe> unselectedKeys = new List<LiveEditKeyframe>();
        }

        private List<LiveEditCurve> m_LiveEditSnapshot;

        public const float kDefaultFrameRate = 30.0f;
        public const string kEditCurveUndoLabel = "Edit Curve";

        public TimelineWindowSelectionItem selection
        {
            get
            {
                if (m_Selection != null)
                    return m_Selection;

                if (m_EmptySelection == null)
                {
                    m_EmptySelection = TimelineClipSelectionItem.Create(null, null);
                    m_EmptySelection.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_EmptySelection;
            }

            set
            {
                if (m_Selection != null)
                    Object.DestroyImmediate(m_Selection);

                // Make a copy and take ownership
                if (value != null)
                {
                    m_Selection = Object.Instantiate(value);
                    m_Selection.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    m_Selection = null;
                }

                OnSelectionChanged();
            }
        }

        // AnimationClip we are currently editing
        public AnimationClip activeAnimationClip
        {
            get
            {
                return selection.animationClip;
            }
            set
            {
                if (selection.canChangeAnimationClip)
                {
                    selection.animationClip = value;
                    OnSelectionChanged();
                }
            }
        }

        public Timeline activeTimeline
        {
            get
            {
                return selection.timeline;
            }
            set
            {
                if (selection.canChangeAnimationClip)
                {
                    selection.timeline = value;
                    OnSelectionChanged();
                }
            }
        }

        // Previously or currently selected gameobject is considered as the active gameobject
        public GameObject activeGameObject
        {
            get
            {
                return selection.gameObject;
            }
        }

        // Closes parent to activeGameObject that has Animator component
        public GameObject activeRootGameObject
        {
            get
            {
                return selection.rootGameObject;
            }
        }

        public Component activeAnimationPlayer
        {
            get
            {
                return selection.animationPlayer;
            }
        }

        public ScriptableObject activeScriptableObject
        {
            get
            {
                return selection.scriptableObject;
            }
        }

        // Is the hierarchy in animator optimized
        public bool animatorIsOptimized
        {
            get
            {
                return selection.objectIsOptimized;
            }
        }

        public bool disabled
        {
            get
            {
                return selection.disabled;
            }
        }

        public ITimelineWindowControl controlInterface
        {
            get
            {
                if (m_OverrideControlInterface != null)
                    return m_OverrideControlInterface;

                return m_ControlInterface;
            }
        }

        public ITimelineWindowControl overrideControlInterface
        {
            get
            {
                return m_OverrideControlInterface;
            }

            set
            {
                if (m_OverrideControlInterface != null)
                    Object.DestroyImmediate(m_OverrideControlInterface);

                m_OverrideControlInterface = value;
            }
        }


        public bool filterBySelection
        {
            get
            {
                return TimelineWindowOptions.filterBySelection;
            }
            set
            {
                TimelineWindowOptions.filterBySelection = value;
                UpdateSelectionFilter();

                // Refresh everything.
                refresh = RefreshType.Everything;
            }
        }

        public bool showReadOnly
        {
            get
            {
                return TimelineWindowOptions.showReadOnly;
            }
            set
            {
                TimelineWindowOptions.showReadOnly = value;

                // Refresh everything.
                refresh = RefreshType.Everything;
            }
        }

        public bool rippleTime
        {
            get
            {
                return m_RippleTime || m_RippleTimeClutch;
            }
            set
            {
                m_RippleTime = value;
            }
        }

        public bool rippleTimeClutch { get { return m_RippleTimeClutch; } set { m_RippleTimeClutch = value; } }

        public bool showFrameRate { get { return TimelineWindowOptions.showFrameRate; } set { TimelineWindowOptions.showFrameRate = value; } }

        public void OnGUI()
        {
            RefreshHashCheck();
            Refresh();
        }

        private void RefreshHashCheck()
        {
            int newRefreshHash = GetRefreshHash();
            if (m_PreviousRefreshHash != newRefreshHash)
            {
                refresh = RefreshType.Everything;
                m_PreviousRefreshHash = newRefreshHash;
            }
        }

        private void Refresh()
        {
            selection.Synchronize();

            if (refresh == RefreshType.Everything)
            {
                selection.ClearCache();

                m_ActiveKeyframeCache = null;
                m_AllCurvesCache = null;
                m_ActiveCurvesCache = null;
                m_dopelinesCache = null;
                m_SelectedKeysCache = null;
                m_SelectionBoundsCache = null;

                if (animEditor != null && animEditor.curveEditor != null)
                    animEditor.curveEditor.InvalidateSelectionBounds();

                ClearCurveWrapperCache();

                if (hierarchyData != null)
                    hierarchyData.UpdateData();

                // If there was new curve added, set it as active selection
                if (m_lastAddedCurveBinding != null)
                    OnNewCurveAdded((EditorCurveBinding)m_lastAddedCurveBinding);

                // select top dopeline if there is no selection available
                if (activeCurves.Count == 0 && dopelines.Count > 0)
                    SelectHierarchyItem(dopelines[0], false, false);

                m_Refresh = RefreshType.None;
            }
            else if (refresh == RefreshType.CurvesOnly)
            {
                m_ActiveKeyframeCache = null;
                m_SelectedKeysCache = null;
                m_SelectionBoundsCache = null;

                if (animEditor != null && animEditor.curveEditor != null)
                    animEditor.curveEditor.InvalidateSelectionBounds();

                ReloadModifiedAnimationCurveCache();
                ReloadModifiedDopelineCache();
                ReloadModifiedCurveWrapperCache();

                m_Refresh = RefreshType.None;
                m_ModifiedCurves.Clear();
            }
        }

        // Hash for checking if any of these things is changed
        private int GetRefreshHash()
        {
            return
                selection.GetRefreshHash() ^
                (hierarchyState != null ? hierarchyState.expandedIDs.Count : 0) ^
                (hierarchyState != null ? hierarchyState.GetTallInstancesCount() : 0) ^
                (showCurveEditor ? 1 : 0);
        }

        public void ForceRefresh()
        {
            refresh = RefreshType.Everything;
        }

        private void PurgeSelection()
        {
            linkedWithSequencer = false;
            Object.DestroyImmediate(m_OverrideControlInterface);

            // Selected object could have been changed when unlocking the animation window
            Object.DestroyImmediate(m_Selection);
            m_Selection = null;
        }

        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            AnimationUtility.onCurveWasModified += CurveWasModified;
            Undo.undoRedoEvent += UndoRedoPerformed;
            AssemblyReloadEvents.beforeAssemblyReload += PurgeSelection;

            // NoOps...
            onStartLiveEdit += () => {};
            onEndLiveEdit += () => {};

            if (m_ControlInterface == null)
                m_ControlInterface = CreateInstance(typeof(TimelineWindowControl)) as TimelineWindowControl;
            m_ControlInterface.state = this;
            m_ControlInterface.OnEnable();
        }

        public void OnDisable()
        {
            AnimationUtility.onCurveWasModified -= CurveWasModified;
            Undo.undoRedoEvent -= UndoRedoPerformed;
            AssemblyReloadEvents.beforeAssemblyReload -= PurgeSelection;

            m_ControlInterface.OnDisable();
        }

        public void OnDestroy()
        {
            m_ControlInterface.OnDestroy();

            Object.DestroyImmediate(m_EmptySelection);
            Object.DestroyImmediate(m_Selection);
            Object.DestroyImmediate(m_KeySelection);
            Object.DestroyImmediate(m_ControlInterface);
            Object.DestroyImmediate(m_OverrideControlInterface);
        }

        public void OnSelectionChanged()
        {
            if (onFrameRateChange != null)
                onFrameRateChange(frameRate);

            UpdateSelectionFilter();

            // reset back time at 0 upon selection change.
            controlInterface.OnSelectionChanged();

            if (animEditor != null)
                animEditor.OnSelectionChanged();
        }

        public void OnSelectionUpdated()
        {
            UpdateSelectionFilter();
            if (filterBySelection)
            {
                // Refresh everything.
                refresh = RefreshType.Everything;
            }
        }

        // Set this property to ask for refresh at the next OnGUI.
        public RefreshType refresh
        {
            get { return m_Refresh; }
            // Make sure that if full refresh is already ordered, nobody gets to f*** with it
            set
            {
                if ((int)m_Refresh < (int)value)
                    m_Refresh = value;
            }
        }

        public void UndoRedoPerformed(in UndoRedoInfo info)
        {
            refresh = RefreshType.Everything;
            controlInterface.ResampleAnimation();
        }

        // When curve is modified, we never trigger refresh right away. We order a refresh at later time by setting refresh to appropriate value.
        void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            // AnimationWindow doesn't care if some other clip somewhere changed
            if (activeAnimationClip != clip)
                return;

            // Refresh curves that already exist.
            if (type == AnimationUtility.CurveModifiedType.CurveModified)
            {
                bool didFind = false;
                bool hadPhantom = false;
                int hashCode = binding.GetHashCode();

                List<TimelineWindowCurve> curves = selection.curves;
                for (int j = 0; j < curves.Count; ++j)
                {
                    TimelineWindowCurve curve = curves[j];
                    int curveHash = curve.GetBindingHashCode();
                    if (curveHash == hashCode)
                    {
                        m_ModifiedCurves.Add(curve.GetHashCode());
                        didFind = true;
                        hadPhantom |= curve.binding.isPhantom;
                    }
                }

                if (didFind && !hadPhantom)
                    refresh = RefreshType.CurvesOnly;
                else
                {
                    // New curve was added, so let's save binding and make it active selection when Refresh is called next time
                    m_lastAddedCurveBinding = binding;
                    refresh = RefreshType.Everything;
                }
            }
            else
            {
                // Otherwise do a full reload
                refresh = RefreshType.Everything;
            }
            // Force repaint to display live animation curve changes from other editor window (like timeline).
            Repaint();
        }

        public void SaveKeySelection(string undoLabel)
        {
            if (m_KeySelection != null)
                Undo.RegisterCompleteObjectUndo(m_KeySelection, undoLabel);
        }

        public void SaveCurve(AnimationClip clip, TimelineWindowCurve curve)
        {
            SaveCurve(clip, curve, kEditCurveUndoLabel);
        }

        public void SaveCurve(AnimationClip clip, TimelineWindowCurve curve, string undoLabel)
        {
            if (!curve.animationIsEditable)
                Debug.LogError("Curve is not editable and shouldn't be saved.");

            Undo.RegisterCompleteObjectUndo(clip, undoLabel);
            TimelineWindowUtility.SaveCurve(clip, curve);
            Repaint();
        }

        public void SaveCurves(AnimationClip clip, ICollection<TimelineWindowCurve> curves, string undoLabel = kEditCurveUndoLabel)
        {
            if (curves.Count == 0)
                return;

            Undo.RegisterCompleteObjectUndo(clip, undoLabel);
            TimelineWindowUtility.SaveCurves(clip, curves);
            Repaint();
        }

        private void SaveSelectedKeys(string undoLabel)
        {
            List<TimelineWindowCurve> saveCurves = new List<TimelineWindowCurve>();

            // Find all curves that need saving
            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                if (!snapshot.curve.animationIsEditable)
                    continue;

                if (!saveCurves.Contains(snapshot.curve))
                    saveCurves.Add(snapshot.curve);

                List<TimelineWindowKeyframe> toBeDeleted = new List<TimelineWindowKeyframe>();

                // If selected keys are dragged over non-selected keyframe at exact same time, then delete the unselected ones underneath
                foreach (TimelineWindowKeyframe other in snapshot.curve.m_Keyframes)
                {
                    // Keyframe is in selection, skip.
                    if (snapshot.selectedKeys.Exists(liveEditKey => liveEditKey.key == other))
                        continue;

                    // There is already a selected keyframe at that time, delete non-selected keyframe.
                    if (!snapshot.selectedKeys.Exists(liveEditKey => TimelineKeyTime.Time(liveEditKey.key.time, frameRate).frame == TimelineKeyTime.Time(other.time, frameRate).frame))
                        continue;

                    toBeDeleted.Add(other);
                }

                foreach (TimelineWindowKeyframe deletedKey in toBeDeleted)
                {
                    snapshot.curve.m_Keyframes.Remove(deletedKey);
                }
            }

            SaveCurves(activeAnimationClip, saveCurves, undoLabel);
        }

        public void RemoveCurve(TimelineWindowCurve curve, string undoLabel)
        {
            if (!curve.animationIsEditable)
                return;

            Undo.RegisterCompleteObjectUndo(curve.clip, undoLabel);

            if (curve.isPPtrCurve)
                AnimationUtility.SetObjectReferenceCurve(curve.clip, curve.binding, null);
            else
                AnimationUtility.SetEditorCurve(curve.clip, curve.binding, null);
        }

        public bool previewing { get { return controlInterface.previewing; } }

        public bool canPreview { get { return controlInterface.canPreview; } }

        public void StartPreview()
        {
            controlInterface.StartPreview();
            controlInterface.ResampleAnimation();
        }

        public void UpdateCurvesDisplayName()
        {
            if (hierarchyData != null)
                hierarchyData.UpdateSerializeReferenceCurvesArrayNiceDisplayName();
        }

        public void StopPreview()
        {
            controlInterface.StopPreview();
        }

        public bool recording { get { return controlInterface.recording; } }

        public bool canRecord { get { return controlInterface.canRecord; } }

        public void StartRecording()
        {
            controlInterface.StartRecording(selection.sourceObject);
            controlInterface.ResampleAnimation();
        }

        public void StopRecording()
        {
            controlInterface.StopRecording();
        }

        public bool playing { get { return controlInterface.playing; } }

        public void StartPlayback()
        {
            controlInterface.StartPlayback();
        }

        public void StopPlayback()
        {
            controlInterface.StopPlayback();
        }

        public void ResampleAnimation()
        {
            controlInterface.ResampleAnimation();
        }

        public void ClearCandidates()
        {
            controlInterface.ClearCandidates();
        }

        public bool ShouldShowCurve(TimelineWindowCurve curve)
        {
            if (filterBySelection && activeRootGameObject != null)
            {
                if (m_SelectionFilter != null)
                {
                    Transform t = activeRootGameObject.transform.Find(curve.path);
                    if (t != null)
                    {
                        if (!m_SelectionFilter.Contains(t.gameObject.GetInstanceID()))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void UpdateSelectionFilter()
        {
            m_SelectionFilter = (filterBySelection) ? (int[])Selection.instanceIDs.Clone() : null;
        }

        public List<TimelineWindowCurve> allCurves
        {
            get
            {
                if (m_AllCurvesCache == null)
                {
                    if (!selection.animationIsEditable && !showReadOnly)
                    {
                        // Empty list.
                        m_AllCurvesCache = new List<TimelineWindowCurve>();
                    }
                    else if (!filterBySelection || activeRootGameObject == null)
                    {
                        m_AllCurvesCache = selection.curves;
                    }
                    else
                    {
                        List<TimelineWindowCurve> allCurvesUnfiltered = selection.curves;
                        m_AllCurvesCache = new List<TimelineWindowCurve>();
                        for (int i = 0; i < allCurvesUnfiltered.Count; ++i)
                        {
                            if (ShouldShowCurve(allCurvesUnfiltered[i]))
                                m_AllCurvesCache.Add(allCurvesUnfiltered[i]);
                        }
                    }
                }

                return m_AllCurvesCache;
            }
        }

        public List<TimelineWindowCurve> activeCurves
        {
            get
            {
                if (m_ActiveCurvesCache == null)
                {
                    m_ActiveCurvesCache = new List<TimelineWindowCurve>();
                    if (hierarchyState != null && hierarchyData != null)
                    {
                        foreach (int id in hierarchyState.selectedIDs)
                        {
                            TreeViewItem node = hierarchyData.FindItem(id);
                            TimelineWindowHierarchyNode hierarchyNode = node as TimelineWindowHierarchyNode;

                            if (hierarchyNode == null)
                                continue;

                            TimelineWindowCurve[] curves = hierarchyNode.curves;
                            if (curves == null)
                                continue;

                            foreach (TimelineWindowCurve curve in curves)
                                if (!m_ActiveCurvesCache.Contains(curve))
                                    m_ActiveCurvesCache.Add(curve);
                        }

                        m_ActiveCurvesCache.Sort();
                    }
                }

                return m_ActiveCurvesCache;
            }
        }

        public TimelineEditor.CurveWrapper[] activeCurveWrappers
        {
            get
            {
                if (m_ActiveCurveWrappersCache == null || m_ActiveCurvesCache == null)
                {
                    List<TimelineEditor.CurveWrapper> activeCurveWrappers = new List<TimelineEditor.CurveWrapper>();
                    foreach (TimelineWindowCurve curve in activeCurves)
                        if (!curve.isDiscreteCurve)
                            activeCurveWrappers.Add(TimelineWindowUtility.GetCurveWrapper(curve, curve.clip));

                    // If there are no active curves, we would end up with empty curve editor so we just give all curves insteads
                    if (!activeCurveWrappers.Any())
                        foreach (TimelineWindowCurve curve in allCurves)
                            if (!curve.isDiscreteCurve)
                                activeCurveWrappers.Add(TimelineWindowUtility.GetCurveWrapper(curve, curve.clip));

                    m_ActiveCurveWrappersCache = activeCurveWrappers.ToArray();
                }

                return m_ActiveCurveWrappersCache;
            }
        }

        public List<DopeLine> dopelines
        {
            get
            {
                if (m_dopelinesCache == null)
                {
                    m_dopelinesCache = new List<DopeLine>();

                    if (hierarchyData != null)
                    {
                        foreach (TreeViewItem node in hierarchyData.GetRows())
                        {
                            TimelineWindowHierarchyNode hierarchyNode = node as TimelineWindowHierarchyNode;

                            if (hierarchyNode == null || hierarchyNode is TimelineWindowHierarchyAddButtonNode)
                                continue;

                            TimelineWindowCurve[] curves = hierarchyNode.curves;
                            if (curves == null)
                                continue;

                            DopeLine dopeLine = new DopeLine(node.id, curves);
                            dopeLine.tallMode = hierarchyState.GetTallMode(hierarchyNode);
                            dopeLine.objectType = hierarchyNode.animatableObjectType;
                            dopeLine.hasChildren = !(hierarchyNode is TimelineWindowHierarchyPropertyNode);
                            dopeLine.isMasterDopeline = node is TimelineWindowHierarchyMasterNode;
                            m_dopelinesCache.Add(dopeLine);
                        }
                    }
                }
                return m_dopelinesCache;
            }
        }

        public List<TimelineWindowHierarchyNode> selectedHierarchyNodes
        {
            get
            {
                List<TimelineWindowHierarchyNode> selectedHierarchyNodes = new List<TimelineWindowHierarchyNode>();

                if (activeAnimationClip != null && hierarchyData != null)
                {
                    foreach (int id in hierarchyState.selectedIDs)
                    {
                        TimelineWindowHierarchyNode hierarchyNode = (TimelineWindowHierarchyNode)hierarchyData.FindItem(id);

                        if (hierarchyNode == null || hierarchyNode is TimelineWindowHierarchyAddButtonNode)
                            continue;

                        selectedHierarchyNodes.Add(hierarchyNode);
                    }
                }

                return selectedHierarchyNodes;
            }
        }

        public TimelineWindowKeyframe activeKeyframe
        {
            get
            {
                if (m_ActiveKeyframeCache == null)
                {
                    foreach (TimelineWindowCurve curve in allCurves)
                    {
                        foreach (TimelineWindowKeyframe keyframe in curve.m_Keyframes)
                        {
                            if (keyframe.GetHash() == m_ActiveKeyframeHash)
                                m_ActiveKeyframeCache = keyframe;
                        }
                    }
                }
                return m_ActiveKeyframeCache;
            }
            set
            {
                m_ActiveKeyframeCache = null;
                m_ActiveKeyframeHash = value != null ? value.GetHash() : 0;
            }
        }

        public List<TimelineWindowKeyframe> selectedKeys
        {
            get
            {
                if (m_SelectedKeysCache == null)
                {
                    m_SelectedKeysCache = new List<TimelineWindowKeyframe>();
                    foreach (TimelineWindowCurve curve in allCurves)
                    {
                        foreach (TimelineWindowKeyframe keyframe in curve.m_Keyframes)
                        {
                            if (KeyIsSelected(keyframe))
                            {
                                m_SelectedKeysCache.Add(keyframe);
                            }
                        }
                    }
                }
                return m_SelectedKeysCache;
            }
        }

        public Bounds selectionBounds
        {
            get
            {
                if (m_SelectionBoundsCache == null)
                {
                    List<TimelineWindowKeyframe> keys = selectedKeys;
                    if (keys.Count > 0)
                    {
                        TimelineWindowKeyframe key = keys[0];
                        float time = key.time;
                        float val = key.isPPtrCurve || key.isDiscreteCurve ? 0.0f : (float)key.value;

                        Bounds bounds = new Bounds(new Vector2(time, val), Vector2.zero);

                        for (int i = 1; i < keys.Count; ++i)
                        {
                            key = keys[i];

                            time = key.time;
                            val = key.isPPtrCurve || key.isDiscreteCurve ? 0.0f : (float)key.value;

                            bounds.Encapsulate(new Vector2(time, val));
                        }

                        m_SelectionBoundsCache = bounds;
                    }
                    else
                    {
                        m_SelectionBoundsCache = new Bounds(Vector2.zero, Vector2.zero);
                    }
                }

                return m_SelectionBoundsCache.Value;
            }
        }

        private HashSet<int> selectedKeyHashes
        {
            get
            {
                if (m_KeySelection == null)
                {
                    m_KeySelection = CreateInstance<TimelineWindowKeySelection>();
                    m_KeySelection.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_KeySelection.selectedKeyHashes;
            }
            set
            {
                if (m_KeySelection == null)
                {
                    m_KeySelection = CreateInstance<TimelineWindowKeySelection>();
                    m_KeySelection.hideFlags = HideFlags.HideAndDontSave;
                }

                m_KeySelection.selectedKeyHashes = value;
            }
        }

        public bool AnyKeyIsSelected(DopeLine dopeline)
        {
            foreach (TimelineWindowKeyframe keyframe in dopeline.keys)
                if (KeyIsSelected(keyframe))
                    return true;

            return false;
        }

        public bool KeyIsSelected(TimelineWindowKeyframe keyframe)
        {
            return selectedKeyHashes.Contains(keyframe.GetHash());
        }

        public void SelectKey(TimelineWindowKeyframe keyframe)
        {
            int hash = keyframe.GetHash();
            if (!selectedKeyHashes.Contains(hash))
                selectedKeyHashes.Add(hash);

            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
        }

        public void UnselectKey(TimelineWindowKeyframe keyframe)
        {
            int hash = keyframe.GetHash();
            if (selectedKeyHashes.Contains(hash))
                selectedKeyHashes.Remove(hash);

            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
        }

        public void DeleteSelectedKeys()
        {
            if (selectedKeys.Count == 0)
                return;

            DeleteKeys(selectedKeys);
        }

        public void DeleteKeys(List<TimelineWindowKeyframe> keys)
        {
            SaveKeySelection(kEditCurveUndoLabel);

            HashSet<TimelineWindowCurve> curves = new HashSet<TimelineWindowCurve>();

            foreach (TimelineWindowKeyframe keyframe in keys)
            {
                if (!keyframe.curve.animationIsEditable)
                    continue;

                curves.Add(keyframe.curve);

                UnselectKey(keyframe);
                keyframe.curve.m_Keyframes.Remove(keyframe);
            }

            SaveCurves(activeAnimationClip, curves, kEditCurveUndoLabel);

            ResampleAnimation();
        }

        public void StartLiveEdit()
        {
            if (onStartLiveEdit != null)
                onStartLiveEdit();

            m_LiveEditSnapshot = new List<LiveEditCurve>();

            SaveKeySelection(kEditCurveUndoLabel);

            foreach (TimelineWindowKeyframe selectedKey in selectedKeys)
            {
                if (!m_LiveEditSnapshot.Exists(snapshot => snapshot.curve == selectedKey.curve))
                {
                    LiveEditCurve snapshot = new LiveEditCurve();
                    snapshot.curve = selectedKey.curve;
                    foreach (TimelineWindowKeyframe key in selectedKey.curve.m_Keyframes)
                    {
                        LiveEditKeyframe liveEditKey = new LiveEditKeyframe();
                        liveEditKey.keySnapshot = new TimelineWindowKeyframe(key);
                        liveEditKey.key = key;

                        if (KeyIsSelected(key))
                            snapshot.selectedKeys.Add(liveEditKey);
                        else
                            snapshot.unselectedKeys.Add(liveEditKey);
                    }

                    m_LiveEditSnapshot.Add(snapshot);
                }
            }
        }

        public void EndLiveEdit()
        {
            SaveSelectedKeys(kEditCurveUndoLabel);

            m_LiveEditSnapshot = null;

            if (onEndLiveEdit != null)
                onEndLiveEdit();
        }

        public bool InLiveEdit()
        {
            return m_LiveEditSnapshot != null;
        }

        public void MoveSelectedKeys(float deltaTime, bool snapToFrame)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            // Clear selections since all hashes are now different
            ClearKeySelections();

            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                foreach (LiveEditKeyframe liveEditKey in snapshot.selectedKeys)
                {
                    if (snapshot.curve.animationIsEditable)
                    {
                        liveEditKey.key.time = Mathf.Max(liveEditKey.keySnapshot.time + deltaTime, 0f);

                        if (snapToFrame)
                            liveEditKey.key.time = SnapToFrame(liveEditKey.key.time, snapshot.curve.clip.frameRate);
                    }

                    SelectKey(liveEditKey.key);
                }
            }

            if (!inLiveEdit)
                EndLiveEdit();
        }

        public void TransformSelectedKeys(Matrix4x4 matrix, bool flipX, bool flipY, bool snapToFrame)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            // Clear selections since all hashes are now different
            ClearKeySelections();

            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                foreach (LiveEditKeyframe liveEditKey in snapshot.selectedKeys)
                {
                    if (snapshot.curve.animationIsEditable)
                    {
                        // Transform time value.
                        Vector3 v = new Vector3(liveEditKey.keySnapshot.time, liveEditKey.keySnapshot.isPPtrCurve || liveEditKey.keySnapshot.isDiscreteCurve ? 0f : (float)liveEditKey.keySnapshot.value, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(v.x, snapshot.curve.clip.frameRate) : v.x, 0f);

                        if (flipX)
                        {
                            liveEditKey.key.inTangent = (liveEditKey.keySnapshot.outTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.outTangent : Mathf.Infinity;
                            liveEditKey.key.outTangent = (liveEditKey.keySnapshot.inTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.inTangent : Mathf.Infinity;

                            if (liveEditKey.keySnapshot.weightedMode == WeightedMode.In)
                                liveEditKey.key.weightedMode = WeightedMode.Out;
                            else if (liveEditKey.keySnapshot.weightedMode == WeightedMode.Out)
                                liveEditKey.key.weightedMode = WeightedMode.In;
                            else
                                liveEditKey.key.weightedMode = liveEditKey.keySnapshot.weightedMode;

                            liveEditKey.key.inWeight = liveEditKey.keySnapshot.outWeight;
                            liveEditKey.key.outWeight = liveEditKey.keySnapshot.inWeight;
                        }

                        if (!liveEditKey.key.isPPtrCurve && !liveEditKey.key.isDiscreteCurve)
                        {
                            liveEditKey.key.value = v.y;

                            if (flipY)
                            {
                                liveEditKey.key.inTangent = (liveEditKey.key.inTangent != Mathf.Infinity) ? -liveEditKey.key.inTangent : Mathf.Infinity;
                                liveEditKey.key.outTangent = (liveEditKey.key.outTangent != Mathf.Infinity) ? -liveEditKey.key.outTangent : Mathf.Infinity;
                            }
                        }
                    }

                    SelectKey(liveEditKey.key);
                }
            }

            if (!inLiveEdit)
                EndLiveEdit();
        }

        public void TransformRippleKeys(Matrix4x4 matrix, float t1, float t2, bool flipX, bool snapToFrame)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            // Clear selections since all hashes are now different
            ClearKeySelections();

            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                foreach (LiveEditKeyframe liveEditKey in snapshot.selectedKeys)
                {
                    if (snapshot.curve.animationIsEditable)
                    {
                        Vector3 v = new Vector3(liveEditKey.keySnapshot.time, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(v.x, snapshot.curve.clip.frameRate) : v.x, 0f);

                        if (flipX)
                        {
                            liveEditKey.key.inTangent = (liveEditKey.keySnapshot.outTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.outTangent : Mathf.Infinity;
                            liveEditKey.key.outTangent = (liveEditKey.keySnapshot.inTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.inTangent : Mathf.Infinity;
                        }
                    }

                    SelectKey(liveEditKey.key);
                }

                if (!snapshot.curve.animationIsEditable)
                    continue;

                foreach (LiveEditKeyframe liveEditKey in snapshot.unselectedKeys)
                {
                    if (liveEditKey.keySnapshot.time > t2)
                    {
                        Vector3 v = new Vector3(flipX ? t1 : t2, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        float dt = v.x - t2;
                        if (dt > 0f)
                        {
                            float newTime = liveEditKey.keySnapshot.time + dt;
                            liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(newTime, snapshot.curve.clip.frameRate) : newTime, 0f);
                        }
                        else
                        {
                            liveEditKey.key.time = liveEditKey.keySnapshot.time;
                        }
                    }
                    else if (liveEditKey.keySnapshot.time < t1)
                    {
                        Vector3 v = new Vector3(flipX ? t2 : t1, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        float dt = v.x - t1;
                        if (dt < 0f)
                        {
                            float newTime = liveEditKey.keySnapshot.time + dt;
                            liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(newTime, snapshot.curve.clip.frameRate) : newTime, 0f);
                        }
                        else
                        {
                            liveEditKey.key.time = liveEditKey.keySnapshot.time;
                        }
                    }
                }
            }

            if (!inLiveEdit)
                EndLiveEdit();
        }

        internal static bool CanPasteKeys()
        {
            return s_KeyframeClipboard != null && s_KeyframeClipboard.Count > 0;
        }

        internal static void ClearKeyframeClipboard()
        {
            s_KeyframeClipboard?.Clear();
        }

        public void CopyKeys()
        {
            if (s_KeyframeClipboard == null)
                s_KeyframeClipboard = new List<TimelineWindowKeyframe>();

            float smallestTime = float.MaxValue;
            s_KeyframeClipboard.Clear();
            foreach (TimelineWindowKeyframe keyframe in selectedKeys)
            {
                s_KeyframeClipboard.Add(new TimelineWindowKeyframe(keyframe));
                if (keyframe.time < smallestTime)
                    smallestTime = keyframe.time;
            }
            if (s_KeyframeClipboard.Count > 0) // copying selected keys
            {
                foreach (TimelineWindowKeyframe keyframe in s_KeyframeClipboard)
                {
                    keyframe.time -= smallestTime;
                }
            }
            else // No selected keys, lets copy entire curves
            {
                CopyAllActiveCurves();
            }

            // Animation keyframes right now do not go through regular clipboard machinery,
            // so when copying keyframes, make sure regular clipboard is cleared, or things
            // get confusing.
            if (s_KeyframeClipboard.Count > 0)
                Clipboard.stringValue = string.Empty;
        }

        public void CopyAllActiveCurves()
        {
            foreach (TimelineWindowCurve curve in activeCurves)
            {
                foreach (TimelineWindowKeyframe keyframe in curve.m_Keyframes)
                {
                    s_KeyframeClipboard.Add(new TimelineWindowKeyframe(keyframe));
                }
            }
        }

        public void PasteKeys()
        {
            if (s_KeyframeClipboard == null)
                s_KeyframeClipboard = new List<TimelineWindowKeyframe>();

            SaveKeySelection(kEditCurveUndoLabel);

            HashSet<int> oldSelection = new HashSet<int>(selectedKeyHashes);
            ClearKeySelections();

            TimelineWindowCurve lastTargetCurve = null;
            TimelineWindowCurve lastSourceCurve = null;
            float lastTime = 0f;

            List<TimelineWindowCurve> clipboardCurves = new List<TimelineWindowCurve>();
            foreach (TimelineWindowKeyframe keyframe in s_KeyframeClipboard)
                if (!clipboardCurves.Any() || clipboardCurves.Last() != keyframe.curve)
                    clipboardCurves.Add(keyframe.curve);

            // If we have equal number of target and source curves, then match by index. If not, then try to match with AnimationWindowUtility.BestMatchForPaste.
            bool matchCurveByIndex = clipboardCurves.Count == activeCurves.Count;
            int targetIndex = 0;

            Dictionary<TimelineWindowCurve, TimelineWindowCurve> sourceCurveToTargetCurve = new Dictionary<TimelineWindowCurve, TimelineWindowCurve>();

            foreach (TimelineWindowKeyframe keyframe in s_KeyframeClipboard)
            {
                if (lastSourceCurve != null && keyframe.curve != lastSourceCurve)
                    targetIndex++;

                TimelineWindowKeyframe newKeyframe = new TimelineWindowKeyframe(keyframe);

                if (sourceCurveToTargetCurve.ContainsKey(keyframe.curve))
                {
                    newKeyframe.curve = sourceCurveToTargetCurve[keyframe.curve];
                }
                else
                {
                    if (matchCurveByIndex)
                        newKeyframe.curve = activeCurves[targetIndex];
                    else
                        newKeyframe.curve = TimelineWindowUtility.BestMatchForPaste(newKeyframe.curve.binding, clipboardCurves, activeCurves);

                    if (newKeyframe.curve == null) // Paste as new curve
                    {
                        // Curves are selected in the animation window hierarchy.  Since we couldn't find a proper match,
                        // create a new curve in first selected clip in active curves.
                        if (activeCurves.Count > 0)
                        {
                            TimelineWindowCurve firstCurve = activeCurves[0];
                            if (firstCurve.animationIsEditable)
                            {
                                newKeyframe.curve = new TimelineWindowCurve(firstCurve.clip, keyframe.curve.binding, keyframe.curve.valueType);
                                newKeyframe.curve.selectionBinding = firstCurve.selectionBinding;
                                newKeyframe.time = keyframe.time;
                            }
                        }
                        // If nothing is selected, create a new curve in first selected clip.
                        else
                        {
                            if (selection.animationIsEditable)
                            {
                                newKeyframe.curve = new TimelineWindowCurve(selection.animationClip, keyframe.curve.binding, keyframe.curve.valueType);
                                newKeyframe.curve.selectionBinding = selection;
                                newKeyframe.time = keyframe.time;
                            }
                        }
                    }
                }

                if (newKeyframe.curve == null || !newKeyframe.curve.animationIsEditable)
                    continue;

                newKeyframe.time = TimelineKeyTime.Time(newKeyframe.time + currentTime, newKeyframe.curve.clip.frameRate).timeRound;

                //  Only allow pasting of key frame from numerical curves to numerical curves or from pptr curves to pptr curves.
                if ((newKeyframe.time >= 0.0f) && (newKeyframe.curve != null) && (newKeyframe.curve.isPPtrCurve == keyframe.curve.isPPtrCurve))
                {
                    if (newKeyframe.curve.HasKeyframe(TimelineKeyTime.Time(newKeyframe.time, newKeyframe.curve.clip.frameRate)))
                        newKeyframe.curve.RemoveKeyframe(TimelineKeyTime.Time(newKeyframe.time, newKeyframe.curve.clip.frameRate));

                    // When copy-pasting multiple keyframes (curve), its a continous thing. This is why we delete the existing keyframes in the pasted range.
                    if (lastTargetCurve == newKeyframe.curve)
                        newKeyframe.curve.RemoveKeysAtRange(lastTime, newKeyframe.time);

                    newKeyframe.curve.m_Keyframes.Add(newKeyframe);
                    SelectKey(newKeyframe);

                    if (!sourceCurveToTargetCurve.ContainsKey(keyframe.curve))
                    {
                        sourceCurveToTargetCurve[keyframe.curve] = newKeyframe.curve;
                    }
                    // TODO: Optimize to only save curve once instead once per keyframe
                    //SaveCurve(newKeyframe.curve.clip, newKeyframe.curve, kEditCurveUndoLabel);

                    lastTargetCurve = newKeyframe.curve;
                    lastTime = newKeyframe.time;
                }

                lastSourceCurve = keyframe.curve;
            }

            Dictionary<AnimationClip, HashSet<TimelineWindowCurve>> clipToCurve = new Dictionary<AnimationClip, HashSet<TimelineWindowCurve>>();
            foreach (var keyValue in sourceCurveToTargetCurve)
            {
                TimelineWindowCurve targetCurve = keyValue.Value;
                AnimationClip clip = targetCurve.clip;
                HashSet<TimelineWindowCurve> curves = null;
                if (!clipToCurve.TryGetValue(clip, out curves))
                {
                    curves = new HashSet<TimelineWindowCurve>();
                    clipToCurve[clip] = curves;
                }

                curves.Add(targetCurve);
            }

            foreach (var keyValue in  clipToCurve)
            {
                SaveCurves(keyValue.Key, keyValue.Value, kEditCurveUndoLabel);
            }

            // If nothing is pasted, then we revert to old selection
            if (selectedKeyHashes.Count == 0)
                selectedKeyHashes = oldSelection;
            else
                ResampleAnimation();
        }

        public void ClearSelections()
        {
            ClearKeySelections();
            ClearHierarchySelection();
        }

        public void ClearKeySelections()
        {
            selectedKeyHashes.Clear();
            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
        }

        public void ClearHierarchySelection()
        {
            hierarchyState.selectedIDs.Clear();
            m_ActiveCurvesCache = null;
        }

        private void ClearCurveWrapperCache()
        {
            if (m_ActiveCurveWrappersCache == null)
                return;

            for (int i = 0; i < m_ActiveCurveWrappersCache.Length; ++i)
            {
                TimelineEditor.CurveWrapper curveWrapper = m_ActiveCurveWrappersCache[i];
                if (curveWrapper.renderer != null)
                    curveWrapper.renderer.FlushCache();
            }

            m_ActiveCurveWrappersCache = null;
        }

        private void ReloadModifiedDopelineCache()
        {
            if (m_dopelinesCache == null)
                return;

            for (int i = 0; i < m_dopelinesCache.Count; ++i)
            {
                DopeLine dopeLine = m_dopelinesCache[i];
                TimelineWindowCurve[] curves = dopeLine.curves;
                for (int j = 0; j < curves.Length; ++j)
                {
                    if (m_ModifiedCurves.Contains(curves[j].GetHashCode()))
                    {
                        dopeLine.InvalidateKeyframes();
                        break;
                    }
                }
            }
        }

        private void ReloadModifiedCurveWrapperCache()
        {
            if (m_ActiveCurveWrappersCache == null)
                return;

            Dictionary<int, TimelineWindowCurve> updateList = new Dictionary<int, TimelineWindowCurve>();

            for (int i = 0; i < m_ActiveCurveWrappersCache.Length; ++i)
            {
                TimelineEditor.CurveWrapper curveWrapper = m_ActiveCurveWrappersCache[i];

                if (m_ModifiedCurves.Contains(curveWrapper.id))
                {
                    TimelineWindowCurve curve = allCurves.Find(c => c.GetHashCode() == curveWrapper.id);
                    if (curve != null)
                    {
                        //  Boundaries have changed, invalidate all curves
                        if (curve.clip.startTime != curveWrapper.renderer.RangeStart() ||
                            curve.clip.stopTime != curveWrapper.renderer.RangeEnd())
                        {
                            ClearCurveWrapperCache();
                            return;
                        }
                        else
                        {
                            updateList[i] = curve;
                        }
                    }
                }
            }

            //  Only update curve wrappers that were modified.
            for (int i = 0; i < updateList.Count; ++i)
            {
                var entry = updateList.ElementAt(i);

                TimelineEditor.CurveWrapper curveWrapper = m_ActiveCurveWrappersCache[entry.Key];
                if (curveWrapper.renderer != null)
                    curveWrapper.renderer.FlushCache();

                // Recreate curve wrapper only if curve has been modified.
                m_ActiveCurveWrappersCache[entry.Key] = TimelineWindowUtility.GetCurveWrapper(entry.Value, entry.Value.clip);
            }
        }

        private void ReloadModifiedAnimationCurveCache()
        {
            for (int i = 0; i < allCurves.Count; ++i)
            {
                TimelineWindowCurve curve = allCurves[i];
                if (m_ModifiedCurves.Contains(curve.GetHashCode()))
                    curve.LoadKeyframes(curve.clip);
            }
        }

        // This is called when there is a new curve, but after the data refresh.
        // This means that hierarchynodes and dopeline(s) for new curve are already available.
        private void OnNewCurveAdded(EditorCurveBinding newCurve)
        {
            //  Retrieve group property name.
            //  For example if we got "position.z" as our newCurve,
            //  the property will be "position" with three child child nodes x,y,z
            string propertyName = newCurve.propertyName;
            string groupPropertyName = TimelineWindowUtility.GetPropertyGroupName(newCurve.propertyName);

            if (hierarchyData == null)
                return;

            if (HasHierarchySelection())
            {
                // Update hierarchy selection with newly created curve.
                foreach (TimelineWindowHierarchyNode node in hierarchyData.GetRows())
                {
                    if (node.path != newCurve.path ||
                        node.animatableObjectType != newCurve.type ||
                        (node.propertyName != propertyName && node.propertyName != groupPropertyName))
                        continue;

                    SelectHierarchyItem(node.id, true, false);

                    // We want the pptr curves to be in tall mode by default
                    if (newCurve.isPPtrCurve)
                        hierarchyState.AddTallInstance(node.id);
                }
            }

            //  Values do not change whenever a new curve is added, so we force an inspector update here.
            controlInterface.ResampleAnimation();

            m_lastAddedCurveBinding = null;
        }

        public void Repaint()
        {
            if (animEditor != null)
                animEditor.Repaint();
        }

        public List<TimelineWindowKeyframe> GetAggregateKeys(TimelineWindowHierarchyNode hierarchyNode)
        {
            DopeLine dopeline = dopelines.FirstOrDefault(e => e.hierarchyNodeID == hierarchyNode.id);
            if (dopeline == null)
                return null;
            return dopeline.keys;
        }

        public void OnHierarchySelectionChanged(int[] selectedInstanceIDs)
        {
            HandleHierarchySelectionChanged(selectedInstanceIDs, true);
        }

        public void HandleHierarchySelectionChanged(int[] selectedInstanceIDs, bool triggerSceneSelectionSync)
        {
            m_ActiveCurvesCache = null;

            if (triggerSceneSelectionSync)
                SyncSceneSelection(selectedInstanceIDs);
        }

        public void SelectHierarchyItem(DopeLine dopeline, bool additive)
        {
            SelectHierarchyItem(dopeline.hierarchyNodeID, additive, true);
        }

        public void SelectHierarchyItem(DopeLine dopeline, bool additive, bool triggerSceneSelectionSync)
        {
            SelectHierarchyItem(dopeline.hierarchyNodeID, additive, triggerSceneSelectionSync);
        }

        public void SelectHierarchyItem(int hierarchyNodeID, bool additive, bool triggerSceneSelectionSync)
        {
            if (!additive)
                ClearHierarchySelection();

            hierarchyState.selectedIDs.Add(hierarchyNodeID);

            int[] selectedInstanceIDs = hierarchyState.selectedIDs.ToArray();

            // We need to manually trigger this event, because injecting data to m_SelectedInstanceIDs directly doesn't trigger one via TreeView
            HandleHierarchySelectionChanged(selectedInstanceIDs, triggerSceneSelectionSync);
        }

        public void SelectHierarchyItems(IEnumerable<int> hierarchyNodeIDs, bool additive, bool triggerSceneSelectionSync)
        {
            if (!additive)
                ClearHierarchySelection();

            hierarchyState.selectedIDs.AddRange(hierarchyNodeIDs);

            int[] selectedInstanceIDs = hierarchyState.selectedIDs.ToArray();

            // We need to manually trigger this event, because injecting data to m_SelectedInstanceIDs directly doesn't trigger one via TreeView
            HandleHierarchySelectionChanged(selectedInstanceIDs, triggerSceneSelectionSync);
        }

        public void UnSelectHierarchyItem(DopeLine dopeline)
        {
            UnSelectHierarchyItem(dopeline.hierarchyNodeID);
        }

        public void UnSelectHierarchyItem(int hierarchyNodeID)
        {
            hierarchyState.selectedIDs.Remove(hierarchyNodeID);
        }

        public bool HasHierarchySelection()
        {
            if (hierarchyState.selectedIDs.Count == 0)
                return false;

            if (hierarchyState.selectedIDs.Count == 1)
                return (hierarchyState.selectedIDs[0] != 0);

            return true;
        }

        public HashSet<int> GetAffectedHierarchyIDs(List<TimelineWindowKeyframe> keyframes)
        {
            HashSet<int> hierarchyIDs = new HashSet<int>();

            foreach (TimelineWindowKeyframe keyframe in keyframes)
            {
                var curve = keyframe.curve;

                int hierarchyID = TimelineWindowUtility.GetPropertyNodeID(0, curve.path, curve.type, curve.propertyName);
                if (hierarchyIDs.Add(hierarchyID))
                {
                    string propertyGroupName = TimelineWindowUtility.GetPropertyGroupName(curve.propertyName);
                    hierarchyIDs.Add(TimelineWindowUtility.GetPropertyNodeID(0, curve.path, curve.type, propertyGroupName));
                }
            }

            return hierarchyIDs;
        }

        public List<TimelineWindowCurve> GetAffectedCurves(List<TimelineWindowKeyframe> keyframes)
        {
            List<TimelineWindowCurve> affectedCurves = new List<TimelineWindowCurve>();

            foreach (TimelineWindowKeyframe keyframe in keyframes)
                if (!affectedCurves.Contains(keyframe.curve))
                    affectedCurves.Add(keyframe.curve);

            return affectedCurves;
        }

        public DopeLine GetDopeline(int selectedInstanceID)
        {
            foreach (var dopeline in dopelines)
            {
                if (dopeline.hierarchyNodeID == selectedInstanceID)
                    return dopeline;
            }

            return null;
        }

        // Set scene active go to be the same as the one selected from hierarchy
        private void SyncSceneSelection(int[] selectedNodeIDs)
        {
            if (filterBySelection)
                return;

            if (!selection.canSyncSceneSelection)
                return;

            GameObject rootGameObject = selection.rootGameObject;
            if (rootGameObject == null)
                return;

            List<int> selectedGameObjectIDs = new List<int>(selectedNodeIDs.Length);
            foreach (var selectedNodeID in selectedNodeIDs)
            {
                // Skip nodes without associated curves.
                if (selectedNodeID == 0)
                    continue;

                TimelineWindowHierarchyNode node = hierarchyData.FindItem(selectedNodeID) as TimelineWindowHierarchyNode;

                if (node == null)
                    continue;

                if (node is TimelineWindowHierarchyMasterNode)
                    continue;

                Transform t = rootGameObject.transform.Find(node.path);

                // In the case of nested animation component, we don't want to sync the scene selection (case 569506)
                // When selection changes, animation window will always pick nearest animator component in terms of hierarchy depth
                // Automatically syncinc scene selection in nested scenarios would cause unintuitive clip & animation change for animation window so we check for it and deny sync if necessary

                if (t != null && rootGameObject != null && activeAnimationPlayer == TimelineWindowUtility.GetClosestAnimationPlayerComponentInParents(t))
                    selectedGameObjectIDs.Add(t.gameObject.GetInstanceID());
            }

            if (selectedGameObjectIDs.Count > 0)
                UnityEditor.Selection.instanceIDs = selectedGameObjectIDs.ToArray();
            else
                UnityEditor.Selection.activeGameObject = rootGameObject;
        }

        public float clipFrameRate
        {
            get
            {
                if (activeAnimationClip == null)
                    return 60.0f;
                return activeAnimationClip.frameRate;
            }
            set
            {
                // @TODO: Changing the clip in AnimationWindowState.frame rate feels a bit intrusive
                // Should probably be done explicitly from the UI and not go through AnimationWindowState...
                if (activeAnimationClip != null && value > 0 && value <= 10000)
                {
                    // Clear selection and save empty selection snapshot for undo consistency.
                    ClearKeySelections();
                    SaveKeySelection(kEditCurveUndoLabel);

                    // Reposition all keyframes to match the new sampling rate
                    foreach (var curve in selection.curves)
                    {
                        foreach (var key in curve.m_Keyframes)
                        {
                            int frame = TimelineKeyTime.Time(key.time, clipFrameRate).frame;
                            key.time = TimelineKeyTime.Frame(frame, value).time;
                        }
                    }

                    SaveCurves(activeAnimationClip, selection.curves, kEditCurveUndoLabel);

                    AnimationEvent[] events = AnimationUtility.GetAnimationEvents(activeAnimationClip);
                    foreach (AnimationEvent ev in events)
                    {
                        int frame = TimelineKeyTime.Time(ev.time, clipFrameRate).frame;
                        ev.time = TimelineKeyTime.Frame(frame, value).time;
                    }
                    AnimationUtility.SetAnimationEvents(activeAnimationClip, events);

                    activeAnimationClip.frameRate = value;
                }
            }
        }

        public float frameRate
        {
            get
            {
                return m_FrameRate;
            }
            set
            {
                if (m_FrameRate != value)
                {
                    m_FrameRate = value;
                    if (onFrameRateChange != null)
                        onFrameRateChange(m_FrameRate);
                }
            }
        }

        public TimelineKeyTime time { get { return controlInterface.time; } }
        public int currentFrame { get { return time.frame; } set { controlInterface.GoToFrame(value); } }
        public float currentTime { get { return time.time; } set { controlInterface.GoToTime(value); } }

        public UnityEditor.TimeArea.TimeFormat timeFormat { get { return TimelineWindowOptions.timeFormat; } set { TimelineWindowOptions.timeFormat = value; } }

        public UnityEditor.TimeArea timeArea
        {
            get { return m_TimeArea; }
            set { m_TimeArea = value; }
        }

        // Pixel to time ratio (used for time-pixel conversions)
        public float pixelPerSecond
        {
            get { return timeArea.m_Scale.x; }
        }

        // The GUI x-coordinate, where time==0 (used for time-pixel conversions)
        public float zeroTimePixel
        {
            get { return timeArea.shownArea.xMin * timeArea.m_Scale.x * -1f; }
        }

        public float PixelToTime(float pixel)
        {
            return PixelToTime(pixel, SnapMode.Disabled);
        }

        public float PixelToTime(float pixel, SnapMode snap)
        {
            float time = pixel - zeroTimePixel;
            return SnapToFrame(time / pixelPerSecond, snap);
        }

        public float TimeToPixel(float time)
        {
            return TimeToPixel(time, SnapMode.Disabled);
        }

        public float TimeToPixel(float time, SnapMode snap)
        {
            return SnapToFrame(time, snap) * pixelPerSecond + zeroTimePixel;
        }

        //@TODO: Move to animatkeytime??
        public float SnapToFrame(float time, SnapMode snap)
        {
            if (snap == SnapMode.Disabled)
                return time;

            float fps = (snap == SnapMode.SnapToFrame) ? frameRate : clipFrameRate;
            return SnapToFrame(time, fps);
        }

        public float SnapToFrame(float time, float fps)
        {
            float snapTime = Mathf.Round(time * fps) / fps;
            return snapTime;
        }

        public float minVisibleTime { get { return m_TimeArea.shownArea.xMin; } }
        public float maxVisibleTime { get { return m_TimeArea.shownArea.xMax; } }
        public float visibleTimeSpan { get { return maxVisibleTime - minVisibleTime; } }
        public float minVisibleFrame { get { return minVisibleTime * frameRate; } }
        public float maxVisibleFrame { get { return maxVisibleTime * frameRate; } }
        public float visibleFrameSpan { get { return visibleTimeSpan * frameRate; } }
        public float minTime { get { return timeRange.x; } }
        public float maxTime { get { return timeRange.y; } }

        public Vector2 timeRange
        {
            get
            {
                if (activeTimeline != null)
                    return new Vector2(0, activeTimeline.Duration);

                return Vector2.zero;
            }
        }

        public string FormatFrame(int frame, int frameDigits)
        {
            return (frame / (int)frameRate) + ":" + (frame % frameRate).ToString().PadLeft(frameDigits, '0');
        }

        //@TODO: Remove. Replace with animationkeytime
        public float TimeToFrame(float time)
        {
            return time * frameRate;
        }

        //@TODO: Remove. Replace with animationkeytime
        public float FrameToTime(float frame)
        {
            return frame / frameRate;
        }

        public int TimeToFrameFloor(float time)
        {
            return Mathf.FloorToInt(TimeToFrame(time));
        }

        public int TimeToFrameRound(float time)
        {
            return Mathf.RoundToInt(TimeToFrame(time));
        }

        public float FrameToPixel(float i, Rect rect)
        {
            return (i - minVisibleFrame) * rect.width / visibleFrameSpan;
        }

        public float FrameDeltaToPixel(Rect rect)
        {
            return rect.width / visibleFrameSpan;
        }

        public float TimeToPixel(float time, Rect rect)
        {
            return FrameToPixel(time * frameRate, rect);
        }

        public float PixelToTime(float pixelX, Rect rect)
        {
            return (pixelX * visibleTimeSpan / rect.width + minVisibleTime);
        }

        public float PixelDeltaToTime(Rect rect)
        {
            return visibleTimeSpan / rect.width;
        }
    }
}
