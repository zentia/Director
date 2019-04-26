using CinemaDirector;
using System;
using System.Timers;
using UnityEditor;
using UnityEngine;

public class DirectorWindow : EditorWindow
{
    public static DirectorWindow Instance;
    public static DirectorControl directorControl;
	public Cutscene cutscene;
    private int cutsceneInstanceID;
    private CutsceneWrapper cutsceneWrapper;
    private float previousTime;
    private bool isSnappingEnabled;
    private Cutscene[] cachedCutscenes;
    private static Timer timer;

    private bool betaFeaturesEnabled = false;

    private const string PREVIEW_MODE = "预览模式";
    private const string CREATE = "创建";
    private GUIContent new_cutscene = new GUIContent("新剧情");

    const string TITLE = "导演";
    const string MENU_ITEM = "Scene/剧情编辑器/导演 %#d";

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

    private const float FRAME_LIMITER = 1 / 60f;
    private double accumulatedTime = 0f;
    int popupSelection = 0;
    [SerializeField]
    private AnimationKeyTime m_Time;
    
    public static AnimationKeyTime ATime(float time, float frameRate)
    {
        AnimationKeyTime key = new AnimationKeyTime();
        key.m_Time = Mathf.Max(time, 0f);
        key.m_FrameRate = frameRate;
        key.m_Frame = Mathf.RoundToInt(key.m_Time * frameRate);
        return key;
    }
    
    public void Awake()
    {
        titleContent = new GUIContent(TITLE, titleImage);
        minSize = new Vector2(400f, 250f);
        loadTextures();
        previousTime = Time.realtimeSinceStartup;
        accumulatedTime = 0;
    }
    public AnimationKeyTime time
    {
        get
        {
            return m_Time;
        }
    }
    
    public void Update()
    {
        float deltaTime = Time.realtimeSinceStartup - previousTime;
        m_Time.m_FrameRate = 1 / deltaTime;
        previousTime = Time.realtimeSinceStartup;
        if (deltaTime > 0)
        {
            accumulatedTime += deltaTime;
        }
        if (accumulatedTime >= FRAME_LIMITER)
        {
            Repaint();
            accumulatedTime -= FRAME_LIMITER;
        }
        if (cutscene != null)
        {
            if (!Application.isPlaying && cutscene.State == Cutscene.CutsceneState.PreviewPlaying)
            {
                float newTime = m_Time.time + deltaTime;
                m_Time = AnimationKeyTime.Time(newTime, directorControl.frameRate);
                Cutscene.frameRate = m_Time.frameRate;
                Cutscene.frame = m_Time.frame;
                cutscene.UpdateCutscene(deltaTime);
            }
        }
    }

    protected void OnEnable()
    {
        Instance = this;
        EditorApplication.playmodeStateChanged = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.playmodeStateChanged, new EditorApplication.CallbackFunction(this.PlaymodeStateChanged));

        GUISkin skin = CreateInstance<GUISkin>();
        skin = (EditorGUIUtility.isProSkin) ? Resources.Load<GUISkin>(PRO_SKIN) : Resources.Load<GUISkin>(FREE_SKIN);
        loadTextures();

        titleContent = new GUIContent(TITLE, titleImage);
        directorControl = new DirectorControl();
        directorControl.OnLoad(skin);

        directorControl.PlayCutscene += directorControl_PlayCutscene;
        directorControl.PauseCutscene += directorControl_PauseCutscene;
        directorControl.StopCutscene += directorControl_StopCutscene;
        directorControl.ScrubCutscene += directorControl_ScrubCutscene;
        directorControl.SetCutsceneTime += directorControl_SetCutsceneTime;
        directorControl.EnterPreviewMode += directorControl_EnterPreviewMode;
        directorControl.ExitPreviewMode += directorControl_ExitPreviewMode;
        directorControl.DragPerformed += directorControl_DragPerformed;
        isSnappingEnabled = directorControl.IsSnappingEnabled;

        directorControl.RepaintRequest += directorControl_RepaintRequest;


        previousTime = Time.realtimeSinceStartup;
        accumulatedTime = 0;

