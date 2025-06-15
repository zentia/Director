using System;
using UnityEngine;
using System.Collections.Generic;
using TimelineEditorInternal;
using TimelineRuntime;
using UnityEditor;
using Object = UnityEngine.Object;

namespace TimelineEditor
{
    [EditorWindowTitle(title = "Timeline", useTypeNameAsIconName = true)]
    public sealed class NewTimelineWindow : EditorWindow, IHasCustomMenu
    {
        // Active Animation windows
        private static List<NewTimelineWindow> s_TimelineWindows = new List<NewTimelineWindow>();
        internal static List<NewTimelineWindow> GetAllTimelineWindows() { return s_TimelineWindows; }

        private TimeEditor m_AnimEditor;

        [SerializeField]
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        [SerializeField] private int m_LastSelectedObjectID;

        private GUIStyle m_LockButtonStyle;
        private GUIContent m_DefaultTitleContent;
        private GUIContent m_RecordTitleContent;

        internal TimeEditor animEditor
        {
            get
            {
                return m_AnimEditor;
            }
        }

        internal TimelineWindowState state
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state;
                }
                return null;
            }
        }

        public AnimationClip animationClip
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.activeAnimationClip;
                }
                return null;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.activeAnimationClip = value;
                }
            }
        }

        public bool previewing
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.previewing;
                }
                return false;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    if (value)
                        m_AnimEditor.state.StartPreview();
                    else
                        m_AnimEditor.state.StopPreview();
                }
            }
        }

        public bool canPreview
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.canPreview;
                }

                return false;
            }
        }

        public bool recording
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.recording;
                }
                return false;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    if (value)
                        m_AnimEditor.state.StartRecording();
                    else
                        m_AnimEditor.state.StopRecording();
                }
            }
        }

        public bool canRecord
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.canRecord;
                }

                return false;
            }
        }

        public bool playing
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.playing;
                }
                return false;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    if (value)
                        m_AnimEditor.state.StartPlayback();
                    else
                        m_AnimEditor.state.StopPlayback();
                }
            }
        }

        public float time
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.currentTime;
                }
                return 0.0f;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.currentTime = value;
                }
            }
        }

        public int frame
        {
            get
            {
                if (m_AnimEditor != null)
                {
                    return m_AnimEditor.state.currentFrame;
                }
                return 0;
            }
            set
            {
                if (m_AnimEditor != null)
                {
                    m_AnimEditor.state.currentFrame = value;
                }
            }
        }

        private NewTimelineWindow()
        {}

        internal void ForceRefresh()
        {
            if (m_AnimEditor != null)
            {
                m_AnimEditor.state.ForceRefresh();
            }
        }

        void OnEnable()
        {
            if (m_AnimEditor == null)
            {
                m_AnimEditor = CreateInstance(typeof(TimeEditor)) as TimeEditor;
                m_AnimEditor.hideFlags = HideFlags.HideAndDontSave;
            }

            s_TimelineWindows.Add(this);
            titleContent = GetLocalizedTitleContent();

            m_DefaultTitleContent = titleContent;
            m_RecordTitleContent = EditorGUIUtility.TextContentWithIcon(titleContent.text, "Animation.Record");

            OnSelectionChange();

            Undo.undoRedoEvent += UndoRedoPerformed;
        }

        void OnDisable()
        {
            s_TimelineWindows.Remove(this);
            m_AnimEditor.OnDisable();

            Undo.undoRedoEvent -= UndoRedoPerformed;
        }

        void OnDestroy()
        {
            DestroyImmediate(m_AnimEditor);
        }

        void Update()
        {
            if (m_AnimEditor == null)
                return;

            m_AnimEditor.Update();
        }

        void OnGUI()
        {
            if (m_AnimEditor == null)
                return;

            titleContent = m_AnimEditor.state.recording ? m_RecordTitleContent : m_DefaultTitleContent;
            m_AnimEditor.OnAnimEditorGUI(this, position);
        }

        internal void OnSelectionChange()
        {
            if (m_AnimEditor == null)
                return;

            Object activeObject = Selection.activeObject;

            bool restoringLockedSelection = false;
            if (m_LockTracker.isLocked && m_AnimEditor.stateDisabled)
            {
                activeObject = EditorUtility.InstanceIDToObject(m_LastSelectedObjectID);
                restoringLockedSelection = true;
                m_LockTracker.isLocked = false;
            }

            GameObject activeGameObject = activeObject as GameObject;
            if (activeGameObject != null)
            {
                EditGameObject(activeGameObject);
            }
            else
            {
                Transform activeTransform = activeObject as Transform;
                if (activeTransform != null)
                {
                    EditGameObject(activeTransform.gameObject);
                }
                else
                {
                    AnimationClip activeAnimationClip = activeObject as AnimationClip;
                    if (activeAnimationClip != null)
                        EditAnimationClip(activeAnimationClip);
                }
            }

            if (restoringLockedSelection && !m_AnimEditor.stateDisabled)
            {
                m_LockTracker.isLocked = true;
            }
        }

        void OnFocus()
        {
            OnSelectionChange();
        }

        internal void OnControllerChange()
        {
            // Refresh selectedItem to update selected clips.
            OnSelectionChange();
        }

        void OnLostFocus()
        {
            if (m_AnimEditor != null)
                m_AnimEditor.OnLostFocus();
        }

        [MenuItem("Window/Timeline/NewEditor", false, 10)]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<NewTimelineWindow>();
        }

        internal bool EditGameObject(GameObject gameObject)
        {
            return EditGameObjectInternal(gameObject, (ITimelineWindowControl)null);
        }

        internal bool EditAnimationClip(AnimationClip animationClip)
        {
            if (state.linkedWithSequencer == true)
                return false;

            EditAnimationClipInternal(animationClip, (Object)null, (ITimelineWindowControl)null);
            return true;
        }

        internal bool EditSequencerClip(AnimationClip animationClip, Object sourceObject, ITimelineWindowControl controlInterface)
        {
            EditAnimationClipInternal(animationClip, sourceObject, controlInterface);
            state.linkedWithSequencer = true;
            return true;
        }

        internal void UnlinkSequencer()
        {
            if (state.linkedWithSequencer)
            {
                state.linkedWithSequencer = false;

                // Selected object could have been changed when unlocking the animation window
                EditAnimationClip(null);
                OnSelectionChange();
            }
        }

        private bool EditGameObjectInternal(GameObject gameObject, ITimelineWindowControl controlInterface)
        {
            if (EditorUtility.IsPersistent(gameObject))
                return false;

            if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            var newSelection = GameObjectSelectionItem.Create(gameObject);
            if (ShouldUpdateGameObjectSelection(newSelection))
            {
                m_AnimEditor.selection = newSelection;
                m_AnimEditor.overrideControlInterface = controlInterface;

                m_LastSelectedObjectID = gameObject != null ? gameObject.GetInstanceID() : 0;
            }
            else
                m_AnimEditor.OnSelectionUpdated();

            return true;
        }

        private void EditAnimationClipInternal(AnimationClip animationClip, Object sourceObject, ITimelineWindowControl controlInterface)
        {
            var newSelection = TimelineClipSelectionItem.Create(animationClip, sourceObject);
            if (ShouldUpdateSelection(newSelection))
            {
                m_AnimEditor.selection = newSelection;
                m_AnimEditor.overrideControlInterface = controlInterface;

                m_LastSelectedObjectID = animationClip != null ? animationClip.GetInstanceID() : 0;
            }
            else
                m_AnimEditor.OnSelectionUpdated();
        }

        void ShowButton(Rect r)
        {
            if (m_LockButtonStyle == null)
                m_LockButtonStyle = "IN LockButton";

            EditorGUI.BeginChangeCheck();

            m_LockTracker.ShowButton(r, m_LockButtonStyle, m_AnimEditor.stateDisabled);

            // Selected object could have been changed when unlocking the animation window
            if (EditorGUI.EndChangeCheck())
                OnSelectionChange();
        }

        private bool ShouldUpdateGameObjectSelection(GameObjectSelectionItem selectedItem)
        {
            if (m_LockTracker.isLocked)
                return false;

            if (state.linkedWithSequencer)
                return false;

            // Selected game object with no animation player.
            if (selectedItem.rootGameObject == null)
                return true;

            TimelineWindowSelectionItem currentSelection = m_AnimEditor.selection;

            // Game object holding animation player has changed.  Update selection.
            if (selectedItem.rootGameObject != currentSelection.rootGameObject)
                return true;

            // No clip in current selection, favour new selection.
            if (currentSelection.animationClip == null)
                return true;

            // Make sure that animation clip is still referenced in animation player.
            if (currentSelection.rootGameObject != null)
            {
                AnimationClip[] allClips = AnimationUtility.GetAnimationClips(currentSelection.rootGameObject);
                if (!Array.Exists(allClips, x => x == currentSelection.animationClip))
                    return true;
            }

            return false;
        }

        private bool ShouldUpdateSelection(TimelineWindowSelectionItem selectedItem)
        {
            if (m_LockTracker.isLocked)
                return false;

            TimelineWindowSelectionItem currentSelection = m_AnimEditor.selection;
            return (selectedItem.GetRefreshHash() != currentSelection.GetRefreshHash());
        }

        private void UndoRedoPerformed(in UndoRedoInfo info)
        {
            Repaint();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu, m_AnimEditor.stateDisabled);
        }
    }
}
