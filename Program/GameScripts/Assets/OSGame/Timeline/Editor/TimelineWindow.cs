using System;
using System.Linq;
using Assets.Plugins.Common;
using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimelineEditor
{
    public class TimelineWindow : EditorWindow
    {
        private const string TITLE = "Timeline";
        private const string MENU_ITEM = "Window/Timeline/Editor %T";

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
        private const string AutoKeyFrameIcon = "Timeline_AutoKeyFrame";

        public static Texture settingsImage;
        public static Texture rescaleImage;
        public static Texture zoomInImage;
        public static Texture zoomOutImage;
        public static Texture snapImage;
        public static Texture rollingEditImage;
        public static Texture rippleEditImage;
        public static Texture pickerImage;
        public static Texture refreshImage;
        public static Texture titleImage;
        public static Texture cropImage;
        public static Texture scaleImage;
        public static Texture ms_AutoKeyFrameImage;
        private static Texture ms_BindImage;
        private static byte delayRepaint;
        private readonly GUIContent newTimeline = new ("New timeline");
        private Timeline m_Timeline;
        private TimelineControl _control;
        private uint m_FrameCount;

        public Timeline timeline => m_Timeline;
        private TimelineCameraAC m_CameraAC;

        public void Awake()
        {
            titleContent = new GUIContent(TITLE, titleImage);
            minSize = new Vector2(600f, 400f);
            LoadTextures();

            m_CameraAC = new TimelineCameraAC(this);
        }

        protected void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlaymodeStateChanged;
            var skin = EditorGUIUtility.isProSkin ? Resources.Load<GUISkin>(PRO_SKIN) : Resources.Load<GUISkin>(FREE_SKIN);
            LoadTextures();
            titleContent = new GUIContent(TITLE, titleImage);
            _control = new TimelineControl();
            _control.OnLoad(skin);
            _control.PlayTimeline += TimelineControlPlayTimeline;
            _control.PauseTimeline += TimelineControlPauseTimeline;
            _control.ScrubTimeline = TimelineControlScrubTimeline;
            _control.SetTimelineFrame = TimelineControlSetTimelineTime;
            _control.EnterPreviewMode += TimelineControlStartPreview;
            _control.ExitPreviewMode += TimelineControlStopPreview;
            _control.DragPerformed += TimelineControlDragPerformed;
            _control.Repaint = TimelineControlRepaint;
        }

        public void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlaymodeStateChanged;
            _control.OnDisable();
            if (!Application.isPlaying && m_Timeline != null)
                m_Timeline.ExitPreviewMode();
        }

        protected void OnGUI()
        {
            if (m_Timeline == null)
            {
                SelectTimeline(FindObjectOfType<Timeline>(true));
            }
            var toolbarArea = new Rect(0, 0, position.width, TOOLBAR_HEIGHT);
            var controlArea = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);
            UpdateToolbar(toolbarArea);
            TimelineHelper.UpdateWrapper(m_Timeline, _control);
            _control.OnGUI(controlArea);
        }

        private void Update()
        {
            if (m_Timeline == null)
            {
                return;
            }
            if (m_Timeline.state is Timeline.TimelineState.PreviewPlaying or Timeline.TimelineState.Playing)
            {
                if (!Application.isPlaying)
                {
                    m_Timeline.UpdateTimeline(Time.deltaTime);
                }
                UpdateRepaint();
                return;
            }

            if (_control.InPreviewMode && _control.IsEditing)
            {
                UpdateRepaint();
                return;
            }

            if (delayRepaint > 0)
            {
                delayRepaint--;
                if (delayRepaint == 0)
                {
                    Repaint();
                }
            }
        }

        private void UpdateRepaint()
        {
            if (++m_FrameCount < 10)
                return;
            Repaint();
            m_FrameCount = 0;
        }

        private void TimelineControlRepaint()
        {
            Repaint();
        }

        public static void TimelineWindowRepaint()
        {
            delayRepaint = 10;
        }

        private static void TimelineControlDragPerformed(object sender, TimelineDragArgs e)
        {
            var c = e.behaviour as Timeline;
            if (c == null)
            {
                return;
            }

            if (e.references != null)
            {
                if (e.references.Length == 1)
                {
                    var gameObject = e.references[0] as GameObject;
                    if (gameObject != null)
                    {
                        var atg = TimelineFactory.CreateTrackGroup(c, typeof(ActorTrackGroup), gameObject.name) as ActorTrackGroup;
                        atg.Actors.Clear();
                        atg.Actors.Add(gameObject.GetComponent<Transform>());
                        Undo.RegisterCreatedObjectUndo(atg.gameObject, $"Created {atg.gameObject.name}");
                    }
                }
            }
        }

        private void TimelineControlStopPreview()
        {
            m_Timeline?.ExitPreviewMode();
        }

        private void TimelineControlStartPreview()
        {
            m_Timeline?.EnterPreviewMode();
        }

        private void TimelineControlSetTimelineTime(short frame)
        {
            if (m_Timeline != null)
            {
                m_Timeline.SetRunningTime(frame / 30.0f);
                Repaint();
            }
        }

        private void TimelineControlScrubTimeline(short frame)
        {
            if (m_Timeline)
            {
                m_Timeline.ScrubToTime(frame/30.0f);
            }
        }

        private void TimelineControlPauseTimeline(object sender, TimelineArgs e)
        {
            var c = e.behaviour as Timeline;
            c?.Pause();
        }

        private void TimelineControlPlayTimeline(object sender, TimelineArgs e)
        {
            var c = e.behaviour as Timeline;
            if (c == null)
            {
                return;
            }

            Time.timeScale = 1;
            if (Application.isPlaying)
            {
                c.Play(true);
            }
            else
            {
                c.PreviewPlay();
            }
        }

        private static Rect Step(ref Rect rect, float value)
        {
            rect.x += rect.width;
            rect.width = value;
            return rect;
        }

        private OdinSelector<string> CreateSelector(Rect rect)
        {
            return TimelineAssetSelector.Create(selection =>
            {
                var path = selection.FirstOrDefault();
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                _control.InPreviewMode = false;
                SelectTimeline((PrefabUtility.InstantiatePrefab(prefab) as GameObject).GetComponent<Timeline>());
            }, TimelineHelper.RootDirList, 400);
        }

        private static void Snap(Timeline timeline)
        {
            if (timeline.inGame)
                return;
            var actorTrackGroups = timeline.actorTrackGroups;
            var activeScene = SceneManager.GetActiveScene();
            var rootGameObjects = activeScene.GetRootGameObjects();
            foreach (var rootGameObject in rootGameObjects)
            {
                if (rootGameObject == timeline.gameObject)
                {
                    continue;
                }

                if (rootGameObject.GetComponent<Timeline>() != null)
                {
                    continue;
                }
                if (rootGameObject.name.EndsWith("(Clone)"))
                    continue;
                foreach (var actorTrackGroup in actorTrackGroups)
                {
                    var child = rootGameObject.FindChildBFS(actorTrackGroup.name);
                    if (child == null)
                    {
                        // if (actorTrackGroup.name is "Camera" or "Camera_AC" or "Camera_AC_IPad")
                        if(IsCameraAC(actorTrackGroup.name))
                        {
                            if (Camera.main)
                                child = Camera.main.gameObject;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    actorTrackGroup.Actors.Clear();
                    if (child != null && child.GetComponent<ActorTrackGroup>() == null)
                    {
                        actorTrackGroup.Actors.Add(child.transform);
                    }
                }
            }
        }

        private static bool IsCameraAC(string name)
        {
            if (name == "Camera")
                return true;

            foreach (var kv in ScreenFitConfig.GetTimelineTrackNameDict(ScreenFitConfig.TimelineTrackType.Camera))
            {
                if (kv.Value == name)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateToolbar(Rect toolbarArea)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var area = new Rect(toolbarArea.x, toolbarArea.y, 0, toolbarArea.height);
            GenericSelector<string>.DrawSelectorDropdown(Step(ref area,50), "Open", CreateSelector, EditorStyles.toolbarDropDown);
            if (GUI.Button(Step(ref area,60), "Create", EditorStyles.toolbarDropDown))
            {
                var createMenu = new GenericMenu();
                createMenu.AddItem(newTimeline, false, CreateTimeline);

                if (m_Timeline != null)
                {
                    createMenu.AddSeparator(string.Empty);

                    var subTypes = TimelineHelper.GetAllSubTypes(typeof(TrackGroup));
                    for (var i = 0; i < subTypes.Length; i++)
                    {
                        var userData = getContextDataFromType(subTypes[i]);
                        var text = string.Format(userData.Label);
                        createMenu.AddItem(new GUIContent(text), false, AddTrackGroup, userData);
                    }
                }

                createMenu.DropDown(new Rect(5, TOOLBAR_HEIGHT, 0, 0));
            }

            if (GUI.Button(Step(ref area, 60), "Save", EditorStyles.toolbarButton))
            {
                SaveTimeline();
            }

            if (GUI.Button(Step(ref area, 60), "Destroy", EditorStyles.toolbarButton))
            {
                DestroyTimeline();
            }

            var timelineName = m_Timeline != null ? m_Timeline.name : "None";
            GenericSelector<Timeline>.DrawSelectorDropdown(Step(ref area,200), timelineName, _ => TimelineSelector.Create(selection => SelectTimeline(selection.FirstOrDefault())),EditorStyles.toolbarDropDown);
            if (GUI.Button(Step(ref area,24), pickerImage, EditorStyles.toolbarButton))
                Selection.activeObject = m_Timeline;
            if (GUI.Button(Step(ref area, 24), refreshImage, EditorStyles.toolbarButton))
            {
                _control.InPreviewMode = false;
                _control.InPreviewMode = true;
            }

            var resizeTexture = cropImage;
            if (_control.ResizeOption == ResizeOption.Scale)
                resizeTexture = scaleImage;
            if (GUI.Button(Step(ref area, 32), new GUIContent(resizeTexture, "Resize Option"), EditorStyles.toolbarDropDown))
            {
                var resizeMenu = new GenericMenu();
                var names = Enum.GetNames(typeof(ResizeOption));
                for (var i = 0; i < names.Length; i++)
                    resizeMenu.AddItem(new GUIContent(names[i]), _control.ResizeOption == (ResizeOption)i, ChooseResizeOption, i);
                resizeMenu.DropDown(new Rect(area.x, TOOLBAR_HEIGHT, 0, 0));
            }

            if (GUI.Button(Step(ref area,24), snapImage, EditorStyles.toolbarButton))
            {
                Snap(m_Timeline);
            }
            if (GUI.Button(Step(ref area, 24),rescaleImage, EditorStyles.toolbarButton))
                _control.Rescale();
            if (GUI.Button(Step(ref area, 24), new GUIContent(zoomInImage, "Zoom In"), EditorStyles.toolbarButton))
                _control.ZoomIn();
            if (GUI.Button(Step(ref area, 24),zoomOutImage, EditorStyles.toolbarButton))
                _control.ZoomOut();
            var temp = GUI.color;
            GUI.color = _control.InPreviewMode ? Color.red : temp;
            _control.InPreviewMode = GUI.Toggle(Step(ref area, 60),_control.InPreviewMode, "Preview", EditorStyles.toolbarButton);
            GUI.color = temp;
            _control.ShowCurves = GUI.Toggle(Step(ref area, 60),_control.ShowCurves, "Curve", EditorStyles.toolbarButton);
            if (GUI.Button(Step(ref area, 50), "展开", EditorStyles.toolbarButton))
            {
                Expanded(true);
            }

            if (GUI.Button(Step(ref area, 50), "折叠", EditorStyles.toolbarButton))
            {
                Expanded(false);
            }

            try
            {
                m_CameraAC?.Draw(ref area);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                throw;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ChooseResizeOption(object userData)
        {
            var selection = (int)userData;

            _control.ResizeOption = (ResizeOption)selection;
        }

        public void PlaymodeStateChanged(PlayModeStateChange state)
        {
            _control.InPreviewMode = false;
        }

        private static void LoadTextures()
        {
            var dir = "";
            var suffix = EditorGUIUtility.isProSkin ? "_Light" : "_Dark";
            var filetype_png = "";
            var missing = " is missing from Resources folder.";

            settingsImage = Resources.Load(SETTINGS_ICON + suffix) as Texture;
            if (settingsImage == null)Log.LogD(LogTag.Timeline, SETTINGS_ICON + suffix + missing);

            rescaleImage = Resources.Load(HORIZONTAL_RESCALE_ICON + suffix) as Texture;
            if (rescaleImage == null)Log.LogD(LogTag.Timeline, "{0}{1}{2}", HORIZONTAL_RESCALE_ICON, suffix, missing);

            zoomInImage = Resources.Load(ZOOMIN_ICON + suffix) as Texture;
            if (zoomInImage == null)Log.LogD(LogTag.Timeline, "{0}{1}{2}", ZOOMIN_ICON, suffix, missing);

            zoomOutImage = Resources.Load(dir + ZOOMOUT_ICON + suffix + filetype_png) as Texture;
            if (zoomOutImage == null)Log.LogD(LogTag.Timeline, "{0}{1}{2}", ZOOMOUT_ICON + suffix + missing);

            snapImage = Resources.Load(dir + MAGNET_ICON + suffix + filetype_png) as Texture;
            if (snapImage == null)Log.LogD(LogTag.Timeline, "{0}{1}{2}", MAGNET_ICON, suffix, missing);

            rollingEditImage = Resources.Load(dir + "Director_RollingIcon" + filetype_png) as Texture;
            if (rollingEditImage == null)Log.LogD(LogTag.Timeline, "Rolling edit icon missing from Resources folder.");

            rippleEditImage = Resources.Load(dir + "Director_RippleIcon" + filetype_png) as Texture;
            if (rippleEditImage == null)Log.LogD(LogTag.Timeline, "Ripple edit icon missing from Resources folder.");

            pickerImage = Resources.Load(dir + PICKER_ICON + suffix + filetype_png) as Texture;
            if (pickerImage == null)Log.LogD(LogTag.Timeline, "{0}{1}{2}", PICKER_ICON, suffix, missing);

            refreshImage = Resources.Load(dir + REFRESH_ICON + suffix + filetype_png) as Texture;
            if (refreshImage == null)Log.LogD(LogTag.Timeline, "{0}{1}{2}", REFRESH_ICON, suffix, missing);

            titleImage = Resources.Load(dir + TITLE_ICON + suffix + filetype_png) as Texture;

            cropImage = Resources.Load(dir + "Director_Resize_Crop" + suffix + filetype_png) as Texture;
            scaleImage = Resources.Load(dir + "Director_Resize_Scale" + suffix + filetype_png) as Texture;
            ms_AutoKeyFrameImage = Resources.Load<Texture>(AutoKeyFrameIcon);
        }

        private void AddTrackGroup(object userData)
        {
            var data = userData as TrackGroupContextData;
            if (data == null)
            {
                return;
            }
            var item = TimelineFactory.CreateTrackGroup(m_Timeline, data.Type, data.Label).gameObject;
            Undo.RegisterCreatedObjectUndo(item, $"Create {item.name}");
        }

        private TrackGroupContextData getContextDataFromType(Type type)
        {
            var label = string.Empty;
            var attrs = type.GetCustomAttributes(typeof(TimelineTrackGroupAttribute), true);
            for (var i = 0; i < attrs.Length; i++)
            {
                var attribute = attrs[i] as TimelineTrackGroupAttribute;
                if (attribute != null)
                {
                    label = attribute.Label;
                    break;
                }
            }

            var userData = new TrackGroupContextData { Type = type, Label = label };
            return userData;
        }

        [MenuItem(MENU_ITEM, false, 10)]
        public static void ShowWindow()
        {
            GetWindow(typeof(TimelineWindow));
        }

        private void CreateTimeline()
        {
            var timelineItemName = TimelineHelper.GetTimelineItemName("Timeline", typeof(Timeline));
            var timelineGo = new GameObject(timelineItemName);
            SelectTimeline(timelineGo.GetOrAddComponent<Timeline>());
        }

        private void SelectTimeline(Timeline timeline)
        {
            if (timeline == null)
                return;
            Snap(timeline);
            m_Timeline = timeline;
            m_Timeline.TimelineFinished -= OnStopTimeline;
            m_Timeline.TimelineFinished += OnStopTimeline;
            _control.InPreviewMode = false;
            _control.Rescale();
            Selection.activeObject = timeline.gameObject;
            m_CameraAC?.Reset();
        }

        private void OnStopTimeline(Timeline sender, TimelineEventArgs e)
        {
            Repaint();
        }

        internal void SaveTimeline()
        {
            if (m_Timeline == null)
                return;
            var prefabInstanceRoot = m_Timeline.gameObject;
            EditorUtil.RemoveMissingComponents(prefabInstanceRoot, true);
            if (!TimelineHelper.Check(prefabInstanceRoot))
            {
                return;
            }
            if (!PrefabUtility.HasPrefabInstanceAnyOverrides(prefabInstanceRoot,false))
                return;
            PrefabUtility.ApplyPrefabInstance(prefabInstanceRoot, InteractionMode.UserAction);
        }

        private void DestroyTimeline()
        {
            if (m_Timeline == null) return;
            DestroyImmediate(m_Timeline.gameObject);
        }

        private void Expanded(bool isExpanded)
        {
            _control?.Children.ForEach(element=>element.isExpanded = isExpanded);
        }

        private class TrackGroupContextData
        {
            public string Label;
            public Type Type;
        }
    }
}