        int instanceId = -1;
        if (EditorPrefs.HasKey("DirectorControl.CutsceneID"))
        {
            instanceId = EditorPrefs.GetInt("DirectorControl.CutsceneID");
        }

        if (instanceId >= 0)
        {
            foreach (Cutscene c in FindObjectsOfType<Cutscene>())
            {
                if (c.GetInstanceID() == instanceId)
                {
                    cutscene = c;
                }
            }
        }

        LoadSettings();
        Undo.undoRedoPerformed += UndoRedoPerformed;
    }

    void UndoRedoPerformed()
    {

    }

    public void LoadSettings()
    {
        if (EditorPrefs.HasKey("DirectorControl.EnableBetaFeatures"))
        {
            betaFeaturesEnabled = EditorPrefs.GetBool("DirectorControl.EnableBetaFeatures");
        }

        if (EditorPrefs.HasKey("DirectorControl.DefaultTangentMode"))
        {
            directorControl.DefaultTangentMode = EditorPrefs.GetInt("DirectorControl.DefaultTangentMode");
        }
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
            if(e.references != null)
            {
                if(e.references.Length == 1)
                {
                    GameObject gameObject = e.references[0] as GameObject;
                    if(gameObject != null)
                    {
                        ActorTrackGroup atg = CutsceneItemFactory.CreateTrackGroup(c, typeof(ActorTrackGroup), string.Format("{0} Track Group", gameObject.name)) as ActorTrackGroup;
                        atg.Actor = gameObject.GetComponent<Transform>();

                        Undo.RegisterCreatedObjectUndo(atg.gameObject, string.Format("Created {0}", atg.gameObject.name));
                    }
                }
            }
        }
    }

    

    void directorControl_ExitPreviewMode(object sender, CinemaDirectorArgs e)
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
        Cutscene c = e.cutscene as Cutscene;
        if (c != null)
        {
            c.ScrubToTime(e.timeArg);
        }
    }

    void directorControl_StopCutscene(object sender, CinemaDirectorArgs e)
    {
        Cutscene c = e.cutscene as Cutscene;
        if (c != null)
        {
            c.Stop();
        }
    }

    void directorControl_PauseCutscene(object sender, CinemaDirectorArgs e)
    {
        Cutscene c = e.cutscene as Cutscene;
        if (c != null)
        {
            c.Pause();
        }
    }

    void directorControl_PlayCutscene(object sender, CinemaDirectorArgs e)
    {
        Cutscene c = e.cutscene as Cutscene;
        if (c != null)
        {
            if (Application.isPlaying)
            {
                c.Play();
            }
            else
            {
                c.PreviewPlay();
                previousTime = Time.realtimeSinceStartup;
                m_Time.m_Time = 0;
                m_Time.m_Frame = 0;
            }
        }
    }

    #endregion

    /// <summary>
    /// Draws the GUI for the Timeline Window.
    /// </summary>
    protected void OnGUI()
    {
        Rect controlArea = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);

        updateToolbar();

        cutsceneWrapper = DirectorHelper.UpdateWrapper(cutscene, cutsceneWrapper);
        switch (Event.current.keyCode)
        {
            case KeyCode.RightArrow:
                cutscene.RunningTime += 0.01f;
                break;
            case KeyCode.LeftArrow:
                cutscene.RunningTime -= 0.01f;
                break;
        }
        directorControl.OnGUI(controlArea, cutsceneWrapper, position);
        DirectorHelper.ReflectChanges(cutscene, cutsceneWrapper);
    }

    private void updateToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // If there are no cutscenes, then only give option to create a new cutscene.
        if (GUILayout.Button(CREATE, EditorStyles.toolbarDropDown, GUILayout.Width(60)))
        {
            GenericMenu createMenu = new GenericMenu();
            createMenu.AddItem(new_cutscene, false, openCutsceneCreatorWindow);

            if (cutscene != null)
            {
                createMenu.AddSeparator(string.Empty);

                foreach (Type type in DirectorHelper.GetAllSubTypes(typeof(TrackGroup)))
                {
                    TrackGroupContextData userData = getContextDataFromType(type);
                    string text = string.Format(userData.Label);
                    createMenu.AddItem(new GUIContent(text), false, AddTrackGroup, userData);
                }
            }

            createMenu.DropDown(new Rect(5, TOOLBAR_HEIGHT, 0, 0));
        }

        // Cutscene selector
        cachedCutscenes = FindObjectsOfType<Cutscene>();
        if (cachedCutscenes != null && cachedCutscenes.Length > 0)
        {
            // Get cutscene names
            GUIContent[] cutsceneNames = new GUIContent[cachedCutscenes.Length];
            for (int i = 0; i < cachedCutscenes.Length; i++)
            {
                cutsceneNames[i] = new GUIContent(cachedCutscenes[i].name);
            }
            
            // Sort alphabetically
            Array.Sort(cutsceneNames, delegate(GUIContent content1, GUIContent content2)
            {
                return string.Compare(content1.text, content2.text);
            });

            int count = 1;
            // Resolve duplicate names
            for (int i = 0; i < cutsceneNames.Length - 1; i++)
            {
                int next = i + 1;
                while (next < cutsceneNames.Length && string.Compare(cutsceneNames[i].text, cutsceneNames[next].text) == 0)
                {
                    cutsceneNames[next].text = string.Format("{0} (duplicate {1})", cutsceneNames[next].text, count++);
                    next++;
                }
                count = 1;
            }

            Array.Sort(cachedCutscenes, delegate(Cutscene c1, Cutscene c2)
            {
                return string.Compare(c1.name, c2.name);
            });

            // Find the currently selected cutscene.
            for (int i = 0; i < cachedCutscenes.Length; i++)
            {
                if (cutscene != null && cutscene.GetInstanceID() == cachedCutscenes[i].GetInstanceID())
                {
                    popupSelection = i;
                }
            }

            // Show the popup
            int tempPopup = EditorGUILayout.Popup(popupSelection, cutsceneNames, EditorStyles.toolbarPopup);
            if (cutscene == null || tempPopup != popupSelection || cutsceneInstanceID != cachedCutscenes[Math.Min(tempPopup, cachedCutscenes.Length - 1)].GetInstanceID())
            {
                popupSelection = tempPopup;
                popupSelection = Math.Min(popupSelection, cachedCutscenes.Length - 1);
                FocusCutscene(cachedCutscenes[popupSelection]);
            }
        }
        if (cutscene != null)
        {
            if (GUILayout.Button(pickerImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                Selection.activeObject = cutscene;
            }
            if (GUILayout.Button(refreshImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                cutscene.Refresh();
            }

            if (Event.current.control && Event.current.keyCode == KeyCode.Space)
            {
                cutscene.Refresh();
                Event.current.Use();
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label(Cutscene.frame.ToString());
        if (betaFeaturesEnabled)
        {
            Texture resizeTexture = cropImage;
            if (directorControl.ResizeOption == DirectorEditor.ResizeOption.Scale)
            {
                resizeTexture = scaleImage;
            }
            Rect resizeRect = GUILayoutUtility.GetRect(new GUIContent(resizeTexture), EditorStyles.toolbarDropDown, GUILayout.Width(32));
            if (GUI.Button(resizeRect, new GUIContent(resizeTexture, "Resize Option"), EditorStyles.toolbarDropDown))
            {
                GenericMenu resizeMenu = new GenericMenu();

                string[] names = Enum.GetNames(typeof(DirectorEditor.ResizeOption));

                for (int i = 0; i < names.Length; i++)
                {
                    resizeMenu.AddItem(new GUIContent(names[i]), directorControl.ResizeOption == (DirectorEditor.ResizeOption)i, chooseResizeOption, i);
                }

                resizeMenu.DropDown(new Rect(resizeRect.x, TOOLBAR_HEIGHT, 0, 0));
            }
        }

        bool tempSnapping = GUILayout.Toggle(isSnappingEnabled, snapImage, EditorStyles.toolbarButton, GUILayout.Width(24));
        if (tempSnapping != isSnappingEnabled)
        {
            isSnappingEnabled = tempSnapping;
            directorControl.IsSnappingEnabled = isSnappingEnabled;
        }

        GUILayout.Space(10f);

        if (GUILayout.Button(rescaleImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
        {
            directorControl.Rescale();
        }
        if (GUILayout.Button(new GUIContent(zoomInImage, "Zoom In"), EditorStyles.toolbarButton, GUILayout.Width(24)))
        {
            directorControl.ZoomIn();
        }
        if (GUILayout.Button(zoomOutImage, EditorStyles.toolbarButton, GUILayout.Width(24)))
        {
            directorControl.ZoomOut();
        }
        GUILayout.Space(10f);

        Color temp = GUI.color;
        GUI.color = directorControl.InPreviewMode ? Color.red : temp;
        
        directorControl.InPreviewMode = GUILayout.Toggle(directorControl.InPreviewMode, string.Format("{0} {1}", PREVIEW_MODE, Cutscene.frameRate), EditorStyles.toolbarButton, GUILayout.Width(150));
        GUI.color = temp;
        GUILayout.Space(10);

        if (GUILayout.Button(settingsImage, EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            GetWindow<DirectorSettingsWindow>();
        }
        var helpWindowType = Type.GetType("CinemaSuite.CinemaSuiteWelcome");
        if (helpWindowType != null)
        {
            if (GUILayout.Button(new GUIContent("?", "Help"), EditorStyles.toolbarButton))
            {
                GetWindow(helpWindowType);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void chooseResizeOption(object userData)
    {
        int selection = (int) userData;

        directorControl.ResizeOption = (DirectorEditor.ResizeOption)selection;
    }

    public void FocusCutscene(Cutscene cutscene)
    {
        if (this.cutscene != null)
        {
            this.cutscene.ExitPreviewMode();
        }
        directorControl.InPreviewMode = false;

        EditorPrefs.SetInt("DirectorControl.CutsceneID", cutscene.GetInstanceID());
        cutsceneInstanceID = cutscene.GetInstanceID();
        this.cutscene = cutscene;
    }

    public void PlaymodeStateChanged()
    {
        directorControl.InPreviewMode = false;
    }

    public void OnDisable()
    {
        Instance = null;
        directorControl.OnDisable();
        if (Application.isEditor && cutscene != null)
        {
            cutscene.ExitPreviewMode();
        }
        if (cutscene != null)
        {
            EditorPrefs.SetInt("DirectorControl.CutsceneID", cutscene.GetInstanceID());
        }
        Undo.undoRedoPerformed -= UndoRedoPerformed;
    }

    private void OnDestroy()
    {
        directorControl.OnDestroy();
    }

    private void loadTextures()
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

        refreshImage = Resources.Load<Texture>(REFRESH_ICON+suffix);
        if (refreshImage == null)
        {
            Debug.Log(REFRESH_ICON+suffix+missing);
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
            GameObject item = CutsceneItemFactory.CreateTrackGroup(cutscene, data.Type, data.Label).gameObject;
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
        GetWindow(typeof(DirectorWindow));

        bool showWelcome = true;
        if (EditorPrefs.HasKey("CinemaSuite.WelcomeWindow.ShowOnStartup"))
        {
            showWelcome = EditorPrefs.GetBool("CinemaSuite.WelcomeWindow.ShowOnStartup");
        }

        if (showWelcome)
        {
            var helpWindowType = Type.GetType("CinemaSuite.CinemaSuiteWelcome");
            if (helpWindowType != null)
            {
                GetWindow(helpWindowType);
            }
        }
    }

    internal void openCutsceneCreatorWindow()
    {
        GetWindow<CutsceneCreatorWindow>();
    }

    public bool BetaFeaturesEnabled
    {
        get 
        { 
            return betaFeaturesEnabled; 
        }
        set 
        { 
            betaFeaturesEnabled = value; 
        }
    }

    private class TrackGroupContextData
    {
        public Type Type;
        public string Label;
    }
}