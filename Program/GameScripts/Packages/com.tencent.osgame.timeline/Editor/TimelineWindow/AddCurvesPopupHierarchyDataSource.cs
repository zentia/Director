using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    internal class AddCurvesPopupHierarchyDataSource : TreeViewDataSource
    {
        public static bool showEntireHierarchy { get; set; }

        public AddCurvesPopupHierarchyDataSource(TreeViewController treeView)
            : base(treeView)
        {
            showRootItem = false;
            rootIsCollapsable = false;
        }

        private void SetupRootNodeSettings()
        {
            showRootItem = false;
            SetExpanded(root, true);
        }

        public override void FetchData()
        {
            m_RootItem = null;
            if (AddCurvesPopup.s_State.selection.canAddCurves)
            {
                GameObject rootGameObject = AddCurvesPopup.s_State.activeRootGameObject;
                ScriptableObject scriptableObject = AddCurvesPopup.s_State.activeScriptableObject;

                if (rootGameObject != null)
                {
                    AddGameObjectToHierarchy(rootGameObject, rootGameObject, AddCurvesPopup.s_State.activeAnimationClip, m_RootItem);
                }
                else if (scriptableObject != null)
                {
                    AddScriptableObjectToHierarchy(scriptableObject, AddCurvesPopup.s_State.activeAnimationClip, m_RootItem);
                }
            }

            SetupRootNodeSettings();
            m_NeedRefreshRows = true;
        }

        private TreeViewItem AddGameObjectToHierarchy(GameObject gameObject, GameObject rootGameObject, AnimationClip animationClip, TreeViewItem parent)
        {
            string path = AnimationUtility.CalculateTransformPath(gameObject.transform, rootGameObject.transform);
            AddCurvesPopupGameObjectNode node = new AddCurvesPopupGameObjectNode(gameObject, parent, gameObject.name);
            List<TreeViewItem> childNodes = new List<TreeViewItem>();

            if (m_RootItem == null)
                m_RootItem = node;

            // Iterate over all animatable objects
            EditorCurveBinding[] allCurveBindings = AnimationUtility.GetAnimatableBindings(gameObject, rootGameObject);
            List<EditorCurveBinding> singleObjectBindings = new List<EditorCurveBinding>();
            for (int i = 0; i < allCurveBindings.Length; i++)
            {
                EditorCurveBinding curveBinding = allCurveBindings[i];

                singleObjectBindings.Add(curveBinding);

                // Don't create group for GameObject.m_IsActive. It looks messy
                if (curveBinding.propertyName == "m_IsActive")
                {
                    // Don't show for the root go
                    if (curveBinding.path != "")
                    {
                        TreeViewItem newNode = CreateNode(singleObjectBindings.ToArray(), node, null);
                        if (newNode != null)
                            childNodes.Add(newNode);
                        singleObjectBindings.Clear();
                    }
                    else
                    {
                        singleObjectBindings.Clear();
                    }
                }
                else
                {
                    // We expect allCurveBindings to come sorted by type

                    bool isLastItemOverall = (i == allCurveBindings.Length - 1);
                    bool isLastItemOnThisGroup = false;

                    if (!isLastItemOverall)
                        isLastItemOnThisGroup = (allCurveBindings[i + 1].type != curveBinding.type);

                    // Let's not add those that already have a existing curve.
                    if (TimelineWindowUtility.IsCurveCreated(animationClip, curveBinding))
                        singleObjectBindings.Remove(curveBinding);

                    // Remove animator enabled property which shouldn't be animated.
                    if (curveBinding.type == typeof(Animator) && curveBinding.propertyName == "m_Enabled")
                        singleObjectBindings.Remove(curveBinding);

                    if ((isLastItemOverall || isLastItemOnThisGroup) && singleObjectBindings.Count > 0)
                    {
                        childNodes.Add(AddAnimatableObjectToHierarchy(singleObjectBindings.ToArray(), node, path));
                        singleObjectBindings.Clear();
                    }
                }
            }

            if (showEntireHierarchy)
            {
                // Iterate over all child GOs
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    Transform childTransform = gameObject.transform.GetChild(i);
                    TreeViewItem childNode = AddGameObjectToHierarchy(childTransform.gameObject, rootGameObject, animationClip, node);
                    if (childNode != null)
                        childNodes.Add(childNode);
                }
            }

            // Remove Leaf nodes without properties.
            if (childNodes.Count == 0)
            {
                node.parent = null;
                return null;
            }

            TreeViewUtility.SetChildParentReferences(childNodes, node);
            return node;
        }

        private TreeViewItem AddScriptableObjectToHierarchy(ScriptableObject scriptableObject, AnimationClip clip, TreeViewItem parent)
        {
            EditorCurveBinding[] allCurveBindings = AnimationUtility.GetAnimatableBindings(scriptableObject);
            EditorCurveBinding[] availableBindings = allCurveBindings.Where(c => !TimelineWindowUtility.IsCurveCreated(clip, c)).ToArray();

            TreeViewItem node = null;
            if (availableBindings.Length > 0)
                node = AddAnimatableObjectToHierarchy(availableBindings, parent, "");
            else
                node = new AddCurvesPopupObjectNode(parent, "", scriptableObject.name);

            if (m_RootItem == null)
                m_RootItem = node;

            return node;
        }

        static string GetClassName(EditorCurveBinding binding)
        {
            if (AddCurvesPopup.s_State.activeRootGameObject != null)
            {
                Object target = AnimationUtility.GetAnimatedObject(AddCurvesPopup.s_State.activeRootGameObject, binding);
                if (target)
                    return ObjectNames.GetInspectorTitle(target);
            }

            return binding.type.Name;
        }

        static Texture2D GetIcon(EditorCurveBinding binding)
        {
            if (AddCurvesPopup.s_State.activeRootGameObject != null)
            {
                return AssetPreview.GetMiniThumbnail(AnimationUtility.GetAnimatedObject(AddCurvesPopup.s_State.activeRootGameObject, binding));
            }
            else if (AddCurvesPopup.s_State.activeScriptableObject != null)
            {
                return AssetPreview.GetMiniThumbnail(AddCurvesPopup.s_State.activeScriptableObject);
            }

            return null;
        }

        private TreeViewItem AddAnimatableObjectToHierarchy(EditorCurveBinding[] curveBindings, TreeViewItem parentNode, string path)
        {
            TreeViewItem node = new AddCurvesPopupObjectNode(parentNode, path, GetClassName(curveBindings[0]));
            node.icon = GetIcon(curveBindings[0]);

            List<TreeViewItem> childNodes = new List<TreeViewItem>();
            List<EditorCurveBinding> singlePropertyBindings = new List<EditorCurveBinding>();
            SerializedObject so = null;

            for (int i = 0; i < curveBindings.Length; i++)
            {
                EditorCurveBinding curveBinding = curveBindings[i];
                if (curveBinding.isSerializeReferenceCurve)
                {
                    var animatedObject = AnimationUtility.GetAnimatedObject(AddCurvesPopup.s_State.activeRootGameObject, curveBinding);
                    if (animatedObject != null && (so == null || so.targetObject != animatedObject))
                        so = new SerializedObject(animatedObject);
                }

                singlePropertyBindings.Add(curveBinding);

                // We expect curveBindings to come sorted by propertyname
                if (i == curveBindings.Length - 1 || TimelineWindowUtility.GetPropertyGroupName(curveBindings[i + 1].propertyName) != TimelineWindowUtility.GetPropertyGroupName(curveBinding.propertyName))
                {
                    TreeViewItem newNode = CreateNode(singlePropertyBindings.ToArray(), node, so);
                    if (newNode != null)
                        childNodes.Add(newNode);
                    singlePropertyBindings.Clear();
                }
            }

            childNodes.Sort();

            TreeViewUtility.SetChildParentReferences(childNodes, node);
            return node;
        }

        private TreeViewItem CreateNode(EditorCurveBinding[] curveBindings, TreeViewItem parentNode, SerializedObject so)
        {
            var node = new AddCurvesPopupPropertyNode(parentNode, curveBindings, TimelineWindowUtility.GetNicePropertyDisplayName(curveBindings[0], so));

            // For RectTransform.position we only want .z
            if (TimelineWindowUtility.IsRectTransformPosition(node.curveBindings[0]))
                node.curveBindings = new EditorCurveBinding[] {node.curveBindings[2]};

            node.icon = parentNode.icon;
            return node;
        }

        public void UpdateData()
        {
            m_TreeView.ReloadData();
        }
    }

    internal class AddCurvesPopupGameObjectNode : TreeViewItem
    {
        public AddCurvesPopupGameObjectNode(GameObject gameObject, TreeViewItem parent, string displayName)
            : base(gameObject.GetInstanceID(), parent != null ? parent.depth + 1 : -1, parent, displayName)
        {
        }
    }

    internal class AddCurvesPopupObjectNode : TreeViewItem
    {
        public AddCurvesPopupObjectNode(TreeViewItem parent, string path, string className)
            : base((path + className).GetHashCode(), parent != null ? parent.depth + 1 : -1, parent, className)
        {
        }
    }

    internal class AddCurvesPopupPropertyNode : TreeViewItem
    {
        public EditorCurveBinding[] curveBindings;

        public AddCurvesPopupPropertyNode(TreeViewItem parent, EditorCurveBinding[] curveBindings, string displayPath)
            : base(curveBindings[0].GetHashCode(), parent.depth + 1, parent, displayPath)
        {
            this.curveBindings = curveBindings;
        }

        public override int CompareTo(TreeViewItem other)
        {
            AddCurvesPopupPropertyNode otherNode = other as AddCurvesPopupPropertyNode;
            if (otherNode != null)
            {
                if (displayName.Contains("Rotation") && otherNode.displayName.Contains("Position"))
                    return 1;
                if (displayName.Contains("Position") && otherNode.displayName.Contains("Rotation"))
                    return -1;
            }
            return base.CompareTo(other);
        }
    }
}
