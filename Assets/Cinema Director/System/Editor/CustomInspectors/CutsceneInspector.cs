using UnityEditor;
using UnityEngine;
using CinemaDirector;
using System;
using CinemaDirectorControl.Utility;

/// <summary>
/// A custom inspector for a cutscene.
/// </summary>
[CustomEditor(typeof(Cutscene))]
public class CutsceneInspector : Editor
{
    private SerializedProperty duration;
    private SerializedProperty isLooping;
    private SerializedProperty isSkippable;
    private SerializedProperty canOptimize;
    private SerializedProperty ID;

    //private SerializedProperty runningTime;
    //private SerializedProperty playbackSpeed;
    private bool containerFoldout = true;

    private Texture inspectorIcon;

    GUIContent durationContent = new GUIContent("Duration", "剧情播放时间，单位秒");
    GUIContent loopingContent = new GUIContent("Loop", "循环播放");
    GUIContent skippableContent = new GUIContent("Skippable", "剧情是否可以被跳过");
    GUIContent optimizeContent = new GUIContent("Optimize", "Enable when Cutscene does not have Track Groups added/removed during playtime.");
    GUIContent idContent = new GUIContent("ID", "剧情表的ID");
    GUIContent groupsContent = new GUIContent("Track Groups", "Organizational units of a cutscene.");
    GUIContent addGroupContent = new GUIContent("Add Group", "Add a new container to the cutscene.");

    /// <summary>
    /// Load texture assets on awake.
    /// </summary>
    private void Awake()
    {
        if (inspectorIcon == null)
        {
            inspectorIcon = Resources.Load<Texture>("Director_InspectorIcon");
        }
        if (inspectorIcon == null)
        {
            Debug.Log("Inspector icon missing from Resources folder.");
        }
    }

    /// <summary>
    /// On inspector enable, load the serialized properties
    /// </summary>
    private void OnEnable()
    {
        duration = serializedObject.FindProperty("duration");
        isLooping = serializedObject.FindProperty("isLooping");
        isSkippable = serializedObject.FindProperty("isSkippable");
        canOptimize = serializedObject.FindProperty("canOptimize");
    }

    /// <summary>
    /// Draw the inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.PrefixLabel(new GUIContent("Director"));
        if (GUILayout.Button("Open Director"))
        {
            DirectorWindow window = EditorWindow.GetWindow(typeof(DirectorWindow)) as DirectorWindow;
            window.FocusCutscene(serializedObject.targetObject as Cutscene);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(duration, durationContent);
        EditorGUILayout.PropertyField(isLooping, loopingContent);
        EditorGUILayout.PropertyField(isSkippable, skippableContent);
        EditorGUILayout.PropertyField(canOptimize, optimizeContent);

        containerFoldout = EditorGUILayout.Foldout(containerFoldout, groupsContent);

        if (containerFoldout)
        {
            EditorGUI.indentLevel++;
            Cutscene c = serializedObject.targetObject as Cutscene;

            foreach (TrackGroup container in c.TrackGroups)
            {
                EditorGUILayout.BeginHorizontal();
                
                container.name = EditorGUILayout.TextField(container.name);
                //GUILayout.Button("add", GUILayout.Width(16));
                if (GUILayout.Button(inspectorIcon, GUILayout.Width(24)))
                {
                    Selection.activeObject = container;
                }
                //GUILayout.Button("u", GUILayout.Width(16));
                //GUILayout.Button("d", GUILayout.Width(16));
                EditorGUILayout.EndHorizontal();

                //EditorGUILayout.ObjectField(container.name, container, typeof(TrackGroup), true);
            }
            EditorGUI.indentLevel--;
            if (GUILayout.Button(addGroupContent))
            {
                CutsceneControlHelper.ShowAddTrackGroupContextMenu(c);
            }
        }
       
        serializedObject.ApplyModifiedProperties();
    }

}
