using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace TimelineEditorInternal
{
    internal class TimelineWindowHierarchyNode : TreeViewItem
    {
        public string path;
        public System.Type animatableObjectType;
        public string propertyName;

        public EditorCurveBinding? binding;
        public TimelineWindowCurve[] curves;

        public float? topPixel = null;
        public int indent = 0;

        public TimelineWindowHierarchyNode(int instanceID, int depth, TreeViewItem parent, System.Type animatableObjectType, string propertyName, string path, string displayName)
            : base(instanceID, depth, parent, displayName)
        {
            this.displayName = displayName;
            this.animatableObjectType = animatableObjectType;
            this.propertyName = propertyName;
            this.path = path;
        }
    }

    internal class TimelineWindowHierarchyPropertyGroupNode : TimelineWindowHierarchyNode
    {
        public TimelineWindowHierarchyPropertyGroupNode(System.Type animatableObjectType, int setId, string propertyName, string path, TreeViewItem parent, string displayName)
            : base(TimelineWindowUtility.GetPropertyNodeID(setId, path, animatableObjectType, propertyName), parent != null ? parent.depth + 1 : -1, parent, animatableObjectType, TimelineWindowUtility.GetPropertyGroupName(propertyName), path, displayName)
        {}
    }

    internal class TimelineWindowHierarchyPropertyNode : TimelineWindowHierarchyNode
    {
        public bool isPptrNode;

        public TimelineWindowHierarchyPropertyNode(System.Type animatableObjectType, int setId, string propertyName, string path, TreeViewItem parent, EditorCurveBinding binding, bool isPptrNode, string displayName)
            : base(TimelineWindowUtility.GetPropertyNodeID(setId, path, animatableObjectType, propertyName), parent != null ? parent.depth + 1 : -1, parent, animatableObjectType, propertyName, path, displayName)
        {
            this.binding = binding;
            this.isPptrNode = isPptrNode;
        }
    }

    internal class TimelineWindowHierarchyClipNode : TimelineWindowHierarchyNode
    {
        public TimelineWindowHierarchyClipNode(TreeViewItem parent, int setId, string name)
            : base(setId, parent != null ? parent.depth + 1 : -1, parent, null, null, null, name)
        {}
    }

    internal class TimelineWindowHierarchyMasterNode : TimelineWindowHierarchyNode
    {
        public TimelineWindowHierarchyMasterNode()
            : base(0, -1, null, null, null, null, "")
        {}
    }

    // A special node to put "Add Curve" button in bottom of the tree
    internal class TimelineWindowHierarchyAddButtonNode : TimelineWindowHierarchyNode
    {
        public TimelineWindowHierarchyAddButtonNode()
            : base(0, -1, null, null, null, null, "")
        {}
    }
}
