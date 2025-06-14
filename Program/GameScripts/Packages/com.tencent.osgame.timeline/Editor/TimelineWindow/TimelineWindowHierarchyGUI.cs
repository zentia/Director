using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TimelineEditor;
using TimelineRuntime;
using UnityEditor.IMGUI.Controls;
using CurveUtility = UnityEditor.CurveUtility;
using RotationCurveInterpolation = UnityEditor.RotationCurveInterpolation;

namespace TimelineEditorInternal
{
    internal class TimelineWindowHierarchyGUI : TreeViewGUI
    {
        public TimelineWindowState state { get; set; }

        readonly GUIContent k_AnimatePropertyLabel = EditorGUIUtility.TrTextContent("Add Track Group");

        private GUIStyle m_AnimationRowEvenStyle;
        private GUIStyle m_AnimationRowOddStyle;
        private GUIStyle m_AnimationSelectionTextField;
        private GUIStyle m_AnimationCurveDropdown;
        private bool m_StyleInitialized;
        private TimelineWindowHierarchyNode m_RenamedNode;
        private Color m_LightSkinPropertyTextColor = new Color(.35f, .35f, .35f);
        private Color m_PhantomCurveColor = new Color(0f, 153f / 255f, 153f / 255f);

        private int[] m_HierarchyItemFoldControlIDs;
        private int[] m_HierarchyItemValueControlIDs;
        private int[] m_HierarchyItemButtonControlIDs;

        private const float k_RowRightOffset = 10;
        private const float k_ValueFieldDragWidth = 15;
        private const float k_ValueFieldWidth = 80;
        private const float k_ValueFieldOffsetFromRightSide = 100;
        private const float k_ColorIndicatorTopMargin = 3;
        public static readonly float k_DopeSheetRowHeight = EditorGUI.kSingleLineHeight;
        public static readonly float k_DopeSheetRowHeightTall = k_DopeSheetRowHeight * 2f;
        public const float k_AddCurveButtonNodeHeight = 40f;
        public const float k_RowBackgroundColorBrightness = 0.28f;
        private const float k_SelectedPhantomCurveColorMultiplier = 1.4f;
        private const float k_CurveColorIndicatorIconSize = 11;

        private readonly static Color k_KeyColorInDopesheetMode = new Color(0.7f, 0.7f, 0.7f, 1);
        private readonly static Color k_KeyColorForNonCurves = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        private readonly static Color k_LeftoverCurveColor = Color.yellow;

        private static readonly string k_DefaultValue = L10n.Tr(" (Default Value)");
        private static readonly string k_TransformPosition = L10n.Tr("Transform position, rotation and scale can't be partially animated. This value will be animated to the default value");
        private static readonly string k_Missing = L10n.Tr(" (Missing!)");
        private static readonly string k_GameObjectComponentMissing = L10n.Tr("The GameObject or Component is missing ({0})");
        private static readonly string k_DuplicateGameObjectName = L10n.Tr(" (Duplicate GameObject name!)");
        private static readonly string k_TargetForCurveIsAmbigous = L10n.Tr("Target for curve is ambiguous since there are multiple GameObjects with same name ({0})");
        private static readonly string k_RemoveProperties = L10n.Tr("Remove Properties");
        private static readonly string k_RemoveProperty = L10n.Tr("Remove Property");
        private static readonly string k_AddKey = L10n.Tr("Add Key");
        private static readonly string k_DeleteKey = L10n.Tr("Delete Key");
        private static readonly string k_RemoveCurve = L10n.Tr("Remove Curve");
        private static readonly string k_CopyCurve = L10n.Tr("Copy Curve");

        internal static int s_WasInsideValueRectFrame = -1;

        public TimelineWindowHierarchyGUI(TreeViewController treeView, TimelineWindowState state)
            : base(treeView)
        {
            this.state = state;
            InitStyles();
        }

        protected void InitStyles()
        {
            if (!m_StyleInitialized)
            {
                m_AnimationRowEvenStyle = "AnimationRowEven";
                m_AnimationRowOddStyle = "AnimationRowOdd";
                m_AnimationSelectionTextField = "AnimationSelectionTextField";

                lineStyle = Styles.lineStyle;
                lineStyle.padding.left = 0;

                m_AnimationCurveDropdown = "AnimPropDropdown";

                m_StyleInitialized = true;
            }
        }

