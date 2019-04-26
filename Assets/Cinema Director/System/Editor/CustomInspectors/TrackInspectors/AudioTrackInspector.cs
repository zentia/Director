using UnityEditor;
using UnityEngine;
using CinemaDirector;

[CustomEditor(typeof(AudioTrack))]
public class AudioTrackInspector : Editor
{
    private SerializedObject audioTrack;

    private GUIContent addAudio = new GUIContent("添加音效", "添加一个音效资源");
    
    public void OnEnable()
    {
        audioTrack = new SerializedObject(this.target);
    }

    public override void OnInspectorGUI()
    {
        audioTrack.Update();

        foreach (CinemaAudio audio in (target as AudioTrack).AudioClips)
        {
            EditorGUILayout.ObjectField(audio.name, audio, typeof(CinemaAudio), true);
        }

        audioTrack.ApplyModifiedProperties();
    }
}
