using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    [System.Serializable]
    internal class TimelineWindowKeySelection : ScriptableObject, ISerializationCallbackReceiver
    {
        private HashSet<int> m_SelectedKeyHashes;
        [SerializeField] private List<int> m_SelectedKeyHashesSerialized;

        public HashSet<int> selectedKeyHashes
        {
            get { return m_SelectedKeyHashes ?? (m_SelectedKeyHashes = new HashSet<int>()); }
            set { m_SelectedKeyHashes = value; }
        }

        public void SaveSelection(string undoLabel)
        {
            Undo.RegisterCompleteObjectUndo(this, undoLabel);
        }

        public void OnBeforeSerialize()
        {
            m_SelectedKeyHashesSerialized = m_SelectedKeyHashes.ToList();
        }

        public void OnAfterDeserialize()
        {
            m_SelectedKeyHashes = new HashSet<int>(m_SelectedKeyHashesSerialized);
        }
    }
}
