using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class CinemaCurveSelection : ScriptableObject, ISerializationCallbackReceiver
{
	public string Type = string.Empty;
	public string Property = string.Empty;
	public int CurveId = -1;
	public int KeyId = -1;
    private HashSet<int> m_SelectedKeyHashs;
    [SerializeField] private List<int> m_SelectedKeyHashesSerialized;
	internal void Reset()
	{
		Type = string.Empty;
		Property = string.Empty;
		CurveId = -1;
		KeyId = -1;
	}
    void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
    }
    public HashSet<int> selectedKeyHashs
    {
        get { return m_SelectedKeyHashs ?? (m_SelectedKeyHashs = new HashSet<int>()); }
        set { m_SelectedKeyHashs = value; }
    }

    public void SaveSelection(string undoLabel)
    {
        Undo.RegisterCompleteObjectUndo(this, undoLabel);
    }

    public void OnBeforeSerialize()
    {
        if (m_SelectedKeyHashs != null)
        {
            m_SelectedKeyHashesSerialized = m_SelectedKeyHashs.ToList();
        }
    }

    public void OnAfterDeserialize()
    {
        m_SelectedKeyHashs = new HashSet<int>(m_SelectedKeyHashesSerialized);
    }
}
