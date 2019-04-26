using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeDirector
{
    public class NodeControl
    {
        public static List<NodeEntity>[] m_NodeInstanceGroups = new List<NodeEntity>[(int)NodeKind.Count];
    }
}