        protected void DoNodeGUI(Rect rect, TimelineWindowHierarchyNode node, bool selected, bool focused, int row)
        {
            InitStyles();

            if (node is TimelineWindowHierarchyMasterNode)
                return;

            float indent = k_BaseIndent + (node.depth + node.indent) * k_IndentWidth;

            if (node is TimelineWindowHierarchyAddButtonNode)
            {
                if (Event.current.type == EventType.MouseMove && s_WasInsideValueRectFrame >= 0)
                {
                    if (s_WasInsideValueRectFrame >= Time.frameCount - 1)
                        Event.current.Use();
                    else
                        s_WasInsideValueRectFrame = -1;
                }

                using (new EditorGUI.DisabledScope(!state.selection.canAddCurves))
                {
                    DoAddCurveButton(rect, node, row);
                }
            }
            else
            {
                DoRowBackground(rect, row);
                DoIconAndName(rect, node, selected, focused, indent);
                DoFoldout(node, rect, indent, row);

                bool enabled = false;
                if (node.curves != null)
                {
                    enabled = !Array.Exists(node.curves, curve => curve.animationIsEditable == false);
                }

                using (new EditorGUI.DisabledScope(!enabled))
                {
                    DoValueField(rect, node, row);
                }
                DoCurveDropdown(rect, node, row, enabled);
                HandleContextMenu(rect, node, enabled);
                DoCurveColorIndicator(rect, node);
            }
            EditorGUIUtility.SetIconSize(Vector2.zero);
        }

        public override void BeginRowGUI()
        {
            base.BeginRowGUI();
            HandleDelete();

            // Reserve unique control ids.
            // This is required to avoid changing control ids mapping as we scroll in the tree view
            // and change items visibility.  Hopefully, we should be able to remove it entirely if we
            // isolate the animation window layouts in separate IMGUIContainers...
            int rowCount = m_TreeView.data.rowCount;
            m_HierarchyItemFoldControlIDs = new int[rowCount];
            m_HierarchyItemValueControlIDs = new int[rowCount];
            m_HierarchyItemButtonControlIDs = new int[rowCount];
            for (int i = 0; i < rowCount; ++i)
            {
                var propertyNode  = m_TreeView.data.GetItem(i) as TimelineWindowHierarchyPropertyNode;
                if (propertyNode != null && !propertyNode.isPptrNode)
                    m_HierarchyItemValueControlIDs[i] = GUIUtility.GetControlID(FocusType.Keyboard);
                else
                    m_HierarchyItemValueControlIDs[i] = 0; // not needed.

                m_HierarchyItemFoldControlIDs[i] = GUIUtility.GetControlID(FocusType.Passive);
                m_HierarchyItemButtonControlIDs[i] = GUIUtility.GetControlID(FocusType.Passive);
            }
        }

