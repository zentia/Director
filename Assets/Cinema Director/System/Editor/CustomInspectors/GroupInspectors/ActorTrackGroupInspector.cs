using UnityEditor;
using UnityEngine;
using CinemaDirector;
using CinemaDirectorControl.Utility;

[CustomEditor(typeof(ActorTrackGroup), true)]
public class ActorTrackGroupInspector : Editor
{
    private SerializedProperty actor;
    private SerializedProperty id;
    private SerializedProperty optimizable;
    private SerializedProperty pos;
    private SerializedProperty rotation;
    private SerializedProperty m_ActorKind;
    private SerializedProperty scale;
    private SerializedProperty m_Name;
    public static ActorTrackGroup _target;

    private bool containerFoldout = true;
    private Texture inspectorIcon;

    GUIContent addTrackContent = new GUIContent("Add New Track", "Add a new track to this actor track group.");
    GUIContent tracksContent = new GUIContent("Actor Tracks", "The tracks associated with this Actor Group.");
    private GUIContent selectResConfig = new GUIContent("选择角色资源", "打开角色资源表");
    private GUIContent selectRoleConfig = new GUIContent("选择主角资源", "打开主角资源表");

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

    private void OnEnable()
    {
        actor = serializedObject.FindProperty("actor");
        id = serializedObject.FindProperty("id");
        optimizable = serializedObject.FindProperty("canOptimize");
        pos = serializedObject.FindProperty("pos");
        rotation = serializedObject.FindProperty("rotation");
        m_ActorKind = serializedObject.FindProperty("m_ActorKind");
        scale = serializedObject.FindProperty("scale");
        m_Name = serializedObject.FindProperty("m_ResName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _target = target as ActorTrackGroup;
        ActorTrackGroup actorGroup = serializedObject.targetObject as ActorTrackGroup;
        TimelineTrack[] tracks = actorGroup.GetTracks();

        Cutscene cutscene = actorGroup.Cutscene;

        bool isCutsceneActive = false;
        if (cutscene == null)
        {
            EditorGUILayout.HelpBox("Track Group must be a child of a Cutscene in the hierarchy", MessageType.Error);
        }
        else
        {
            isCutsceneActive = !(cutscene.State == Cutscene.CutsceneState.Inactive);
            if (isCutsceneActive)
            {
                EditorGUILayout.HelpBox("Cutscene is Active. Actors cannot be altered at the moment.", MessageType.Info);
            }
        }

        GUI.enabled = !isCutsceneActive;
        EditorGUILayout.PropertyField(actor);
        EditorGUILayout.PropertyField(id);
        GUI.enabled = true;
        
        EditorGUILayout.PropertyField(optimizable);
        if (GUILayout.Button("保存当前Transform数据", GUILayout.Width(150)))
        {
            _target.pos = _target.actor.position;
            _target.rotation = _target.actor.localEulerAngles;
        }
        EditorGUILayout.PropertyField(pos);
        EditorGUILayout.PropertyField(rotation);
        EditorGUILayout.PropertyField(m_ActorKind);
        EditorGUILayout.PropertyField(scale);
        EditorGUILayout.PropertyField(m_Name);
        if (tracks.Length > 0)
        {
            containerFoldout = EditorGUILayout.Foldout(containerFoldout, tracksContent);
            if (containerFoldout)
            {
                EditorGUI.indentLevel++;

                foreach (TimelineTrack track in tracks)
                {
                    EditorGUILayout.BeginHorizontal();
                    track.name = EditorGUILayout.TextField(track.name);
                    if (GUILayout.Button(inspectorIcon, GUILayout.Width(24)))
                    {
                        Selection.activeObject = track;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
        }
        if (GUILayout.Button(addTrackContent))
        {
            CutsceneControlHelper.ShowAddTrackContextMenu(actorGroup);
        }
        serializedObject.ApplyModifiedProperties();
    }

   
}
