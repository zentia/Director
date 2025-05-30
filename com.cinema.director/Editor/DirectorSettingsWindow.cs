using UnityEditor;
using UnityEngine;

public class DirectorSettingsWindow : EditorWindow
{
    private const string TITLE = "Settings";
    private bool enableBetaFeatures;
    private bool enableHQWaveformTextures;

    private TangentMode defaultTangentMode;

    private enum TangentMode
    {
        Flat = 0,
        Auto = 10,
        Linear = 21,
        Constant = 31
    }

    /// <summary>
    /// Sets the window title and minimum pane size
    /// </summary>
    public void Awake()
    {
#if UNITY_5 && !UNITY_5_0  || UNITY_2017_1_OR_NEWER
        base.titleContent = new GUIContent(TITLE);
#else
        base.title = TITLE;
#endif
        this.minSize = new Vector2(250f, 150f);

        if (EditorPrefs.HasKey("DirectorControl.DefaultTangentMode"))
        {
            defaultTangentMode = (TangentMode) EditorPrefs.GetInt("DirectorControl.DefaultTangentMode");
        }

        if (EditorPrefs.HasKey("DirectorControl.UseHQWaveformTextures"))
        {
            enableHQWaveformTextures = EditorPrefs.GetBool("DirectorControl.UseHQWaveformTextures");
        }
        else
        {
            enableHQWaveformTextures = true;
            EditorPrefs.SetBool("DirectorControl.UseHQWaveformTextures", enableHQWaveformTextures);
        }

        if (EditorPrefs.HasKey("DirectorControl.EnableBetaFeatures"))
        {
            enableBetaFeatures = EditorPrefs.GetBool("DirectorControl.EnableBetaFeatures");
        }
    }

    /// <summary>
    /// Draws the Settings GUI
    /// </summary>
    protected void OnGUI()
    {
        // Defaults

        TangentMode tempTangentMode = (TangentMode)EditorGUILayout.EnumPopup(new GUIContent("Tangent Mode"), defaultTangentMode);
        if(tempTangentMode != defaultTangentMode)
        {
            defaultTangentMode = tempTangentMode;
            EditorPrefs.SetInt("DirectorControl.DefaultTangentMode", (int)defaultTangentMode);
        }

        bool useHQWaveformTextures = EditorGUILayout.Toggle(new GUIContent("Use HQ Waveform Textures", 
                                                            "Replaces the old blurry and stretched waveform with custom made waveform textures, but uses more resources. Requires reloading the Director Window."),
                                                            enableHQWaveformTextures);
        if (useHQWaveformTextures != enableHQWaveformTextures)
        {
            enableHQWaveformTextures = useHQWaveformTextures;
            EditorPrefs.SetBool("DirectorControl.UseHQWaveformTextures", enableHQWaveformTextures);
        }

        bool tempBetaFeatures = EditorGUILayout.Toggle(new GUIContent("Enable Beta Features"), enableBetaFeatures);
        if(tempBetaFeatures!=enableBetaFeatures)
        {
            enableBetaFeatures = tempBetaFeatures;
            EditorPrefs.SetBool("DirectorControl.EnableBetaFeatures", enableBetaFeatures);
        }

        GUILayout.FlexibleSpace();
        if(GUILayout.Button("Apply"))
        {
            EditorWindow.GetWindow<DirectorWindow>().LoadSettings();
        }
    }
}