        private void DoAddCurveButton(Rect rect, TimelineWindowHierarchyNode node, int row)
        {
            const int k_ButtonWidth = 230;
            float xMargin = (rect.width - k_ButtonWidth) / 2f;
            float yMargin = 10f;

            Rect rectWithMargin = new Rect(rect.xMin + xMargin, rect.yMin + yMargin, rect.width - xMargin * 2f, rect.height - yMargin * 2f);

            if (GUI.Button(rectWithMargin, k_AnimatePropertyLabel, GUI.skin.button))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("ActorTrackGroup"),false, OnNewActorTrackGroupAdded, null);
                menu.AddItem(new GUIContent("DirectorGroup"),false, OnNewDirectorGroupAdded, null);
                menu.ShowAsContext();    
            }
            // case 767863.
            // This control id is unique to the hierarchy node it refers to.
            // The tree view only renders the elements that are visible, and will cause
            // the control id counter to shift when scrolling through the view.
            // if (DoTreeViewButton(m_HierarchyItemButtonControlIDs[row], rectWithMargin, k_AnimatePropertyLabel, GUI.skin.button))
            // {
            //     AddCurvesPopupHierarchyDataSource.showEntireHierarchy = true;
            //
            //     if (AddCurvesPopup.ShowAtPosition(rectWithMargin, state, OnNewCurveAdded))
            //     {
            //         GUIUtility.ExitGUI();
            //     }
            // }
        }

        private void OnNewActorTrackGroupAdded(object userdata)
        {
            TimelineFactory.CreateTrackGroup(state.activeTimeline, typeof(ActorTrackGroup), "ActorTrackGroup");
        }

        private void OnNewDirectorGroupAdded(object userdata)
        {
            TimelineFactory.CreateTrackGroup(state.activeTimeline, typeof(DirectorGroup), "DirectorGroup");
        }

        private void DoRowBackground(Rect rect, int row)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Different background for even rows
            if (row % 2 == 0)
                m_AnimationRowEvenStyle.Draw(rect, false, false, false, false);
            else
                m_AnimationRowOddStyle.Draw(rect, false, false, false, false);
        }

        // Draw foldout (after text content above to ensure drop down icon is rendered above selection highlight)
        private void DoFoldout(TimelineWindowHierarchyNode node, Rect rect, float indent, int row)
        {
            if (m_TreeView.data.IsExpandable(node))
            {
                Rect toggleRect = rect;
                toggleRect.x = indent;
                toggleRect.width = foldoutStyleWidth;
                EditorGUI.BeginChangeCheck();
                bool newExpandedValue = GUI.Toggle(toggleRect, m_HierarchyItemFoldControlIDs[row], m_TreeView.data.IsExpanded(node), GUIContent.none, foldoutStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    if (Event.current.alt)
                        m_TreeView.data.SetExpandedWithChildren(node, newExpandedValue);
                    else
                        m_TreeView.data.SetExpanded(node, newExpandedValue);
                }
            }
            else
            {
                TimelineWindowHierarchyPropertyNode hierarchyPropertyNode = node as TimelineWindowHierarchyPropertyNode;
                TimelineWindowHierarchyState hierarchyState = m_TreeView.state as TimelineWindowHierarchyState;

                if (hierarchyPropertyNode != null && hierarchyPropertyNode.isPptrNode)
                {
                    Rect toggleRect = rect;
                    toggleRect.x = indent;
                    toggleRect.width = foldoutStyleWidth;

                    EditorGUI.BeginChangeCheck();
                    bool tallMode = hierarchyState.GetTallMode(hierarchyPropertyNode);
                    tallMode = GUI.Toggle(toggleRect, m_HierarchyItemFoldControlIDs[row], tallMode, GUIContent.none, foldoutStyle);
                    if (EditorGUI.EndChangeCheck())
                        hierarchyState.SetTallMode(hierarchyPropertyNode, tallMode);
                }
            }
        }

        private void DoIconAndName(Rect rect, TimelineWindowHierarchyNode node, bool selected, bool focused, float indent)
        {
            EditorGUIUtility.SetIconSize(new Vector2(13, 13));   // If not set we see icons scaling down if text is being cropped

            // TODO: All this is horrible. SHAME FIX!
            if (Event.current.type == EventType.Repaint)
            {
                if (selected)
                    selectionStyle.Draw(rect, false, false, true, focused);

                // Leave some space for the value field that comes after.
                if (node is TimelineWindowHierarchyPropertyNode)
                    rect.width -= k_ValueFieldOffsetFromRightSide + 2;

                bool isLeftOverCurve = TimelineWindowUtility.IsNodeLeftOverCurve(node);
                bool isAmbiguous = TimelineWindowUtility.IsNodeAmbiguous(node);
                bool isPhantom = TimelineWindowUtility.IsNodePhantom(node);

                string warningText = "";
                string tooltipText = "";
                if (isPhantom)
                {
                    warningText = k_DefaultValue;
                    tooltipText = k_TransformPosition;
                }
                if (isLeftOverCurve)
                {
                    warningText = k_Missing;
                    tooltipText = string.Format(k_GameObjectComponentMissing, node.path);
                }
                if (isAmbiguous)
                {
                    warningText = k_DuplicateGameObjectName;
                    tooltipText = string.Format(k_TargetForCurveIsAmbigous, node.path);
                }

                Color oldColor = lineStyle.normal.textColor;
                Color textColor = oldColor;
                if (node.depth == 0)
                {
                    string nodePrefix = "";
                    if (node.curves.Length > 0)
                    {
                        TimelineWindowSelectionItem selectionBinding = node.curves[0].selectionBinding;
                        string gameObjectName = GetGameObjectName(selectionBinding != null ? selectionBinding.rootGameObject : null, node.path);
                        nodePrefix = string.IsNullOrEmpty(gameObjectName) ? "" : gameObjectName + " : ";
                    }

                    Styles.content = new GUIContent(nodePrefix + node.displayName + warningText, GetIconForItem(node), tooltipText);

                    textColor = EditorGUIUtility.isProSkin ? Color.gray * 1.35f : Color.black;
                }
                else
                {
                    Styles.content = new GUIContent(node.displayName + warningText, GetIconForItem(node), tooltipText);

                    textColor = EditorGUIUtility.isProSkin ? Color.gray : m_LightSkinPropertyTextColor;

                    var phantomColor = selected ? m_PhantomCurveColor * k_SelectedPhantomCurveColorMultiplier : m_PhantomCurveColor;
                    textColor = isPhantom ? phantomColor : textColor;
                }
                textColor = isLeftOverCurve || isAmbiguous ? k_LeftoverCurveColor : textColor;
                SetStyleTextColor(lineStyle, textColor);

                rect.xMin += (int)(indent + foldoutStyleWidth + lineStyle.margin.left);
                rect.yMin = rect.y + (rect.height - EditorGUIUtility.singleLineHeight) / 2;
                GUI.Label(rect, Styles.content, lineStyle);

                SetStyleTextColor(lineStyle, oldColor);
            }

            if (IsRenaming(node.id) && Event.current.type != EventType.Layout)
                GetRenameOverlay().editFieldRect = new Rect(rect.x + k_IndentWidth, rect.y, rect.width - k_IndentWidth - 1, rect.height);
        }

        private string GetGameObjectName(GameObject rootGameObject, string path)
        {
            if (string.IsNullOrEmpty(path))
                return rootGameObject != null ? rootGameObject.name : "";

            string[] splits = path.Split('/');
            return splits[splits.Length - 1];
        }

        private void DoValueField(Rect rect, TimelineWindowHierarchyNode node, int row)
        {
            bool curvesChanged = false;

            if (node is TimelineWindowHierarchyPropertyNode)
            {
                TimelineWindowCurve[] curves = node.curves;
                if (curves == null || curves.Length == 0)
                    return;

                // We do valuefields for dopelines that only have single curve
                TimelineWindowCurve curve = curves[0];
                object value = CurveBindingUtility.GetCurrentValue(state, curve);

                if (!curve.isPPtrCurve)
                {
                    Rect valueFieldDragRect = new Rect(rect.xMax - k_ValueFieldOffsetFromRightSide - k_ValueFieldDragWidth, rect.y, k_ValueFieldDragWidth, rect.height);
                    Rect valueFieldRect = new Rect(rect.xMax - k_ValueFieldOffsetFromRightSide, rect.y, k_ValueFieldWidth, rect.height);

                    if (Event.current.type == EventType.MouseMove && valueFieldRect.Contains(Event.current.mousePosition))
                        s_WasInsideValueRectFrame = Time.frameCount;

                    EditorGUI.BeginChangeCheck();

                    if (curve.valueType == typeof(bool))
                    {
                        value = GUI.Toggle(valueFieldRect, m_HierarchyItemValueControlIDs[row], (float)value != 0, GUIContent.none, EditorStyles.toggle) ? 1f : 0f;
                    }
                    else
                    {
                        int id = m_HierarchyItemValueControlIDs[row];
                        bool enterInTextField = (EditorGUIUtility.keyboardControl == id
                            && EditorGUIUtility.editingTextField
                            && Event.current.type == EventType.KeyDown
                            && (Event.current.character == '\n' || (int)Event.current.character == 3));

                        //  Force back keyboard focus to float field editor when editing it.
                        //  TreeView forces keyboard focus on itself at mouse down and we lose focus here.
                        if (EditorGUI.s_RecycledEditor.controlID == id && Event.current.type == EventType.MouseDown && valueFieldRect.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.keyboardControl = id;
                        }

                        if (curve.isDiscreteCurve)
                        {
                            value = EditorGUI.DoIntField(EditorGUI.s_RecycledEditor,
                                valueFieldRect,
                                valueFieldDragRect,
                                id,
                                (int)value,
                                EditorGUI.kIntFieldFormatString,
                                m_AnimationSelectionTextField,
                                true,
                                0);
                            if (enterInTextField)
                            {
                                GUI.changed = true;
                                Event.current.Use();
                            }
                        }
                        else
                        {
                            value = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor,
                                valueFieldRect,
                                valueFieldDragRect,
                                id,
                                (float)value,
                                "g5",
                                m_AnimationSelectionTextField,
                                true);
                            if (enterInTextField)
                            {
                                GUI.changed = true;
                                Event.current.Use();
                            }

                            if (float.IsInfinity((float)value) || float.IsNaN((float)value))
                                value = 0f;
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        string undoLabel = "Edit Key";

                        TimelineKeyTime newAnimationKeyTime = TimelineKeyTime.Time(state.currentTime, curve.clip.frameRate);
                        TimelineWindowUtility.AddKeyframeToCurve(curve, value, curve.valueType, newAnimationKeyTime);

                        state.SaveCurve(curve.clip, curve, undoLabel);
                        curvesChanged = true;
                    }
                }
            }

            if (curvesChanged)
            {
                //Fix for case 1382193: Stop recording any candidates if a property value field is modified
                if (AnimationMode.IsRecordingCandidates())
                    state.ClearCandidates();

                state.ResampleAnimation();
            }
        }

        private bool DoTreeViewButton(int id, Rect position, GUIContent content, GUIStyle style)
        {
            Event evt = Event.current;
            EventType type = evt.GetTypeForControl(id);
            switch (type)
            {
                case EventType.Repaint:
                    style.Draw(position, content, id, false, position.Contains(evt.mousePosition));
                    break;
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (position.Contains(evt.mousePosition))
                        {
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        private void DoCurveDropdown(Rect rect, TimelineWindowHierarchyNode node, int row, bool enabled)
        {
            rect = new Rect(
                rect.xMax - k_RowRightOffset - 12,
                rect.yMin + 2 + (rect.height - EditorGUIUtility.singleLineHeight) / 2,
                22, 12);

            // case 767863.
            // This control id is unique to the hierarchy node it refers to.
            // The tree view only renders the elements that are visible, and will cause
            // the control id counter to shift when scrolling through the view.
            if (DoTreeViewButton(m_HierarchyItemButtonControlIDs[row], rect, GUIContent.none, m_AnimationCurveDropdown))
            {
                state.SelectHierarchyItem(node.id, false, false);
                GenericMenu menu = GenerateMenu(new TimelineWindowHierarchyNode[] { node }.ToList(), enabled);
                menu.DropDown(rect);
                Event.current.Use();
            }
        }

        private void DoCurveColorIndicator(Rect rect, TimelineWindowHierarchyNode node)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color originalColor = GUI.color;

            if (!state.showCurveEditor)
                GUI.color = k_KeyColorInDopesheetMode;
            else if (node.curves.Length == 1 && !node.curves[0].isPPtrCurve)
                GUI.color = CurveUtility.GetPropertyColor(node.curves[0].binding.propertyName);
            else
                GUI.color = k_KeyColorForNonCurves;

            bool hasKey = false;
            if (state.previewing)
            {
                foreach (var curve in node.curves)
                {
                    if (curve.m_Keyframes.Any(key => state.time.ContainsTime(key.time)))
                    {
                        hasKey = true;
                    }
                }
            }
            Texture icon = hasKey ? CurveUtility.GetIconKey() : CurveUtility.GetIconCurve();
            rect = new Rect(rect.xMax - k_RowRightOffset - (k_CurveColorIndicatorIconSize / 2) - 5, rect.yMin + k_ColorIndicatorTopMargin + (rect.height - EditorGUIUtility.singleLineHeight) / 2, k_CurveColorIndicatorIconSize, k_CurveColorIndicatorIconSize);
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true, 1);

            GUI.color = originalColor;
        }

        private void HandleDelete()
        {
            if (m_TreeView.HasFocus())
            {
                switch (Event.current.type)
                {
                    case EventType.ExecuteCommand:
                        if ((Event.current.commandName == EventCommandNames.SoftDelete || Event.current.commandName == EventCommandNames.Delete))
                        {
                            if (Event.current.type == EventType.ExecuteCommand)
                                RemoveCurvesFromSelectedNodes();
                            Event.current.Use();
                        }
                        break;

                    case EventType.KeyDown:
                        if (Event.current.keyCode == KeyCode.Backspace || Event.current.keyCode == KeyCode.Delete)
                        {
                            RemoveCurvesFromSelectedNodes();
                            Event.current.Use();
                        }
                        break;
                }
            }
        }

        private void HandleContextMenu(Rect rect, TimelineWindowHierarchyNode node, bool enabled)
        {
            if (Event.current.type != EventType.ContextClick)
                return;

            if (rect.Contains(Event.current.mousePosition))
            {
                state.SelectHierarchyItem(node.id, true, true);
                //state.animationWindow.RefreshShownCurves (true);
                GenerateMenu(state.selectedHierarchyNodes, enabled).ShowAsContext();
                Event.current.Use();
            }
        }

        private GenericMenu GenerateMenu(List<TimelineWindowHierarchyNode> interactedNodes, bool enabled)
        {
            List<TimelineWindowCurve> curves = GetCurvesAffectedByNodes(interactedNodes, false);
            // Linked curves are like regular affected curves but always include transform siblings
            List<TimelineWindowCurve> linkedCurves = GetCurvesAffectedByNodes(interactedNodes, true);

            bool forceGroupRemove = curves.Count == 1 ? TimelineWindowUtility.ForceGrouping(curves[0].binding) : false;

            GenericMenu menu = new GenericMenu();

            // Remove curves
            GUIContent removePropertyContent = new GUIContent(curves.Count > 1 || forceGroupRemove ? k_RemoveProperties : k_RemoveProperty);
            if (!enabled)
                menu.AddDisabledItem(removePropertyContent);
            else
            {
                menu.AddItem(removePropertyContent, false, RemoveCurvesFromSelectedNodes);
                if (curves.Count == 1)
                {
                    menu.AddItem(new GUIContent(k_CopyCurve), false, CopyCurve, curves[0]);
                }
            }

            // Change rotation interpolation
            bool showInterpolation = true;
            EditorCurveBinding[] curveBindings = new EditorCurveBinding[linkedCurves.Count];
            for (int i = 0; i < linkedCurves.Count; i++)
                curveBindings[i] = linkedCurves[i].binding;
            RotationCurveInterpolation.Mode rotationInterpolation = GetRotationInterpolationMode(curveBindings);
            if (rotationInterpolation == RotationCurveInterpolation.Mode.Undefined)
            {
                showInterpolation = false;
            }
            else
            {
                foreach (var node in interactedNodes)
                {
                    if (!(node is TimelineWindowHierarchyPropertyGroupNode))
                        showInterpolation = false;
                }
            }
            if (showInterpolation)
            {
                string legacyWarning = state.activeAnimationClip.legacy ? " (Not fully supported in Legacy)" : "";
                GenericMenu.MenuFunction2 nullMenuFunction2 = null;
                menu.AddItem(EditorGUIUtility.TrTextContent("Interpolation/Euler Angles" + legacyWarning), rotationInterpolation == RotationCurveInterpolation.Mode.RawEuler, enabled ? ChangeRotationInterpolation : nullMenuFunction2, RotationCurveInterpolation.Mode.RawEuler);
                menu.AddItem(EditorGUIUtility.TrTextContent("Interpolation/Euler Angles (Quaternion)"), rotationInterpolation == RotationCurveInterpolation.Mode.Baked, enabled ? ChangeRotationInterpolation : nullMenuFunction2, RotationCurveInterpolation.Mode.Baked);
                menu.AddItem(EditorGUIUtility.TrTextContent("Interpolation/Quaternion"), rotationInterpolation == RotationCurveInterpolation.Mode.NonBaked, enabled ? ChangeRotationInterpolation : nullMenuFunction2, RotationCurveInterpolation.Mode.NonBaked);
            }

            // Menu items that are only applicaple when in animation mode:
            if (state.previewing)
            {
                menu.AddSeparator("");

                bool allHaveKeys = true;
                bool noneHaveKeys = true;
                foreach (TimelineWindowCurve curve in curves)
                {
                    bool curveHasKey = curve.HasKeyframe(state.time);
                    if (!curveHasKey)
                        allHaveKeys = false;
                    else
                        noneHaveKeys = false;
                }

                string str;

                str = k_AddKey;
                if (allHaveKeys || !enabled)
                    menu.AddDisabledItem(new GUIContent(str));
                else
                    menu.AddItem(new GUIContent(str), false, AddKeysAtCurrentTime, curves);

                str = k_DeleteKey;
                if (noneHaveKeys || !enabled)
                    menu.AddDisabledItem(new GUIContent(str));
                else
                    menu.AddItem(new GUIContent(str), false, DeleteKeysAtCurrentTime, curves);
            }

            return menu;
        }

        private void AddKeysAtCurrentTime(object obj) { AddKeysAtCurrentTime((List<TimelineWindowCurve>)obj); }
        private void AddKeysAtCurrentTime(List<TimelineWindowCurve> curves)
        {
            TimelineWindowUtility.AddKeyframes(state, curves, state.time);
        }

        private void DeleteKeysAtCurrentTime(object obj) { DeleteKeysAtCurrentTime((List<TimelineWindowCurve>)obj); }
        private void DeleteKeysAtCurrentTime(List<TimelineWindowCurve> curves)
        {
            TimelineWindowUtility.RemoveKeyframes(state, curves, state.time);
        }

        private void ChangeRotationInterpolation(System.Object interpolationMode)
        {
            RotationCurveInterpolation.Mode mode = (RotationCurveInterpolation.Mode)interpolationMode;

            TimelineWindowCurve[] activeCurves = state.activeCurves.ToArray();
            EditorCurveBinding[] curveBindings = new EditorCurveBinding[activeCurves.Length];

            for (int i = 0; i < activeCurves.Length; i++)
            {
                curveBindings[i] = activeCurves[i].binding;
            }

            RotationCurveInterpolation.SetInterpolation(state.activeAnimationClip, curveBindings, mode);
            MaintainTreeviewStateAfterRotationInterpolation(mode);
            state.hierarchyData.ReloadData();
        }

        private void RemoveCurvesFromSelectedNodes()
        {
            RemoveCurvesFromNodes(state.selectedHierarchyNodes);
        }

        private static void CopyCurve(object userdata)
        {
            var animationWindowCurve = userdata as TimelineWindowCurve;
            if (animationWindowCurve == null)
                return;
            var keyframes = new Keyframe[animationWindowCurve.m_Keyframes.Count];
            for (var i = 0; i < animationWindowCurve.m_Keyframes.Count; i++)
            {
                keyframes[i] = animationWindowCurve.m_Keyframes[i].ToKeyframe();
            }

            if (AnimationUtility.onCurveCopy != null)
            {
                AnimationUtility.onCurveCopy(keyframes);
            }
        }

        private void RemoveCurvesFromNodes(List<TimelineWindowHierarchyNode> nodes)
        {
            string undoLabel = k_RemoveCurve;
            state.SaveKeySelection(undoLabel);

            foreach (var node in nodes)
            {
                TimelineWindowHierarchyNode hierarchyNode = (TimelineWindowHierarchyNode)node;

                if (hierarchyNode.parent is TimelineWindowHierarchyPropertyGroupNode && hierarchyNode.binding != null && TimelineWindowUtility.ForceGrouping((EditorCurveBinding)hierarchyNode.binding))
                    hierarchyNode = (TimelineWindowHierarchyNode)hierarchyNode.parent;

                if (hierarchyNode.curves == null)
                    continue;

                List<TimelineWindowCurve> curves = null;

                // Property or propertygroup
                if (hierarchyNode is TimelineWindowHierarchyPropertyGroupNode || hierarchyNode is TimelineWindowHierarchyPropertyNode)
                    curves = TimelineWindowUtility.FilterCurves(hierarchyNode.curves.ToArray(), hierarchyNode.path, hierarchyNode.animatableObjectType, hierarchyNode.propertyName);
                else
                    curves = TimelineWindowUtility.FilterCurves(hierarchyNode.curves.ToArray(), hierarchyNode.path, hierarchyNode.animatableObjectType);

                foreach (TimelineWindowCurve animationWindowCurve in curves)
                    state.RemoveCurve(animationWindowCurve, undoLabel);
            }

            m_TreeView.ReloadData();

            state.controlInterface.ResampleAnimation();
        }

        private List<TimelineWindowCurve> GetCurvesAffectedByNodes(List<TimelineWindowHierarchyNode> nodes, bool includeLinkedCurves)
        {
            List<TimelineWindowCurve> curves = new List<TimelineWindowCurve>();
            foreach (var node in nodes)
            {
                TimelineWindowHierarchyNode hierarchyNode = node;

                if (hierarchyNode.parent is TimelineWindowHierarchyPropertyGroupNode && includeLinkedCurves)
                    hierarchyNode = (TimelineWindowHierarchyNode)hierarchyNode.parent;

                if (hierarchyNode.curves == null)
                    continue;

                if (hierarchyNode.curves.Length > 0)
                {
                    // Property or propertygroup
                    if (hierarchyNode is TimelineWindowHierarchyPropertyGroupNode || hierarchyNode is TimelineWindowHierarchyPropertyNode)
                        curves.AddRange(TimelineWindowUtility.FilterCurves(hierarchyNode.curves, hierarchyNode.path, hierarchyNode.animatableObjectType, hierarchyNode.propertyName));
                    else
                        curves.AddRange(TimelineWindowUtility.FilterCurves(hierarchyNode.curves, hierarchyNode.path, hierarchyNode.animatableObjectType));
                }
            }
            return curves.Distinct().ToList();
        }

        // Changing rotation interpolation will change the propertynames of the curves
        // Propertynames are used in treeview node IDs, so we need to anticipate the new IDs by injecting them into treeview state
        // This way treeview state (selection and expanding) will be preserved once the curve data is eventually reloaded
        private void MaintainTreeviewStateAfterRotationInterpolation(RotationCurveInterpolation.Mode newMode)
        {
            List<int> selectedInstaceIDs = state.hierarchyState.selectedIDs;
            List<int> expandedInstaceIDs = state.hierarchyState.expandedIDs;

            List<int> oldIDs = new List<int>();
            List<int> newIds = new List<int>();

            for (int i = 0; i < selectedInstaceIDs.Count; i++)
            {
                TimelineWindowHierarchyNode node = state.hierarchyData.FindItem(selectedInstaceIDs[i]) as TimelineWindowHierarchyNode;

                if (node != null && !node.propertyName.Equals(RotationCurveInterpolation.GetPrefixForInterpolation(newMode)))
                {
                    string oldPrefix = node.propertyName.Split('.')[0];
                    string newPropertyName = node.propertyName.Replace(oldPrefix, RotationCurveInterpolation.GetPrefixForInterpolation(newMode));

                    // old treeview node id
                    oldIDs.Add(selectedInstaceIDs[i]);
                    // and its new replacement
                    newIds.Add((node.path + node.animatableObjectType.Name + newPropertyName).GetHashCode());
                }
            }

            // Replace old IDs with new ones
            for (int i = 0; i < oldIDs.Count; i++)
            {
                if (selectedInstaceIDs.Contains(oldIDs[i]))
                {
                    int index = selectedInstaceIDs.IndexOf(oldIDs[i]);
                    selectedInstaceIDs[index] = newIds[i];
                }
                if (expandedInstaceIDs.Contains(oldIDs[i]))
                {
                    int index = expandedInstaceIDs.IndexOf(oldIDs[i]);
                    expandedInstaceIDs[index] = newIds[i];
                }
                if (state.hierarchyState.lastClickedID == oldIDs[i])
                    state.hierarchyState.lastClickedID = newIds[i];
            }

            state.hierarchyState.selectedIDs = new List<int>(selectedInstaceIDs);
            state.hierarchyState.expandedIDs = new List<int>(expandedInstaceIDs);
        }

        private RotationCurveInterpolation.Mode GetRotationInterpolationMode(EditorCurveBinding[] curves)
        {
            if (curves == null || curves.Length == 0)
                return RotationCurveInterpolation.Mode.Undefined;

            RotationCurveInterpolation.Mode mode = RotationCurveInterpolation.GetModeFromCurveData(curves[0]);
            for (int i = 1; i < curves.Length; i++)
            {
                RotationCurveInterpolation.Mode nextMode = RotationCurveInterpolation.GetModeFromCurveData(curves[i]);
                if (mode != nextMode)
                    return RotationCurveInterpolation.Mode.Undefined;
            }

            return mode;
        }

        // TODO: Make real styles, not this
        private void SetStyleTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.focused.textColor = color;
            style.active.textColor = color;
            style.hover.textColor = color;
        }

        public override void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
        {
            firstRowVisible = 0;
            lastRowVisible = m_TreeView.data.rowCount - 1;
        }

        public float GetNodeHeight(TimelineWindowHierarchyNode node)
        {
            if (node is TimelineWindowHierarchyAddButtonNode)
                return k_AddCurveButtonNodeHeight;

            TimelineWindowHierarchyState hierarchyState = m_TreeView.state as TimelineWindowHierarchyState;
            return hierarchyState.GetTallMode(node) ? k_DopeSheetRowHeightTall : k_DopeSheetRowHeight;
        }

        public override Vector2 GetTotalSize()
        {
            var rows = m_TreeView.data.GetRows();
            float height = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                TimelineWindowHierarchyNode node = rows[i] as TimelineWindowHierarchyNode;
                height += GetNodeHeight(node);
            }

            return new Vector2(1, height);
        }

        float GetTopPixelOfRow(int row, IList<TreeViewItem> rows)
        {
            float top = 0f;
            for (int i = 0; i < row && i < rows.Count; i++)
            {
                TimelineWindowHierarchyNode node = rows[i] as TimelineWindowHierarchyNode;
                top += GetNodeHeight(node);
            }
            return top;
        }

        public override Rect GetRowRect(int row, float rowWidth)
        {
            var rows = m_TreeView.data.GetRows();
            TimelineWindowHierarchyNode hierarchyNode = rows[row] as TimelineWindowHierarchyNode;
            if (hierarchyNode.topPixel == null)
                hierarchyNode.topPixel = GetTopPixelOfRow(row, rows);

            float rowHeight = GetNodeHeight(hierarchyNode);
            return new Rect(0, (float)hierarchyNode.topPixel, rowWidth, rowHeight);
        }

        public override void OnRowGUI(Rect rowRect, TreeViewItem node, int row, bool selected, bool focused)
        {
            TimelineWindowHierarchyNode hierarchyNode = node as TimelineWindowHierarchyNode;
            DoNodeGUI(rowRect, hierarchyNode, selected, focused, row);
        }

        override public bool BeginRename(TreeViewItem item, float delay)
        {
            m_RenamedNode = item as TimelineWindowHierarchyNode;

            return GetRenameOverlay().BeginRename(m_RenamedNode.path, item.id, delay);
        }

        override protected void SyncFakeItem()
        {
            //base.SyncFakeItem();
        }

        override protected void RenameEnded()
        {
            var renameOverlay = GetRenameOverlay();
            if (renameOverlay.userAcceptedRename)
            {
                string newName = renameOverlay.name;
                string oldName = renameOverlay.originalName;

                if (newName != oldName)
                {
                    Undo.RegisterCompleteObjectUndo(state.activeAnimationClip, "Rename Curve");

                    foreach (TimelineWindowCurve curve in m_RenamedNode.curves)
                    {
                        EditorCurveBinding newBinding = TimelineWindowUtility.GetRenamedBinding(curve.binding, newName);

                        if (TimelineWindowUtility.CurveExists(newBinding, state.allCurves.ToArray()))
                        {
                            Debug.LogWarning("Curve already exists, renaming cancelled.");
                            continue;
                        }

                        TimelineWindowUtility.RenameCurvePath(curve, newBinding, curve.clip);
                    }
                }
            }

            m_RenamedNode = null;
        }

        override protected Texture GetIconForItem(TreeViewItem item)
        {
            if (item != null)
                return item.icon;

            return null;
        }
    }
}
