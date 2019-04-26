using UnityEditor;
using UnityEngine;
using CinemaDirector;

[CustomEditor(typeof(CinemaAudio))]
public class CinemaAudioInspector : Editor
{
    private SerializedObject cinemaAudio;
    private SerializedProperty firetime;
    private SerializedProperty duration;
    private SerializedProperty inTime;
    private SerializedProperty outTime;
    private SerializedProperty itemLength;
    private SerializedProperty m_Path;
    private CinemaAudio m_Target;
    public void OnEnable()
    {
        cinemaAudio = new SerializedObject(target);
        m_Target = target as CinemaAudio;
        firetime = cinemaAudio.FindProperty("firetime");
        duration = cinemaAudio.FindProperty("duration");
        inTime = cinemaAudio.FindProperty("inTime");
        outTime = cinemaAudio.FindProperty("outTime");
        itemLength = cinemaAudio.FindProperty("itemLength");
        m_Path = cinemaAudio.FindProperty("m_Path");
    }
    public void Callback(string path)
    {
        m_Target.m_Path = path.Replace('\\', '/');
    }
    public override void OnInspectorGUI()
    {
        cinemaAudio.Update();
        EditorGUILayout.PropertyField(firetime);
        EditorGUILayout.PropertyField(duration);
        EditorGUILayout.PropertyField(inTime);
        EditorGUILayout.PropertyField(outTime);
        EditorGUILayout.PropertyField(m_Path);
        cinemaAudio.ApplyModifiedProperties();
        if (GUILayout.Button("打开音效资源"))
        {
            EffectSelector.Show(Callback, "Sounds", false);
        }
    }
}
