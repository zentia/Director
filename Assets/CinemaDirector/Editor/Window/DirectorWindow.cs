using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AGE;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using EditorExtension;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CinemaDirector
{
    [InitializeOnLoad]
    public partial class DirectorWindow : EditorWindow
    {
        private static DirectorWindow instance;
        public static DirectorWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GetWindow<DirectorWindow>();
                }
                return instance;
            }
        }

        [SerializeField]
        private SelectionHelper selection;
        public static SelectionHelper GetSelection()
        {
            if (Instance.selection == null)
                Instance.selection = new SelectionHelper();
            return Instance.selection;
        }
        public static DirectorControl directorControl;
        public Cutscene cutscene;
        private CutsceneWrapper cutsceneWrapper;
        private bool isSnappingEnabled;

        private const string PREVIEW_MODE = "预览模式";
        private const string CREATE = "创建";
        private const string SAVE = "保存";
        private const string SAVEAS = "另存为";

        const string TITLE = "Timeline";
        const string MENU_ITEM = "OSGame/Timeline %g";

        private Texture settingsImage = null;
        private Texture rescaleImage = null;
        private Texture zoomInImage = null;
        private Texture zoomOutImage = null;
        private Texture snapImage = null;
        private Texture rollingEditImage = null;
        private Texture rippleEditImage = null;
        private Texture pickerImage = null;
        private Texture refreshImage = null;
        private Texture titleImage = null;
        private Texture cropImage = null;
        private Texture scaleImage = null;

        private const float TOOLBAR_HEIGHT = 17f;
        private const string PRO_SKIN = "Director_LightSkin";
        private const string FREE_SKIN = "Director_DarkSkin";

        private const string SETTINGS_ICON = "Director_SettingsIcon";
        private const string HORIZONTAL_RESCALE_ICON = "Director_HorizontalRescaleIcon";
        private const string PICKER_ICON = "Director_PickerIcon";
        private const string REFRESH_ICON = "Director_RefreshIcon";
        private const string MAGNET_ICON = "Director_Magnet";
        private const string ZOOMIN_ICON = "Director_ZoomInIcon";
        private const string ZOOMOUT_ICON = "Director_ZoomOutIcon";
        private const string TITLE_ICON = "Director_Icon";

        private float m_touchTime;
        private bool isRepaint;
       
        public void Update()
        {
            if (!isRepaint)
                Repaint();
            isRepaint = !isRepaint;
            if (selection != null)
            {
                selection.Repaint();
            }
            if (cutscene == null)
            {
                if (Application.isPlaying && directorControl.InPreviewMode)
                {
                    var runningActions = ActionService.instance.HistoryRunningActions;
                    if (runningActions.Count > 0)
                    {
                        var runningAction = runningActions[runningActions.Count - 1];
                        CreateAge(runningAction);
                        DirectorControl_PlayCutscene(directorControl, new CinemaDirectorArgs(cutscene));
                        cutscene.PlantingBomb(runningAction.Length, DestroyCutscene);
                    }
                }
            }
        }

        private void DestroyCutscene()
        {
            if (cutscene == null)
            {
                return;
            }
            cutscene.Destroy();
            cutscene = null;
        }

        protected void OnEnable()
        {
            instance = this;
            EditorApplication.playmodeStateChanged = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.playmodeStateChanged, new EditorApplication.CallbackFunction(PlaymodeStateChanged));
            EditorSceneManager.activeSceneChangedInEditMode += EditorSceneManagerOnactiveSceneChanged;
            GUISkin skin = CreateInstance<GUISkin>();
            skin = EditorGUIUtility.isProSkin ? Resources.Load<GUISkin>(PRO_SKIN) : Resources.Load<GUISkin>(FREE_SKIN);
            LoadTextures();
            titleContent = new GUIContent(TITLE, titleImage);
            directorControl = new DirectorControl();
            directorControl.OnLoad(skin);
            directorControl.PlayCutscene += DirectorControl_PlayCutscene;
            directorControl.PauseCutscene += DirectorControl_PauseCutscene;
            directorControl.StopCutscene += directorControl_StopCutscene;
            directorControl.ScrubCutscene += directorControl_ScrubCutscene;
            directorControl.TouchCutscene += DirectorControlTouchCutscene;
            directorControl.SetCutsceneTime += directorControl_SetCutsceneTime;
            directorControl.EnterPreviewMode += directorControl_EnterPreviewMode;
            directorControl.ExitPreviewMode += DirectorControl_ExitPreviewMode;
            directorControl.DragPerformed += directorControl_DragPerformed;
            isSnappingEnabled = directorControl.IsSnappingEnabled;
            directorControl.RepaintRequest += directorControl_RepaintRequest;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            AddGlobalEventHandler(OnModifierKeysChanged);
            EditorHooker.OnEditorFocusChanged += OnEditorFocusChanged;
            DirectorEvent.ExecuteCoroutines.AddListener(ExecuteCoroutines);
            DirectorEvent.StopCoroutines.AddListener(DestroyCoroutines);
            LoadLastAge();
        }

        public void OnDisable()
        {
            directorControl.OnDisable();
            if (Application.isEditor && cutscene != null)
            {
                cutscene.ExitPreviewMode();
            }
            if (cutscene != null)
            {
                if (cutscene.Dirty)
                {
                    CheckModifyCutscene(1);
                }
                EditorPrefs.SetString("DirectorControl.CutscenePath", cutscene.actionName);
            }
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            instance = null;
            EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManagerOnactiveSceneChanged;
            EditorHooker.OnEditorFocusChanged -= OnEditorFocusChanged;
            RemoveGlobalEventHandler(OnModifierKeysChanged);
            DirectorEvent.ExecuteCoroutines.RemoveListener(ExecuteCoroutines);
            DirectorEvent.StopCoroutines.RemoveListener(DestroyCoroutines);
        }

        private void ExecuteCoroutines(CoroutinesEvent coroutinesEvent)
        {
            this.StartCoroutine(coroutinesEvent());
        }

        private void DestroyCoroutines(CoroutinesEvent coroutinesEvent)
        {
            this.StopCoroutine(coroutinesEvent());
        }

        private void LoadLastAge()
        {
            if (cutscene != null)
                return;
            string cutscenePath = string.Empty;
            if (EditorPrefs.HasKey(DirectorControlSettings.CUTSCENEPATH))
            {
                cutscenePath = EditorPrefs.GetString(DirectorControlSettings.CUTSCENEPATH);
            }

            if (!string.IsNullOrEmpty(cutscenePath))
            {
                this.StartCoroutine(CreateAge(cutscenePath));
            }
        }

        private void OnEditorFocusChanged(bool focused)
        {
            if (focused && cutscene && cutscene.DetectExternalChanged(directorControl.Settings.assetsPath))
            {
                if (EditorUtility.DisplayDialog("文件发生变化","AGE文件发生，是否重载？","重载","取消"))
                {
                    this.StartCoroutine(CreateAge(cutscene.actionName));
                }
            }
        }

        private void AddGlobalEventHandler(EditorApplication.CallbackFunction action)
        {
            FieldInfo globalEventHandler = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
            var value = (EditorApplication.CallbackFunction)globalEventHandler.GetValue(null);
            value += action;
            globalEventHandler.SetValue(null, value);
        }

        private void RemoveGlobalEventHandler(EditorApplication.CallbackFunction action)
        {
            FieldInfo globalEventHandler = typeof(EditorApplication).GetField("globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);
            var value = (EditorApplication.CallbackFunction)globalEventHandler.GetValue(null);
            value -= action;
            globalEventHandler.SetValue(null, value);
        }


        void UndoRedoPerformed()
        {

        }

        void directorControl_RepaintRequest(object sender, CinemaDirectorArgs e)
        {
            Repaint();
        }

        #region EventHandlers

        void directorControl_DragPerformed(object sender, CinemaDirectorDragArgs e)
        {
            Cutscene c = e.cutscene as Cutscene;
            if (c != null)
            {
                if (e.references != null)
                {
                    if (e.references.Length == 1)
                    {
                        GameObject gameObject = e.references[0] as GameObject;
                        if (gameObject != null)
                        {
                            
                        }
                    }
                }
            }
        }

        private void DirectorControl_ExitPreviewMode(object sender, CinemaDirectorArgs e)
        {
            Cutscene c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.ExitPreviewMode();
            }
        }

        void directorControl_EnterPreviewMode(object sender, CinemaDirectorArgs e)
        {
            Cutscene c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.EnterPreviewMode();
            }
        }

        void directorControl_SetCutsceneTime(object sender, CinemaDirectorArgs e)
        {
            Cutscene c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.SetRunningTime(e.timeArg);
                cutsceneWrapper.RunningTime = e.timeArg;
            }
        }

        void directorControl_ScrubCutscene(object sender, CinemaDirectorArgs e)
        {
            var c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.ScrubToTime(e.timeArg);
            }
        }

        private void DirectorControlTouchCutscene(object sender, CinemaDirectorArgs e)
        {
            m_touchTime = e.timeArg;
        }

        void directorControl_StopCutscene(object sender, CinemaDirectorArgs e)
        {
            Cutscene c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.Stop();
            }
        }

        void DirectorControl_PauseCutscene(object sender, CinemaDirectorArgs e)
        {
            Cutscene c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.Pause();
            }
        }

        private void DirectorControl_PlayCutscene(object sender, CinemaDirectorArgs e)
        {
            var c = e.cutscene as Cutscene;
            if (c != null)
            {
                c.Play();
            }
        }

        #endregion

        /// <summary>
        /// Draws the GUI for the Timeline Window.
        /// </summary>
        protected void OnGUI()
        {
            Rect controlArea = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);
            
            UpdateToolbar();

            cutsceneWrapper = DirectorHelper.UpdateWrapper(cutscene, cutsceneWrapper);
            switch (Event.current.keyCode)
            {
                case KeyCode.RightArrow:
                    directorControl.OnScrubCutscene(0.01f);
                    break;
                case KeyCode.LeftArrow:
                    directorControl.OnScrubCutscene(-0.01f);
                    break;
            }
            directorControl.OnGUI(controlArea, cutsceneWrapper, position);
            DirectorHelper.ReflectChanges(cutscene, cutsceneWrapper);
        }

        private void OnModifierKeysChanged()
        {
            directorControl.OnGlobalEvent();
        }

        private IEnumerable<string> GetAllAgePathes()
        {
            Profiler.BeginSample("GetAllAgePathes");
            var pathes = Directory.GetFiles(directorControl.Settings.assetsPath, "*.xml", SearchOption.AllDirectories);

            var selecter = pathes.Select(path => path.RelativeTo(directorControl.Settings.assetsPath));
            Profiler.EndSample();
            return selecter;
        }

        private void chooseResizeOption(object userData)
        {
            int selection = (int)userData;

            directorControl.ResizeOption = (ResizeOption)selection;
        }

        public void FocusCutscene(Cutscene cutscene)
        {
            if (this.cutscene != null)
            {
                this.cutscene.ExitPreviewMode();
            }
            directorControl.InPreviewMode = false;

            EditorPrefs.SetString(DirectorControlSettings.CUTSCENEPATH, cutscene.actionName);
            this.cutscene = cutscene;
        }

        public void PlaymodeStateChanged()
        {
            directorControl.InPreviewMode = false;
        }

        private void EditorSceneManagerOnactiveSceneChanged(Scene arg0, Scene arg1)
        {
            CheckModifyCutscene(1);
        }

        private void OnDestroy()
        {
            directorControl.OnDestroy();
            if (cutscene != null)
                cutscene.Destroy();
            if (selection != null)
            {
                selection.Destroy();
            }
        }

        private void LoadTextures()
        {
            string suffix = EditorGUIUtility.isProSkin ? "_Light" : "_Dark";
            string missing = " is missing from Resources folder.";

            settingsImage = Resources.Load<Texture>(SETTINGS_ICON + suffix);
            if (settingsImage == null)
            {
                Debug.Log(SETTINGS_ICON + suffix + missing);
            }

            rescaleImage = Resources.Load<Texture>(HORIZONTAL_RESCALE_ICON + suffix);
            if (rescaleImage == null)
            {
                Debug.Log(HORIZONTAL_RESCALE_ICON + suffix + missing);
            }

            zoomInImage = Resources.Load<Texture>(ZOOMIN_ICON + suffix);
            if (zoomInImage == null)
            {
                Debug.Log(ZOOMIN_ICON + suffix + missing);
            }

            zoomOutImage = Resources.Load<Texture>(ZOOMOUT_ICON + suffix);
            if (zoomOutImage == null)
            {
                Debug.Log(ZOOMOUT_ICON + suffix + missing);
            }

            snapImage = Resources.Load<Texture>(MAGNET_ICON + suffix);
            if (snapImage == null)
            {
                Debug.Log(MAGNET_ICON + suffix + missing);
            }

            rollingEditImage = Resources.Load<Texture>("Director_RollingIcon");
            if (rollingEditImage == null)
            {
                Debug.Log("Rolling edit icon missing from Resources folder.");
            }

            rippleEditImage = Resources.Load<Texture>("Director_RippleIcon");
            if (rippleEditImage == null)
            {
                Debug.Log("Ripple edit icon missing from Resources folder.");
            }

            pickerImage = Resources.Load<Texture>(PICKER_ICON + suffix);
            if (pickerImage == null)
            {
                Debug.Log(PICKER_ICON + suffix + missing);
            }

            refreshImage = Resources.Load<Texture>(REFRESH_ICON + suffix);
            if (refreshImage == null)
            {
                Debug.Log(REFRESH_ICON + suffix + missing);
            }

            titleImage = Resources.Load<Texture>(TITLE_ICON + suffix);
            if (titleImage == null)
            {
                Debug.Log(TITLE_ICON + suffix + missing);
            }

            cropImage = Resources.Load<Texture>("Director_Resize_Crop" + suffix);
            if (cropImage == null)
            {
                Debug.Log("Director_Resize_Crop" + suffix + missing);
            }

            scaleImage = Resources.Load<Texture>("Director_Resize_Scale" + suffix);
            if (scaleImage == null)
            {
                Debug.Log("Director_Resize_Crop" + suffix + missing);
            }
        }

        private void AddTrackGroup(object userData)
        {
            TrackGroupContextData data = userData as TrackGroupContextData;
            if (data != null)
            {
                var item = CutsceneItemFactory.CreateTrackGroup(cutscene, data.Type, data.Label);
                Undo.RegisterCreatedObjectUndo(item, string.Format("Create {0}", item.name));
            }
        }

        private TrackGroupContextData getContextDataFromType(Type type)
        {
            string label = string.Empty;
            foreach (TrackGroupAttribute attribute in type.GetCustomAttributes(typeof(TrackGroupAttribute), true))
            {
                if (attribute != null)
                {
                    label = attribute.Label;
                    break;
                }
            }
            TrackGroupContextData userData = new TrackGroupContextData { Type = type, Label = label };
            return userData;
        }

        [MenuItem(MENU_ITEM, false, 10)]
        private static void ShowWindow()
        {
            var window = GetWindow(typeof(DirectorWindow));
        }

        private void ShowCreateMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("New Age"), false, data =>
            {
                this.StartCoroutine(CreateAge(""));    
            }, null);
            if (cutscene != null)
            {
                menu.AddItem(new GUIContent("New Track Group"), false, data =>
                {
                    if (cutscene != null)
                    {
                        var trackGroup = cutscene.CreateChild();
                        trackGroup.name = "New Track Group";
                    }
                }, null);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("New Track Group"));
            }
            menu.ShowAsContext();
        }

        private IEnumerator CreateAge(string path)
        {
            yield return null;
            CheckModifyCutscene(1);
            DestroyCutscene();
            cutscene = DirectorHelper.LoadCutscene(path, true, directorControl.Settings.assetsPath);
        }

        public void CreateAgePlaying(string action)
        {
            CheckModifyCutscene(1);
            cutscene = DirectorHelper.LoadCutscene($"RawAssets/{action}", false, directorControl.Settings.assetsPath);
        }

        private class TrackGroupContextData
        {
            public Type Type;
            public string Label;
        }
    }
}