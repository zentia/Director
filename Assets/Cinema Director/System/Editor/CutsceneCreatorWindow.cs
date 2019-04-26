using CinemaDirector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CutsceneCreatorWindow : EditorWindow
{
    #region UI Fields
    private string txtCutsceneName = "Cutscene";
    private float txtDuration = 30;
    private DirectorHelper.TimeEnum timeEnum = DirectorHelper.TimeEnum.Seconds;
    private bool isLooping = false;
    private bool isSkippable = true;
    private Vector2 scrollPosition = new Vector2();
    private StartMethod StartMethod = StartMethod.None;

    private int directorTrackGroupsSelection = 1;
    private int actorTrackGroupsSelection = 0;
    private int multiActorTrackGroupsSelection = 0;
    private int characterTrackGroupsSelection = 0;
    private int entityTrackGroupsSelection = 0;

    private int shotTrackSelection = 1;
    private int audioTrackSelection = 2;
    private int globalItemTrackSelection;

    private List<GUIContent> intValues1 = new List<GUIContent>();
    private List<GUIContent> intValues4 = new List<GUIContent>();
    private List<GUIContent> intValues10 = new List<GUIContent>();

    private Transform[] actors = new Transform[0];
    private Transform[] characters = new Transform[0];
    private uint[] entitys = new uint[0];
    #endregion
    
    #region Language

    const string TITLE = "创建者";
    const string NAME_DUPLICATE = "剧情的名字已经存在";
    
    GUIContent NameContentCutscene = new GUIContent("名字", "创建出来的剧情名字");
    GUIContent IdContentCutscene = new GUIContent("ID", "剧情表的ID");
    GUIContent DurationContentCutscene = new GUIContent("持续时间", "剧情的持续时间(秒/毫秒)");
    GUIContent LoopingContentCutscene = new GUIContent("循环", "剧情循环播放");
    GUIContent SkippableContentCutscene = new GUIContent("可跳过", "剧情可以被跳过");

    GUIContent AddDirectorGroupContent = new GUIContent("DirectorGroup");
    GUIContent AddShotTracksContent = new GUIContent("ShotTrack");
    GUIContent AddAudioTracksContent = new GUIContent("AudioTrack");
    GUIContent AddGlobalTracksContent = new GUIContent("全局轨道");

    #endregion


    /// <summary>
    /// Sets the window title and minimum pane size
    /// </summary>
    public void Awake()
    {
        base.titleContent = new GUIContent(TITLE);
        this.minSize = new Vector2(250f, 150f);
        intValues1.Add(new GUIContent("0"));
        intValues1.Add(new GUIContent("1"));

        for (int i = 0; i <= 4; i++)
        {
            intValues4.Add(new GUIContent(i.ToString()));
        }

        for (int i = 0; i <= 10; i++)
        {
            intValues10.Add(new GUIContent(i.ToString()));
        }
    }

    [MenuItem("Window/Director/创建剧情", false, 10)]
    static void Init()
    {
        GetWindow<CutsceneCreatorWindow>();
    }

    void Add<T>(ref T[] data, ref int TrackGroupsSelection, string name)
    {
        EditorGUILayout.Space();
        int TGCount = EditorGUILayout.Popup(new GUIContent(name), TrackGroupsSelection, intValues10.ToArray());
        if (TGCount != TrackGroupsSelection)
        {
            TrackGroupsSelection = TGCount;
            T[] temp = new T[data.Length];
            Array.Copy(data, temp, data.Length);
            data = new T[TGCount];
            int amount = Math.Min(TGCount, temp.Length);
            Array.Copy(temp, data, amount);
        }
        EditorGUI.indentLevel++;
    }

    /// <summary>
    /// Draws the Director GUI
    /// </summary>
    protected void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        {
            txtCutsceneName = EditorGUILayout.TextField(NameContentCutscene, txtCutsceneName);
            EditorGUILayout.BeginHorizontal();
            txtDuration = EditorGUILayout.FloatField(DurationContentCutscene, txtDuration);
            timeEnum = (DirectorHelper.TimeEnum)EditorGUILayout.EnumPopup(timeEnum);
            EditorGUILayout.EndHorizontal();

            isLooping = EditorGUILayout.Toggle(LoopingContentCutscene, isLooping);
            isSkippable = EditorGUILayout.Toggle(SkippableContentCutscene, isSkippable);
            StartMethod = (StartMethod)EditorGUILayout.EnumPopup(new GUIContent("开始方法"), StartMethod);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("轨道组", EditorStyles.boldLabel);

            // Director Group
            directorTrackGroupsSelection = EditorGUILayout.Popup(AddDirectorGroupContent, directorTrackGroupsSelection, intValues1.ToArray());
            
            if(directorTrackGroupsSelection > 0)
            {
                EditorGUI.indentLevel++;

                // Shot Tracks
                shotTrackSelection = EditorGUILayout.Popup(AddShotTracksContent, shotTrackSelection, intValues1.ToArray());

                // Audio Tracks
                audioTrackSelection = EditorGUILayout.Popup(AddAudioTracksContent, audioTrackSelection, intValues4.ToArray());

                // Global Item Tracks
                globalItemTrackSelection = EditorGUILayout.Popup(AddGlobalTracksContent, globalItemTrackSelection, intValues10.ToArray());

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Actor Track Groups
            int actorCount = EditorGUILayout.Popup(new GUIContent("主角轨道群组"), actorTrackGroupsSelection, intValues10.ToArray());

            if (actorCount != actorTrackGroupsSelection)
            {
                actorTrackGroupsSelection = actorCount;

                Transform[] tempActors = new Transform[actors.Length];
                Array.Copy(actors, tempActors, actors.Length);
                
                actors = new Transform[actorCount];
                int amount = Math.Min(actorCount, tempActors.Length);
                Array.Copy(tempActors, actors, amount);
            }

            EditorGUI.indentLevel++;
            for(int i = 1; i <= actorTrackGroupsSelection; i++)
            {
                actors[i - 1] = EditorGUILayout.ObjectField(new GUIContent(string.Format("主角 {0}", i)), actors[i - 1], typeof(Transform), true) as Transform;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            // Multi Actor Track Groups
            multiActorTrackGroupsSelection = EditorGUILayout.Popup(new GUIContent("多主角轨道群组"), multiActorTrackGroupsSelection, intValues10.ToArray());
            EditorGUI.indentLevel++;
            EditorGUI.indentLevel--;
            Add(ref characters, ref characterTrackGroupsSelection, "ActorTrackGroups");
            for (int i = 1; i <= characterTrackGroupsSelection; i++)
            {
                characters[i - 1] = EditorGUILayout.ObjectField(new GUIContent(string.Format("角色 {0}", i)), characters[i - 1], typeof(Transform), true) as Transform;
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("I'm Feeling Lucky"))
            {
                List<Transform> interestingActors = UnitySceneEvaluator.GetHighestRankedGameObjects(10);

                actorTrackGroupsSelection = interestingActors.Count;
                actors = interestingActors.ToArray();
            }

            if (GUILayout.Button("创建剧情"))
            {
                string cutsceneName = DirectorHelper.getCutsceneItemName(txtCutsceneName, typeof(Cutscene));

                GameObject cutsceneGO = new GameObject(cutsceneName);
                Cutscene cutscene = cutsceneGO.AddComponent<Cutscene>();
                for (int i = 0; i < directorTrackGroupsSelection; i++)
                {
                    DirectorGroup dg = CutsceneItemFactory.CreateDirectorGroup(cutscene);
                    dg.Ordinal = 0;
                    for (int j = 0; j < shotTrackSelection; j++)
                    {
                        CutsceneItemFactory.CreateShotTrack(dg);
                    }
                    for (int j = 0; j < audioTrackSelection; j++)
                    {
                        CutsceneItemFactory.CreateAudioTrack(dg);
                    }
                    for (int j = 0; j < globalItemTrackSelection; j++)
                    {
                        CutsceneItemFactory.CreateGlobalItemTrack(dg);
                    }
                }

                for (int i = 0; i < actorTrackGroupsSelection; i++)
                {
                    CutsceneItemFactory.CreateActorTrackGroup(cutscene, actors[i]);
                }

                for (int i = 0; i < multiActorTrackGroupsSelection; i++)
                {
                    CutsceneItemFactory.CreateMultiActorTrackGroup(cutscene);
                }

                for (int i = 0; i < characterTrackGroupsSelection; i++)
                {
                    CutsceneItemFactory.CreateCharacterTrackGroup(cutscene, characters[i]);
                }
                float duration = txtDuration;
                if (timeEnum == DirectorHelper.TimeEnum.Minutes) duration *= 60;
                cutscene.Duration = duration;

                int undoIndex = Undo.GetCurrentGroup();

                if(StartMethod != StartMethod.None)
                {
                    GameObject cutsceneTriggerGO = new GameObject("Cutscene Trigger");
                    CutsceneTrigger cutsceneTrigger = cutsceneTriggerGO.AddComponent<CutsceneTrigger>();
                    if (StartMethod == StartMethod.OnTrigger)
                    {
                        cutsceneTriggerGO.AddComponent<BoxCollider>();
                    }
                    cutsceneTrigger.StartMethod = StartMethod;
                    cutsceneTrigger.Cutscene = cutscene;
                    Undo.RegisterCreatedObjectUndo(cutsceneTriggerGO, string.Format("Created {0}", txtCutsceneName));
                }
                
                Undo.RegisterCreatedObjectUndo(cutsceneGO, string.Format("Created {0}",txtCutsceneName));
                Undo.CollapseUndoOperations(undoIndex);
                Selection.activeTransform = cutsceneGO.transform;
            }
        }
        EditorGUILayout.EndHorizontal();
    }
}