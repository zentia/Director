using UnityEditor;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    internal class TimelineWindowHierarchyDataSource : TreeViewDataSource
    {
        // Animation window shared state
        private TimelineWindowState state { get; set; }
        public bool showAll { get; set; }

        public TimelineWindowHierarchyDataSource(TreeViewController treeView, TimelineWindowState animationWindowState)
            : base(treeView)
        {
            state = animationWindowState;
        }

        private void SetupRootNodeSettings()
        {
            showRootItem = false;
            rootIsCollapsable = false;
            SetExpanded(m_RootItem, true);
        }

        private TimelineWindowHierarchyNode GetEmptyRootNode()
        {
            return new TimelineWindowHierarchyNode(0, -1, null, null, "", "", "root");
        }

        public override void FetchData()
        {
            m_RootItem = GetEmptyRootNode();
            SetupRootNodeSettings();
            m_NeedRefreshRows = true;

            if (state.selection.disabled)
            {
                root.children = null;
                return;
            }

            List<TimelineWindowHierarchyNode> childNodes = new List<TimelineWindowHierarchyNode>();

            if (state.allCurves.Count > 0)
            {
                TimelineWindowHierarchyMasterNode masterNode = new TimelineWindowHierarchyMasterNode();
                masterNode.curves = state.allCurves.ToArray();

                childNodes.Add(masterNode);
            }

            childNodes.AddRange(CreateTreeFromCurves());
            childNodes.Add(new TimelineWindowHierarchyAddButtonNode());

            TreeViewUtility.SetChildParentReferences(new List<TreeViewItem>(childNodes.ToArray()), root);
        }

        public override bool IsRenamingItemAllowed(TreeViewItem item)
        {
            if (item is TimelineWindowHierarchyAddButtonNode || item is TimelineWindowHierarchyMasterNode || item is TimelineWindowHierarchyClipNode)
                return false;

            return (item as TimelineWindowHierarchyNode).path.Length != 0;
        }

        public List<TimelineWindowHierarchyNode> CreateTreeFromCurves()
        {
            List<TimelineWindowHierarchyNode> nodes = new List<TimelineWindowHierarchyNode>();
            List<TimelineWindowCurve> singlePropertyCurves = new List<TimelineWindowCurve>();

            TimelineWindowCurve[] curves = state.allCurves.ToArray();
            TimelineWindowHierarchyNode parentNode = (TimelineWindowHierarchyNode)m_RootItem;
            SerializedObject so = null;

            for (int i = 0; i < curves.Length; i++)
            {
                TimelineWindowCurve curve = curves[i];

                if (!state.ShouldShowCurve(curve))
                    continue;

                TimelineWindowCurve nextCurve = i < curves.Length - 1 ? curves[i + 1] : null;

                if (curve.isSerializeReferenceCurve && state.activeRootGameObject != null)
                {
                    var animatedObject = AnimationUtility.GetAnimatedObject(state.activeRootGameObject, curve.binding);
                    if (animatedObject != null && (so == null || so.targetObject != animatedObject))
                        so = new SerializedObject(animatedObject);
                }

                singlePropertyCurves.Add(curve);

                bool areSameGroup = nextCurve != null && TimelineWindowUtility.GetPropertyGroupName(nextCurve.propertyName) == TimelineWindowUtility.GetPropertyGroupName(curve.propertyName);
                bool areSamePathAndType = nextCurve != null && curve.path.Equals(nextCurve.path) && curve.type == nextCurve.type;

                // We expect curveBindings to come sorted by propertyname
                // So we compare curve vs nextCurve. If its different path or different group (think "scale.xyz" as group), then we know this is the last element of such group.
                if (i == curves.Length - 1 || !areSameGroup || !areSamePathAndType)
                {
                    if (singlePropertyCurves.Count > 1)
                        nodes.Add(AddPropertyGroupToHierarchy(singlePropertyCurves.ToArray(), parentNode, so));
                    else
                        nodes.Add(AddPropertyToHierarchy(singlePropertyCurves[0], parentNode, so));
                    singlePropertyCurves.Clear();
                }
            }

            return nodes;
        }

        private TimelineWindowHierarchyPropertyGroupNode AddPropertyGroupToHierarchy(TimelineWindowCurve[] curves, TimelineWindowHierarchyNode parentNode, SerializedObject so)
        {
            List<TimelineWindowHierarchyNode> childNodes = new List<TimelineWindowHierarchyNode>();

            System.Type animatableObjectType = curves[0].type;
            TimelineWindowHierarchyPropertyGroupNode node = new TimelineWindowHierarchyPropertyGroupNode(animatableObjectType, 0, TimelineWindowUtility.GetPropertyGroupName(curves[0].propertyName), curves[0].path, parentNode, TimelineWindowUtility.GetNicePropertyGroupDisplayName(curves[0].binding, so));

            node.icon = GetIcon(curves[0].binding);

            node.indent = curves[0].depth;
            node.curves = curves;

            foreach (TimelineWindowCurve curve in curves)
            {
                TimelineWindowHierarchyPropertyNode childNode = AddPropertyToHierarchy(curve, node, so);
                // For child nodes we do not want to display the type in front (It is already shown by the group node)
                childNode.displayName = TimelineWindowUtility.GetPropertyDisplayName(childNode.propertyName);
                childNodes.Add(childNode);
            }

            TreeViewUtility.SetChildParentReferences(new List<TreeViewItem>(childNodes.ToArray()), node);
            return node;
        }

        private TimelineWindowHierarchyPropertyNode AddPropertyToHierarchy(TimelineWindowCurve curve, TimelineWindowHierarchyNode parentNode, SerializedObject so)
        {
            TimelineWindowHierarchyPropertyNode node = new TimelineWindowHierarchyPropertyNode(curve.type, 0, curve.propertyName, curve.path, parentNode, curve.binding, curve.isPPtrCurve, TimelineWindowUtility.GetNicePropertyDisplayName(curve.binding, so));

            if (parentNode.icon != null)
                node.icon = parentNode.icon;
            else
                node.icon = GetIcon(curve.binding);

            node.indent = curve.depth;
            node.curves = new[] { curve };
            return node;
        }

        public Texture2D GetIcon(EditorCurveBinding curveBinding)
        {
            if (state.activeRootGameObject != null)
            {
                Object animatedObject = AnimationUtility.GetAnimatedObject(state.activeRootGameObject, curveBinding);
                if (animatedObject != null)
                    return AssetPreview.GetMiniThumbnail(animatedObject);
            }
            return AssetPreview.GetMiniTypeThumbnail(curveBinding.type);
        }

        public void UpdateSerializeReferenceCurvesArrayNiceDisplayName()
        {
            if (state.activeRootGameObject == null)
                return;

            //This is required in the case that there might have been a topological change
            //leading to a different display name(topological path)
            SerializedObject so = null;
            foreach (TimelineWindowHierarchyNode hierarchyNode in GetRows())
            {
                if (hierarchyNode.curves != null)
                {
                    foreach (var curve in hierarchyNode.curves)
                    {
                        if (curve.isSerializeReferenceCurve && hierarchyNode.displayName.Contains(".Array.data["))
                        {
                            var animatedObject = AnimationUtility.GetAnimatedObject(state.activeRootGameObject, curve.binding);
                            if (animatedObject != null && (so == null || so.targetObject != animatedObject))
                                so = new SerializedObject(animatedObject);

                            hierarchyNode.displayName = TimelineWindowUtility.GetNicePropertyDisplayName(curve.binding, so);
                        }
                    }
                }
            }

        }

        public void UpdateData()
        {
            m_TreeView.ReloadData();
        }
    }
}
