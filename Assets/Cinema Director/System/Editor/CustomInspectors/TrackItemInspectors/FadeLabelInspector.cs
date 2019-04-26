using UnityEditor;
using UnityEngine;
using CinemaDirector;

[CustomEditor(typeof(FadeLabel))]
public class FadeLabelInspector : Editor
{
    private SerializedObject fadeLabel;
    private SerializedProperty firetime;
    private SerializedProperty duration;
	private SerializedProperty TextTarget;
    private SerializedProperty ActorPath;
    private SerializedProperty itemLength;
    private SerializedProperty m_Offfset;
	private FadeLabel m_Target;
    public void OnEnable()
    {
		fadeLabel = new SerializedObject(target);
		m_Target = target as FadeLabel;
		firetime = fadeLabel.FindProperty("firetime");
		duration = fadeLabel.FindProperty("duration");
		TextTarget = fadeLabel.FindProperty("target");
		ActorPath = fadeLabel.FindProperty("m_TargetPath");
		itemLength = fadeLabel.FindProperty("itemLength");
		m_Offfset = fadeLabel.FindProperty("m_Offset");
    }
    public override void OnInspectorGUI()
    {
		fadeLabel.Update();
        EditorGUILayout.PropertyField(firetime);
        EditorGUILayout.PropertyField(duration);
		EditorGUILayout.PropertyField(m_Offfset);
		EditorGUILayout.PropertyField (TextTarget);
		EditorGUILayout.PropertyField (ActorPath);
		if (m_Target.target != null && !Application.isPlaying && DirectorWindow.Instance.cutscene.State != Cutscene.CutsceneState.PreviewPlaying) {
			var position = m_Target.target.rectTransform.position;
			m_Target.target.rectTransform.position = new Vector3 (position.x + m_Target.m_Offset.x, position.y + m_Target.m_Offset.y);
		}
		fadeLabel.ApplyModifiedProperties();
    }
}
