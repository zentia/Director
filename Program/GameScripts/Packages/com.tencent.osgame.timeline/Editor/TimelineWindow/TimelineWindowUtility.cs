using System;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using TimelineRuntime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace TimelineEditorInternal
{
    static internal class TimelineWindowUtility
    {
        public static void SaveCurve(AnimationClip clip, TimelineWindowCurve curve)
        {
            if (!curve.animationIsEditable)
                throw new ArgumentException("Curve is not editable and shouldn't be saved.");

            if (curve.isPPtrCurve)
            {
                ObjectReferenceKeyframe[] objectCurve = curve.ToObjectCurve();

                if (objectCurve.Length == 0)
                    objectCurve = null;

                AnimationUtility.SetObjectReferenceCurve(clip, curve.binding, objectCurve);
            }
            else
            {
                AnimationCurve animationCurve = curve.ToAnimationCurve();

                if (animationCurve.keys.Length == 0)
                    animationCurve = null;
                else
                    AnimationUtility.UpdateTangentsFromMode(animationCurve);

                AnimationUtility.SetEditorCurve(clip, curve.binding, animationCurve);
            }
        }

        public static void SaveCurves(AnimationClip clip, IEnumerable<TimelineWindowCurve> curves)
        {
            foreach (TimelineWindowCurve curve in curves)
            {
                if (!curve.animationIsEditable)
                    throw new ArgumentException("Curve is not editable and shouldn't be saved.");

                if (curve.isPPtrCurve)
                {
                    ObjectReferenceKeyframe[] objectCurve = curve.ToObjectCurve();

                    if (objectCurve.Length == 0)
                        objectCurve = null;

                    AnimationUtility.SetObjectReferenceCurveNoSync(clip, curve.binding, objectCurve);
                }
                else
                {
                    AnimationCurve animationCurve = curve.ToAnimationCurve();

                    if (animationCurve.keys.Length == 0)
                        animationCurve = null;
                    else
                        AnimationUtility.UpdateTangentsFromMode(animationCurve);

                    AnimationUtility.SetEditorCurveNoSync(clip, curve.binding, animationCurve);
                }
            }

            AnimationUtility.SyncEditorCurves(clip);
        }

        public static void SaveCurves(AnimationClip clip, IList<EditorCurveBinding> bindings, IList<AnimationCurve> curves)
        {
            if (bindings.Count != curves.Count)
                throw new ArgumentException("bindings and curves array sizes do not match");

            for (int i = 0; i < bindings.Count; ++i)
            {
                AnimationUtility.SetEditorCurveNoSync(clip, bindings[i], curves[i]);
            }

            AnimationUtility.SyncEditorCurves(clip);
        }

        public static void CreateDefaultCurves(TimelineWindowState state, EditorCurveBinding[] properties)
        {
            CreateDefaultCurves(state, state.activeAnimationClip, properties);
        }

        public static void CreateDefaultCurves(TimelineWindowState state, AnimationClip animationClip, EditorCurveBinding[] properties)
        {
            properties = RotationCurveInterpolation.ConvertRotationPropertiesToDefaultInterpolation(animationClip, properties);

            if (properties.Length == 0)
                return;

            var curves = new List<TimelineWindowCurve>(properties.Length);
            foreach (EditorCurveBinding prop in properties)
                curves.Add(CreateDefaultCurve(state, animationClip, prop));

            state.SaveCurves(animationClip, curves);
        }

        public static TimelineWindowCurve CreateDefaultCurve(TimelineWindowState state, AnimationClip animationClip, EditorCurveBinding binding)
        {
            Type type = state.selection.GetEditorCurveValueType(binding);

            TimelineWindowCurve curve = new TimelineWindowCurve(animationClip, binding, type);

            object currentValue = CurveBindingUtility.GetCurrentValue(state.activeRootGameObject, binding);
            if (animationClip.length == 0.0F)
            {
                AddKeyframeToCurve(curve, currentValue, type, TimelineKeyTime.Time(0.0F, animationClip.frameRate));
            }
            else
            {
                AddKeyframeToCurve(curve, currentValue, type, TimelineKeyTime.Time(0.0F, animationClip.frameRate));
                AddKeyframeToCurve(curve, currentValue, type, TimelineKeyTime.Time(animationClip.length, animationClip.frameRate));
            }

            return curve;
        }

        public static bool ShouldShowAnimationWindowCurve(EditorCurveBinding curveBinding)
        {
            // We don't want to convert the w component of rotation curves to be shown in animation window
            if (IsTransformType(curveBinding.type))
                return !curveBinding.propertyName.EndsWith(".w");

            return true;
        }

        public static bool IsNodeLeftOverCurve(TimelineWindowHierarchyNode node)
        {
            if (node.binding != null)
            {
                if (node.curves.Length > 0)
                {
                    TimelineWindowSelectionItem selectionBinding = node.curves[0].selectionBinding;
                    if (selectionBinding != null)
                    {
                        if (selectionBinding.rootGameObject == null && selectionBinding.scriptableObject == null)
                            return false;

                        return selectionBinding.GetEditorCurveValueType((EditorCurveBinding)node.binding) == null;
                    }
                }
            }

            // Go through all child nodes recursively
            if (node.hasChildren)
            {
                foreach (var child in node.children)
                    return IsNodeLeftOverCurve(child as TimelineWindowHierarchyNode);
            }

            return false;
        }

        public static bool IsNodeAmbiguous(TimelineWindowHierarchyNode node)
        {
            if (node.binding != null)
            {
                if (node.curves.Length > 0)
                {
                    TimelineWindowSelectionItem selectionBinding = node.curves[0].selectionBinding;
                    if (selectionBinding != null)
                    {
                        if (selectionBinding.rootGameObject != null)
                            return AnimationUtility.AmbiguousBinding(node.binding.Value.path, node.binding.Value.m_ClassID, selectionBinding.rootGameObject.transform);
                    }
                }
            }

            // Go through all child nodes recursively
            if (node.hasChildren)
            {
                foreach (var child in node.children)
                    return IsNodeAmbiguous(child as TimelineWindowHierarchyNode);
            }

            return false;
        }

        public static bool IsNodePhantom(TimelineWindowHierarchyNode node)
        {
            if (node.binding != null)
                return node.binding.Value.isPhantom;

            return false;
        }

        public static void AddSelectedKeyframes(TimelineWindowState state, TimelineKeyTime time)
        {
            List<TimelineWindowCurve> curves = state.activeCurves.Count > 0 ? state.activeCurves : state.allCurves;
            AddKeyframes(state, curves, time);
        }

        public static void AddKeyframes(TimelineWindowState state, IList<TimelineWindowCurve> curves, TimelineKeyTime time)
        {
            string undoLabel = L10n.Tr("Add Key");
            state.SaveKeySelection(undoLabel);

            state.ClearKeySelections();

            foreach (TimelineWindowCurve curve in curves)
            {
                if (!curve.animationIsEditable)
                    continue;

                TimelineKeyTime shiftedMouseKeyTime = TimelineKeyTime.Time(time.time, time.frameRate);

                object value = CurveBindingUtility.GetCurrentValue(state, curve);
                TimelineWindowKeyframe keyframe = TimelineWindowUtility.AddKeyframeToCurve(curve, value, curve.valueType, shiftedMouseKeyTime);

                if (keyframe != null)
                    state.SelectKey(keyframe);
            }

            state.SaveCurves(state.activeAnimationClip, curves, undoLabel);
        }

        public static void RemoveKeyframes(TimelineWindowState state, IList<TimelineWindowCurve> curves, TimelineKeyTime time)
        {
            string undoLabel = L10n.Tr("Remove Key");
            state.SaveKeySelection(undoLabel);

            foreach (TimelineWindowCurve curve in curves)
            {
                if (!curve.animationIsEditable)
                    continue;

                curve.RemoveKeyframe(time);
            }

            state.SaveCurves(state.activeAnimationClip, curves, undoLabel);
        }

        public static TimelineWindowKeyframe AddKeyframeToCurve(TimelineWindowCurve curve, object value, Type type, TimelineKeyTime time)
        {
            // When there is already a key a this time
            // Make sure that only value is updated but tangents are maintained.
            TimelineWindowKeyframe previousKey = curve.FindKeyAtTime(time);
            if (previousKey != null)
            {
                previousKey.value = value;
                return previousKey;
            }

            TimelineWindowKeyframe keyframe = null;

            if (curve.isPPtrCurve)
            {
                keyframe = new TimelineWindowKeyframe();

                keyframe.time = time.time;
                keyframe.value = value;
                keyframe.curve = curve;
                curve.AddKeyframe(keyframe, time);
            }
            else if (curve.isDiscreteCurve)
            {
                Keyframe tempKey = new Keyframe(time.time, 0f);
                AnimationUtility.SetKeyLeftTangentMode(ref tempKey, TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(ref tempKey, TangentMode.Constant);
                AnimationUtility.SetKeyBroken(ref tempKey, true);

                keyframe = new TimelineWindowKeyframe(curve, tempKey);
                keyframe.value = Convert.ToInt32(value);

                curve.AddKeyframe(keyframe, time);
            }
            else if (type == typeof(bool) || type == typeof(float) || type == typeof(int))
            {
                Keyframe tempKey = new Keyframe(time.time, (float)value);
                if (type == typeof(bool))
                {
                    AnimationUtility.SetKeyLeftTangentMode(ref tempKey, TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(ref tempKey, TangentMode.Constant);

                    AnimationUtility.SetKeyBroken(ref tempKey, true);
                }
                else if (type == typeof(int))
                {
                    // Create temporary curve to get proper tangents
                    AnimationCurve animationCurve = curve.ToAnimationCurve();

                    if (animationCurve.length <= 1)
                    {
                        TangentMode tangentMode = TangentMode.Linear;
                        AnimationUtility.SetKeyLeftTangentMode(ref tempKey, tangentMode);
                        AnimationUtility.SetKeyRightTangentMode(ref tempKey, tangentMode);
                    }
                    else
                    {
                        int keyIndex = animationCurve.AddKey(tempKey);
                        if (keyIndex != -1)
                        {
                            CurveUtility.SetKeyModeFromContext(animationCurve, keyIndex);
                            tempKey = animationCurve[keyIndex];
                        }
                    }

                    AnimationUtility.SetKeyBroken(ref tempKey, true);
                }
                else
                {
                    // Create temporary curve to get proper tangents
                    AnimationCurve animationCurve = curve.ToAnimationCurve();

                    int keyIndex = animationCurve.AddKey(tempKey);
                    if (keyIndex != -1)
                    {
                        // Make sure tangent slopes default to ClampedAuto.  Tangent mode will be modified afterwards.
                        AnimationUtility.SetKeyLeftTangentMode(animationCurve, keyIndex, TangentMode.ClampedAuto);
                        AnimationUtility.SetKeyRightTangentMode(animationCurve, keyIndex, TangentMode.ClampedAuto);
                        AnimationUtility.UpdateTangentsFromModeSurrounding(animationCurve, keyIndex);

                        CurveUtility.SetKeyModeFromContext(animationCurve, keyIndex);

                        tempKey = animationCurve[keyIndex];
                    }
                }

                keyframe = new TimelineWindowKeyframe(curve, tempKey);
                curve.AddKeyframe(keyframe, time);
            }

            return keyframe;
        }

        public static List<TimelineWindowCurve> FilterCurves(TimelineWindowCurve[] curves, string path, bool entireHierarchy)
        {
            List<TimelineWindowCurve> results = new List<TimelineWindowCurve>();

            if (curves != null)
            {
                foreach (TimelineWindowCurve curve in curves)
                    if (curve.path.Equals(path) || (entireHierarchy && curve.path.Contains(path)))
                        results.Add(curve);
            }

            return results;
        }

        public static List<TimelineWindowCurve> FilterCurves(TimelineWindowCurve[] curves, string path, Type animatableObjectType)
        {
            List<TimelineWindowCurve> results = new List<TimelineWindowCurve>();

            if (curves != null)
            {
                foreach (TimelineWindowCurve curve in curves)
                    if (curve.path.Equals(path) && curve.type == animatableObjectType)
                        results.Add(curve);
            }

            return results;
        }

        public static bool IsCurveCreated(AnimationClip clip, EditorCurveBinding binding)
        {
            if (binding.isPPtrCurve)
            {
                return AnimationUtility.GetObjectReferenceCurve(clip, binding) != null;
            }
            else
            {
                // For RectTransform.position we only want .z
                if (IsRectTransformPosition(binding))
                    binding.propertyName = binding.propertyName.Replace(".x", ".z").Replace(".y", ".z");
                if (IsRotationCurve(binding))
                {
                    return AnimationUtility.GetEditorCurve(clip, binding) != null || HasOtherRotationCurve(clip, binding);
                }

                return AnimationUtility.GetEditorCurve(clip, binding) != null;
            }
        }

        internal static bool HasOtherRotationCurve(AnimationClip clip, EditorCurveBinding rotationBinding)
        {
            if (rotationBinding.propertyName.StartsWith("m_LocalRotation"))
            {
                EditorCurveBinding x = rotationBinding;
                EditorCurveBinding y = rotationBinding;
                EditorCurveBinding z = rotationBinding;
                x.propertyName = "localEulerAnglesRaw.x";
                y.propertyName = "localEulerAnglesRaw.y";
                z.propertyName = "localEulerAnglesRaw.z";

                return AnimationUtility.GetEditorCurve(clip, x) != null ||
                    AnimationUtility.GetEditorCurve(clip, y) != null ||
                    AnimationUtility.GetEditorCurve(clip, z) != null;
            }
            else
            {
                EditorCurveBinding x = rotationBinding;
                EditorCurveBinding y = rotationBinding;
                EditorCurveBinding z = rotationBinding;
                EditorCurveBinding w = rotationBinding;

                x.propertyName = "m_LocalRotation.x";
                y.propertyName = "m_LocalRotation.y";
                z.propertyName = "m_LocalRotation.z";
                w.propertyName = "m_LocalRotation.w";

                return AnimationUtility.GetEditorCurve(clip, x) != null ||
                    AnimationUtility.GetEditorCurve(clip, y) != null ||
                    AnimationUtility.GetEditorCurve(clip, z) != null ||
                    AnimationUtility.GetEditorCurve(clip, w) != null;
            }
        }

        internal static bool IsRotationCurve(EditorCurveBinding curveBinding)
        {
            string propertyName = GetPropertyGroupName(curveBinding.propertyName);
            return propertyName == "m_LocalRotation" || propertyName == "localEulerAnglesRaw";
        }

        public static bool IsRectTransformPosition(EditorCurveBinding curveBinding)
        {
            return curveBinding.type == typeof(RectTransform) && GetPropertyGroupName(curveBinding.propertyName) == "m_LocalPosition";
        }

        public static bool ContainsFloatKeyframes(List<TimelineWindowKeyframe> keyframes)
        {
            if (keyframes == null || keyframes.Count == 0)
                return false;

            foreach (var key in keyframes)
            {
                if (!key.isPPtrCurve)
                    return true;
            }

            return false;
        }

        // Get curves for property or propertygroup (example: x,y,z)
        public static List<TimelineWindowCurve> FilterCurves(TimelineWindowCurve[] curves, string path, Type animatableObjectType, string propertyName)
        {
            List<TimelineWindowCurve> results = new List<TimelineWindowCurve>();

            if (curves != null)
            {
                string propertyGroupName = GetPropertyGroupName(propertyName);
                bool isPropertyGroup = propertyGroupName == propertyName;

                foreach (TimelineWindowCurve curve in curves)
                {
                    bool propertyNameMatches = isPropertyGroup ? GetPropertyGroupName(curve.propertyName).Equals(propertyGroupName) : curve.propertyName.Equals(propertyName);
                    if (curve.path.Equals(path) && curve.type == animatableObjectType && propertyNameMatches)
                        results.Add(curve);
                }
            }

            return results;
        }

        // Current value of the property that rootGO + curveBinding is pointing to
        public static object GetCurrentValue(GameObject rootGameObject, EditorCurveBinding curveBinding)
        {
            if (curveBinding.isPPtrCurve)
            {
                AnimationUtility.GetObjectReferenceValue(rootGameObject, curveBinding, out var value);
                return value;
            }
            else if (curveBinding.isDiscreteCurve)
            {
                AnimationUtility.GetDiscreteIntValue(rootGameObject, curveBinding, out var value);
                return value;
            }
            else
            {
                AnimationUtility.GetFloatValue(rootGameObject, curveBinding, out var value);
                return value;
            }
        }

        public static List<EditorCurveBinding> GetAnimatableProperties(GameObject gameObject, GameObject root, Type valueType)
        {
            EditorCurveBinding[] animatable = AnimationUtility.GetAnimatableBindings(gameObject, root);

            List<EditorCurveBinding> result = new List<EditorCurveBinding>();
            foreach (EditorCurveBinding binding in animatable)
                if (AnimationUtility.GetEditorCurveValueType(root, binding) == valueType)
                    result.Add(binding);

            return result;
        }

        public static List<EditorCurveBinding> GetAnimatableProperties(GameObject gameObject, GameObject root, Type objectType, Type valueType)
        {
            EditorCurveBinding[] animatable = AnimationUtility.GetAnimatableBindings(gameObject, root);

            List<EditorCurveBinding> result = new List<EditorCurveBinding>();
            foreach (EditorCurveBinding binding in animatable)
                if (binding.type == objectType && AnimationUtility.GetEditorCurveValueType(root, binding) == valueType)
                    result.Add(binding);

            return result;
        }

        public static List<EditorCurveBinding> GetAnimatableProperties(ScriptableObject scriptableObject, Type valueType)
        {
            EditorCurveBinding[] animatable = AnimationUtility.GetAnimatableBindings(scriptableObject);

            List<EditorCurveBinding> result = new List<EditorCurveBinding>();
            foreach (EditorCurveBinding binding in animatable)
                if (AnimationUtility.GetEditorCurveValueType(scriptableObject, binding) == valueType)
                    result.Add(binding);

            return result;
        }

        public static bool PropertyIsAnimatable(Object targetObject, string propertyPath, Object rootObject)
        {
            if (targetObject is ScriptableObject)
            {
                ScriptableObject scriptableObject = (ScriptableObject)targetObject;
                EditorCurveBinding[] allCurveBindings = AnimationUtility.GetAnimatableBindings(scriptableObject);
                return Array.Exists(allCurveBindings, binding => binding.propertyName == propertyPath);
            }
            else
            {
                GameObject gameObject = targetObject as GameObject;
                if (targetObject is Component)
                    gameObject = ((Component)targetObject).gameObject;

                if (gameObject != null)
                {
                    var dummyModification = new PropertyModification();
                    dummyModification.propertyPath = propertyPath;
                    dummyModification.target = targetObject;

                    EditorCurveBinding binding = new EditorCurveBinding();
                    return AnimationUtility.PropertyModificationToEditorCurveBinding(dummyModification, rootObject == null ? gameObject : (GameObject)rootObject, out binding) != null;
                }
            }

            return false;
        }

        // Given a serialized property, gathers all animateable properties
        public static PropertyModification[] SerializedPropertyToPropertyModifications(SerializedProperty property)
        {
            List<SerializedProperty> properties = new List<SerializedProperty>();

            properties.Add(property);

            // handles child properties (Vector3 is 3 recordable properties)
            if (property.hasChildren)
            {
                var iter = property.Copy();
                var end = property.GetEndProperty(false);

                // recurse over all children properties
                while (iter.Next(true) && !SerializedProperty.EqualContents(iter, end) && iter.propertyPath.StartsWith(property.propertyPath))
                {
                    properties.Add(iter.Copy());
                }
            }

            // Special case for m_LocalRotation...
            if (property.propertyPath.StartsWith("m_LocalRotation"))
            {
                var serializedObject = property.serializedObject;
                if (serializedObject.targetObject is Transform)
                {
                    SerializedProperty eulerHintProperty = serializedObject.FindProperty("m_LocalEulerAnglesHint");
                    if (eulerHintProperty != null && eulerHintProperty.hasChildren)
                    {
                        var iter = eulerHintProperty.Copy();
                        var end = eulerHintProperty.GetEndProperty(false);

                        // recurse over all children properties
                        while (iter.Next(true) && !SerializedProperty.EqualContents(iter, end) && iter.propertyPath.StartsWith(eulerHintProperty.propertyPath))
                        {
                            properties.Add(iter.Copy());
                        }
                    }
                }
            }

            List<PropertyModification> modifications = new List<PropertyModification>();

            for (int i = 0; i < properties.Count; ++i)
            {
                var propertyIter = properties[i];
                var isObject = propertyIter.propertyType == SerializedPropertyType.ObjectReference;
                var isFloat = propertyIter.propertyType == SerializedPropertyType.Float;
                var isBool = propertyIter.propertyType == SerializedPropertyType.Boolean;
                var isInt = propertyIter.propertyType == SerializedPropertyType.Integer;
                var isEnum = propertyIter.propertyType == SerializedPropertyType.Enum;

                if (isObject || isFloat || isBool || isInt || isEnum)
                {
                    var serializedObject = propertyIter.serializedObject;
                    var targetObjects = serializedObject.targetObjects;

                    if (propertyIter.hasMultipleDifferentValues)
                    {
                        for (int j = 0; j < targetObjects.Length; ++j)
                        {
                            var singleObject = new SerializedObject(targetObjects[j]);
                            SerializedProperty singleProperty = singleObject.FindProperty(propertyIter.propertyPath);

                            string value = string.Empty;
                            Object objectReference = null;

                            if (isObject)
                                objectReference = singleProperty.objectReferenceValue;
                            else if (isFloat)
                                value = singleProperty.floatValue.ToString(CultureInfo.InvariantCulture);
                            else if (isInt)
                                value = singleProperty.intValue.ToString();
                            else if (isEnum)
                                value = singleProperty.enumValueIndex.ToString();
                            else // if (isBool)
                                value = singleProperty.boolValue ? "1" : "0";

                            var modification = new PropertyModification();

                            modification.target = targetObjects[j];
                            modification.propertyPath = (singleProperty.isReferencingAManagedReferenceField ? singleProperty.managedReferencePropertyPath : singleProperty.propertyPath);
                            modification.value = value;
                            modification.objectReference = objectReference;
                            modifications.Add(modification);
                        }
                    }
                    // fast path
                    else
                    {
                        string value = string.Empty;
                        Object objectReference = null;

                        if (isObject)
                            objectReference = propertyIter.objectReferenceValue;
                        else if (isFloat)
                            value = propertyIter.floatValue.ToString(CultureInfo.InvariantCulture);
                        else if (isInt)
                            value = propertyIter.intValue.ToString();
                        else if (isEnum)
                                value = propertyIter.enumValueIndex.ToString();
                        else // if (isBool)
                            value = propertyIter.boolValue ? "1" : "0";

                        for (int j = 0; j < targetObjects.Length; ++j)
                        {
                            var modification = new PropertyModification();

                            modification.target = targetObjects[j];
                            modification.propertyPath = (propertyIter.isReferencingAManagedReferenceField ? propertyIter.managedReferencePropertyPath : propertyIter.propertyPath);
                            modification.value = value;
                            modification.objectReference = objectReference;
                            modifications.Add(modification);
                        }
                    }
                }
            }

            return modifications.ToArray();
        }

        public static EditorCurveBinding[] PropertyModificationsToEditorCurveBindings(PropertyModification[] modifications, GameObject rootGameObject, AnimationClip animationClip)
        {
            if (modifications == null)
                return new EditorCurveBinding[] {};

            var bindings = new HashSet<EditorCurveBinding>();

            for (int i = 0; i < modifications.Length; ++i)
            {
                var binding = new EditorCurveBinding();
                if (AnimationUtility.PropertyModificationToEditorCurveBinding(modifications[i], rootGameObject, out binding) != null)
                {
                    EditorCurveBinding[] additionalBindings = RotationCurveInterpolation.RemapAnimationBindingForAddKey(binding, animationClip);
                    if (additionalBindings != null)
                    {
                        for (int j = 0; j < additionalBindings.Length; ++j)
                        {
                            bindings.Add(additionalBindings[j]);
                        }
                    }
                    else
                    {
                        bindings.Add(binding);
                    }
                }
            }

            return bindings.ToArray();
        }

        public static EditorCurveBinding[] SerializedPropertyToEditorCurveBindings(SerializedProperty property, GameObject rootGameObject, AnimationClip animationClip)
        {
            PropertyModification[] modifications = TimelineWindowUtility.SerializedPropertyToPropertyModifications(property);
            return PropertyModificationsToEditorCurveBindings(modifications, rootGameObject, animationClip);
        }

        public static bool CurveExists(EditorCurveBinding binding, TimelineWindowCurve[] curves)
        {
            foreach (var animationWindowCurve in curves)
            {
                if (binding.propertyName == animationWindowCurve.binding.propertyName &&
                    binding.type == animationWindowCurve.binding.type &&
                    binding.path == animationWindowCurve.binding.path)
                    return true;
            }
            return false;
        }

        public static EditorCurveBinding GetRenamedBinding(EditorCurveBinding binding, string newPath)
        {
            EditorCurveBinding newBinding = new EditorCurveBinding();
            newBinding.path = newPath;
            newBinding.propertyName = binding.propertyName;
            newBinding.type = binding.type;
            return newBinding;
        }

        public static void RenameCurvePath(TimelineWindowCurve curve, EditorCurveBinding newBinding, AnimationClip clip)
        {
            if (curve.isPPtrCurve)
            {
                // Delete old curve
                AnimationUtility.SetObjectReferenceCurve(clip, curve.binding, null);

                // Add new curve
                AnimationUtility.SetObjectReferenceCurve(clip, newBinding, curve.ToObjectCurve());
            }
            else
            {
                // Delete old curve
                AnimationUtility.SetEditorCurve(clip, curve.binding, null);

                // Add new curve
                AnimationUtility.SetEditorCurve(clip, newBinding, curve.ToAnimationCurve());
            }
        }

        private static readonly string k_PositionDisplayName = L10n.Tr("Position");
        private static readonly string k_ScaleDisplayName = L10n.Tr("Scale");
        private static readonly string k_RotationDisplayName = L10n.Tr("Rotation");
        private static readonly string k_MaterialReferenceDisplayName = L10n.Tr("Material Reference");

        // Takes raw animation curve propertyname and makes it pretty
        public static string GetPropertyDisplayName(string propertyName)
        {
            propertyName = propertyName.Replace("m_LocalPosition", k_PositionDisplayName);
            propertyName = propertyName.Replace("m_LocalScale", k_ScaleDisplayName);
            propertyName = propertyName.Replace("m_LocalRotation", k_RotationDisplayName);
            propertyName = propertyName.Replace("localEulerAnglesBaked", k_RotationDisplayName);
            propertyName = propertyName.Replace("localEulerAnglesRaw", k_RotationDisplayName);
            propertyName = propertyName.Replace("localEulerAngles", k_RotationDisplayName);
            propertyName = propertyName.Replace("m_Materials.Array.data", k_MaterialReferenceDisplayName);
            if (propertyName.StartsWith("managedReferences["))
                propertyName = propertyName.Remove(0, propertyName.IndexOf('.')+1);

            propertyName = ObjectNames.NicifyVariableName(propertyName);
            propertyName = propertyName.Replace("m_", "");

            return propertyName;
        }

        // Transform and Sprite: just show Position / Rotation / Scale / Sprite
        public static bool ShouldPrefixWithTypeName(Type animatableObjectType, string propertyName)
        {
            if (animatableObjectType == typeof(Transform) || animatableObjectType == typeof(RectTransform))
                return false;

            if (animatableObjectType == typeof(SpriteRenderer) && propertyName == "m_Sprite")
                return false;

            return true;
        }

        public static string GetNicePropertyDisplayName(EditorCurveBinding curveBinding, SerializedObject so)
        {
            if (curveBinding.isSerializeReferenceCurve)
            {
                if (so != null)
                {
                    var displayName = curveBinding.propertyName;
                    var sp = so.FindFirstPropertyFromManagedReferencePath(displayName);
                    if (sp != null)
                        displayName = TimelineWindowUtility.GetPropertyDisplayName(TimelineWindowUtility.GetPropertyGroupName(sp.propertyPath));
                    if (displayName != "")
                        return displayName;
                }
                else
                {
                    return ObjectNames.NicifyVariableName(curveBinding.type.Name) + "." + curveBinding.propertyName;
                }

            }

            return TimelineWindowUtility.GetNicePropertyDisplayName(curveBinding.type, TimelineWindowUtility.GetPropertyGroupName(curveBinding.propertyName));
        }

        public static string GetNicePropertyDisplayName(Type animatableObjectType, string propertyName)
        {
            if (ShouldPrefixWithTypeName(animatableObjectType, propertyName))
                return ObjectNames.NicifyVariableName(animatableObjectType.Name) + "." + GetPropertyDisplayName(propertyName);
            else
                return GetPropertyDisplayName(propertyName);
        }

        public static string GetNicePropertyGroupDisplayName(EditorCurveBinding curveBinding, SerializedObject so)
        {
            if (curveBinding.isSerializeReferenceCurve )
            {
                if (so != null)
                {
                    var displayName = curveBinding.propertyName;
                    var sp = so.FindFirstPropertyFromManagedReferencePath(displayName);
                    if (sp != null)
                        displayName = TimelineWindowUtility.GetPropertyDisplayName(TimelineWindowUtility.GetPropertyGroupName(sp.propertyPath));
                    if (displayName != "")
                        return displayName;
                }
                else
                {
                    return ObjectNames.NicifyVariableName(curveBinding.type.Name) + "." + curveBinding.propertyName;
                }
            }

            return NicifyPropertyGroupName(curveBinding.type, TimelineWindowUtility.GetPropertyGroupName(curveBinding.propertyName));
        }

        public static string GetNicePropertyGroupDisplayName(Type animatableObjectType, string propertyGroupName)
        {
            if (ShouldPrefixWithTypeName(animatableObjectType, propertyGroupName))
                return ObjectNames.NicifyVariableName(animatableObjectType.Name) + "." + NicifyPropertyGroupName(animatableObjectType, propertyGroupName);
            else
                return NicifyPropertyGroupName(animatableObjectType, propertyGroupName);
        }

        // Takes raw animation curve propertyname and returns a pretty groupname
        public static string NicifyPropertyGroupName(Type animatableObjectType, string propertyGroupName)
        {
            string result = GetPropertyGroupName(GetPropertyDisplayName(propertyGroupName));

            // Workaround for uGUI RectTransform which only animates position.z
            if (animatableObjectType == typeof(RectTransform) && result.Equals("Position"))
                result = "Position (Z)";

            return result;
        }

        // We automatically group Vector4, Vector3 and Color
        static public int GetComponentIndex(string name)
        {
            if (name == null || name.Length < 3 || name[name.Length - 2] != '.')
                return -1;
            char lastCharacter = name[name.Length - 1];
            switch (lastCharacter)
            {
                case 'r':
                    return 0;
                case 'g':
                    return 1;
                case 'b':
                    return 2;
                case 'a':
                    return 3;
                case 'x':
                    return 0;
                case 'y':
                    return 1;
                case 'z':
                    return 2;
                case 'w':
                    return 3;
                default:
                    return -1;
            }
        }

        // If Vector4, Vector3 or Color, return group name instead of full name
        public static string GetPropertyGroupName(string propertyName)
        {
            if (GetComponentIndex(propertyName) != -1)
                return propertyName.Substring(0, propertyName.Length - 2);

            return propertyName;
        }

        public static float GetNextKeyframeTime(TimelineWindowCurve[] curves, float currentTime, float frameRate)
        {
            TimelineKeyTime candidateKeyTime = TimelineKeyTime.Frame(int.MaxValue, frameRate);
            TimelineKeyTime time = TimelineKeyTime.Time(currentTime, frameRate);
            TimelineKeyTime nextTime = TimelineKeyTime.Frame(time.frame + 1, frameRate);
            bool found = false;

            foreach (TimelineWindowCurve curve in curves)
            {
                foreach (TimelineWindowKeyframe keyframe in curve.m_Keyframes)
                {
                    TimelineKeyTime keyTime = TimelineKeyTime.Time(keyframe.time, frameRate);
                    if (keyTime.frame <= candidateKeyTime.frame && keyTime.frame >= nextTime.frame)
                    {
                        if (keyframe.time <= candidateKeyTime.time)
                        {
                            candidateKeyTime = keyTime;
                            found = true;
                        }
                    }
                }
            }
            return found ? candidateKeyTime.time : time.time;
        }

        public static float GetPreviousKeyframeTime(TimelineWindowCurve[] curves, float currentTime, float frameRate)
        {
            TimelineKeyTime candidateKeyTime = TimelineKeyTime.Time(float.MinValue, frameRate);
            TimelineKeyTime time = TimelineKeyTime.Time(currentTime, frameRate);
            TimelineKeyTime previousTime = TimelineKeyTime.Frame(time.frame - 1, frameRate);

            bool found = false;

            foreach (TimelineWindowCurve curve in curves)
            {
                foreach (TimelineWindowKeyframe keyframe in curve.m_Keyframes)
                {
                    TimelineKeyTime keyTime = TimelineKeyTime.Time(keyframe.time, frameRate);
                    if (keyTime.frame >= candidateKeyTime.frame && keyTime.frame <= previousTime.frame)
                    {
                        if (keyTime.time >= candidateKeyTime.time)
                        {
                            candidateKeyTime = keyTime;
                            found = true;
                        }
                    }
                }
            }

            return found ? candidateKeyTime.time : time.time;
        }

        // Add animator, controller and clip to gameobject if they are missing to make this gameobject animatable
        // public static bool InitializeGameobjectForAnimation(GameObject animatedObject)
        // {
        //     Component animationPlayer = GetClosestAnimationPlayerComponentInParents(animatedObject.transform);
        //     if (animationPlayer == null)
        //     {
        //         var newClip = CreateNewClip(animatedObject.name);
        //
        //         if (newClip == null)
        //             return false;
        //
        //         animationPlayer = EnsureActiveAnimationPlayer(animatedObject);
        //         Undo.RecordObject(animationPlayer, "Add animation clip");
        //         bool success = AddClipToAnimationPlayerComponent(animationPlayer, newClip);
        //
        //         if (!success)
        //             Object.DestroyImmediate(animationPlayer);
        //
        //         return success;
        //     }
        //
        //     return EnsureAnimationPlayerHasClip(animationPlayer);
        // }

        // Ensures that the gameobject or it's parents have an animation player component. If not try to create one.
        public static Component EnsureActiveAnimationPlayer(GameObject animatedObject)
        {
            Component closestAnimator = GetClosestAnimationPlayerComponentInParents(animatedObject.transform);
            if (closestAnimator == null)
            {
                return Undo.AddComponent<Animator>(animatedObject);
            }
            return closestAnimator;
        }

        // Ensures that animator has atleast one clip and controller to go with it
        private static bool EnsureAnimationPlayerHasClip(Component animationPlayer)
        {
            if (animationPlayer == null)
                return false;

            if (AnimationUtility.GetAnimationClips(animationPlayer.gameObject).Length > 0)
                return true;

            // At this point we know that we can create a clip
            var newClip = CreateNewClip(animationPlayer.gameObject.name);

            if (newClip == null)
                return false;

            // End animation mode before adding or changing animation component to object
            AnimationMode.StopAnimationMode();

            // By default add it the animation to the Animator component.
            return true;
        }

        public static bool AddClipToAnimationPlayerComponent(Component animationPlayer, AnimationClip newClip)
        {
            if (animationPlayer is Animator)
                return AddClipToAnimatorComponent(animationPlayer as Animator, newClip);
            else if (animationPlayer is Animation)
                return AddClipToAnimationComponent(animationPlayer as Animation, newClip);
            return false;
        }

        public static bool AddClipToAnimatorComponent(Animator animator, AnimationClip newClip)
        {
            UnityEditor.Animations.AnimatorController controller = UnityEditor.Animations.AnimatorController.GetEffectiveAnimatorController(animator);
            if (controller == null)
            {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerForClip(newClip, animator.gameObject);
                UnityEditor.Animations.AnimatorController.SetAnimatorController(animator, controller);

                if (controller != null)
                    return true;
            }
            else
            {
                // Do we already have a state with the clips name?
                ChildAnimatorState childAnimatorState = controller.layers[0].stateMachine.FindState(newClip.name);

                if (childAnimatorState.Equals(default(ChildAnimatorState)))
                    controller.AddMotion(newClip);

                // Assign clip if state already present, but without a motion
                else if (childAnimatorState.state && childAnimatorState.state.motion == null)
                    childAnimatorState.state.motion = newClip;

                // State present, but with some other clip
                else if (childAnimatorState.state && childAnimatorState.state.motion != newClip)
                    controller.AddMotion(newClip);

                return true;
            }
            return false;
        }

        public static bool AddClipToAnimationComponent(Animation animation, AnimationClip newClip)
        {
            SetClipAsLegacy(newClip);
            animation.AddClip(newClip, newClip.name);
            return true;
        }

        internal static string s_LastPathUsedForNewClip;
        internal static Timeline CreateNewClip(string gameObjectName)
        {
            // Go forward with presenting user a save clip dialog
            string message = string.Format(L10n.Tr("Create a new animation for the game object '{0}':"), gameObjectName);
            string newClipDirectory = ProjectWindowUtil.GetActiveFolderPath();
            if (s_LastPathUsedForNewClip != null)
            {
                string directoryPath = Path.GetDirectoryName(s_LastPathUsedForNewClip);
                if (directoryPath != null && Directory.Exists(directoryPath))
                {
                    newClipDirectory = directoryPath;
                }
            }
            string newClipPath = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create New Animation"), "New Timeline", "anim", message, newClipDirectory);

            // If user canceled or save path is invalid, we can't create a clip
            if (newClipPath == "")
                return null;

            return CreateNewClipAtPath(newClipPath);
        }

        // Create a new animation clip asset for gameObject at a certain asset path.
        // The clipPath parameter must be a full asset path ending with '.anim'. e.g. "Assets/Animations/New Clip.anim"
        // This function will overwrite existing .anim files.
        internal static Timeline CreateNewClipAtPath(string clipPath)
        {
            s_LastPathUsedForNewClip = clipPath;

            var newClip = new GameObject("Timeline",typeof(Timeline)).GetComponent<Timeline>();
            return newClip;
        }

        private static void SetClipAsLegacy(AnimationClip clip)
        {
            SerializedObject s = new SerializedObject(clip);
            s.FindProperty("m_Legacy").boolValue = true;
            s.ApplyModifiedProperties();
        }

        internal static AnimationClip AllocateAndSetupClip(bool useAnimator)
        {
            // At this point we know that we can create a clip
            AnimationClip newClip = new AnimationClip();
            if (useAnimator)
            {
                AnimationClipSettings info = AnimationUtility.GetAnimationClipSettings(newClip);
                info.loopTime = true;
                AnimationUtility.SetAnimationClipSettingsNoDirty(newClip, info);
            }
            return newClip;
        }

        public static int GetPropertyNodeID(int setId, string path, System.Type type, string propertyName)
        {
            return (setId + path + type.Name + propertyName).GetHashCode();
        }

        // What is the first animation player component (Animator or Animation) when recursing parent tree toward root
        public static Component GetClosestAnimationPlayerComponentInParents(Transform tr)
        {
            while (true)
            {
                if (tr.TryGetComponent(out Timeline animation))
                {
                    return animation;
                }

                if (tr == tr.root)
                    break;

                tr = tr.parent;
            }
            return null;
        }

        // What is the first animator component when recursing parent tree toward root
        public static Animator GetClosestAnimatorInParents(Transform tr)
        {
            while (true)
            {
                if (tr.TryGetComponent(out Animator animator))
                {
                    return animator;
                }
                if (tr == tr.root) break;
                tr = tr.parent;
            }
            return null;
        }

        // What is the first animation component when recursing parent tree toward root
        public static Animation GetClosestAnimationInParents(Transform tr)
        {
            while (true)
            {
                if (tr.TryGetComponent(out Animation animation))
                {
                    return animation;
                }
                if (tr == tr.root) break;
                tr = tr.parent;
            }
            return null;
        }

        public static void SyncTimeArea(TimeArea from, TimeArea to)
        {
            to.SetDrawRectHack(from.drawRect);
            to.m_Scale = new Vector2(from.m_Scale.x, to.m_Scale.y);
            to.m_Translation = new Vector2(from.m_Translation.x, to.m_Translation.y);
            to.EnforceScaleAndRange();
        }

        public static void DrawInRangeOverlay(Rect rect, Color color, float startOfClipPixel, float endOfClipPixel)
        {
            // Rect shaded shape drawn inside range
            if (endOfClipPixel >= rect.xMin)
            {
                if (color.a > 0f)
                {
                    Rect inRect = Rect.MinMaxRect(Mathf.Max(startOfClipPixel, rect.xMin), rect.yMin, Mathf.Min(endOfClipPixel, rect.xMax), rect.yMax);
                    DrawRect(inRect, color);
                }
            }
        }

        public static void DrawOutOfRangeOverlay(Rect rect, Color color, float startOfClipPixel, float endOfClipPixel)
        {
            Color lineColor = Color.white.RGBMultiplied(0.4f);

            // Rect shaded shape drawn before range
            if (startOfClipPixel > rect.xMin)
            {
                Rect startRect = Rect.MinMaxRect(rect.xMin, rect.yMin, Mathf.Min(startOfClipPixel, rect.xMax), rect.yMax);
                DrawRect(startRect, color);
                TimeArea.DrawVerticalLine(startRect.xMax, startRect.yMin, startRect.yMax, lineColor);
            }

            // Rect shaded shape drawn after range
            Rect endRect = Rect.MinMaxRect(Mathf.Max(endOfClipPixel, rect.xMin), rect.yMin, rect.xMax, rect.yMax);
            DrawRect(endRect, color);
            TimeArea.DrawVerticalLine(endRect.xMin, endRect.yMin, endRect.yMax, lineColor);
        }

        public static void DrawSelectionOverlay(Rect rect, Color color, float startPixel, float endPixel)
        {
            startPixel = Mathf.Max(startPixel, rect.xMin);
            endPixel = Mathf.Max(endPixel, rect.xMin);

            Rect labelRect = Rect.MinMaxRect(startPixel, rect.yMin, endPixel, rect.yMax);
            DrawRect(labelRect, color);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial();
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.QUADS);
            GL.Color(color);
            GL.Vertex(rect.min);
            GL.Vertex(new Vector2(rect.xMax, rect.yMin));
            GL.Vertex(rect.max);
            GL.Vertex(new Vector2(rect.xMin, rect.yMax));
            GL.End();
            GL.PopMatrix();
        }

        private static TimelineEditor.CurveRenderer CreateRendererForCurve(TimelineWindowCurve curve)
        {
            TimelineEditor.CurveRenderer renderer;
            switch (System.Type.GetTypeCode(curve.valueType))
            {
                case TypeCode.Int32:
                    renderer = new TimelineEditor.IntCurveRenderer(curve.ToAnimationCurve());
                    break;
                case TypeCode.Boolean:
                    renderer = new TimelineEditor.BoolCurveRenderer(curve.ToAnimationCurve());
                    break;
                default:
                    renderer = new TimelineEditor.NormalCurveRenderer(curve.ToAnimationCurve());
                    break;
            }
            return renderer;
        }

        private static TimelineEditor.CurveWrapper.PreProcessKeyMovement CreateKeyPreprocessorForCurve(TimelineWindowCurve curve)
        {
            TimelineEditor.CurveWrapper.PreProcessKeyMovement method;
            switch (System.Type.GetTypeCode(curve.valueType))
            {
                case TypeCode.Int32:
                    method = (ref Keyframe key) => { key.value = Mathf.Floor(key.value + 0.5f); };
                    break;
                case TypeCode.Boolean:
                    method = (ref Keyframe key) => { key.value = key.value > 0.5f ? 1.0f : 0.0f; };
                    break;
                default:
                    method = null;
                    break;
            }
            return method;
        }

        public static TimelineEditor.CurveWrapper GetCurveWrapper(TimelineWindowCurve curve, AnimationClip clip)
        {
            TimelineEditor.CurveWrapper curveWrapper = new TimelineEditor.CurveWrapper();
            curveWrapper.renderer = CreateRendererForCurve(curve);
            curveWrapper.preProcessKeyMovementDelegate = CreateKeyPreprocessorForCurve(curve);
            curveWrapper.renderer.SetWrap(WrapMode.Clamp, clip.isLooping ? WrapMode.Loop : WrapMode.Clamp);
            curveWrapper.renderer.SetCustomRange(clip.startTime, clip.stopTime);
            curveWrapper.binding = curve.binding;
            curveWrapper.id = curve.GetHashCode();
            curveWrapper.color = CurveUtility.GetPropertyColor(curve.propertyName);
            curveWrapper.hidden = false;
            curveWrapper.selectionBindingInterface = curve.selectionBinding;
            return curveWrapper;
        }

        // Convert keyframe from curve editor representation (CurveSelection) to animation window representation (AnimationWindowKeyframe)
        public static TimelineWindowKeyframe CurveSelectionToAnimationWindowKeyframe(TimelineEditor.CurveSelection curveSelection, List<TimelineWindowCurve> allCurves)
        {
            foreach (TimelineWindowCurve curve in allCurves)
            {
                int curveID = curve.GetHashCode();
                if (curveID == curveSelection.curveID)
                    if (curve.m_Keyframes.Count > curveSelection.key)
                        return curve.m_Keyframes[curveSelection.key];
            }

            return null;
        }

        // Convert keyframe from animation window representation (AnimationWindowKeyframe) to curve editor representation (CurveSelection) to animation window representation (AnimationWindowKeyframe)
        public static TimelineEditor.CurveSelection AnimationWindowKeyframeToCurveSelection(TimelineWindowKeyframe keyframe, TimelineEditor.CurveEditor curveEditor)
        {
            int curveID = keyframe.curve.GetHashCode();
            foreach (TimelineEditor.CurveWrapper curveWrapper in curveEditor.animationCurves)
                if (curveWrapper.id == curveID && keyframe.GetIndex() >= 0)
                    return new TimelineEditor.CurveSelection(curveWrapper.id, keyframe.GetIndex());

            return null;
        }

        public static TimelineWindowCurve BestMatchForPaste(EditorCurveBinding binding, List<TimelineWindowCurve> clipboardCurves, List<TimelineWindowCurve> targetCurves)
        {
            // Exact match
            foreach (TimelineWindowCurve targetCurve in targetCurves)
                if (targetCurve.binding == binding)
                    return targetCurve;

            // Matching propertyname
            foreach (TimelineWindowCurve targetCurve in targetCurves)
            {
                if (targetCurve.binding.propertyName == binding.propertyName)
                {
                    // Only match if key in binding is not already being pasted itself in clipboardCurves.
                    if (!clipboardCurves.Exists(clipboardCurve => clipboardCurve.binding == targetCurve.binding))
                    {
                        return targetCurve;
                    }
                }
            }

            // No good match found.
            return null;
        }

        // Make a rect from MinMax values and make sure they're positive sizes
        internal static Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }
            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }
            return r;
        }

        public static bool IsTransformType(Type type)
        {
            return type == typeof(Transform) || type == typeof(RectTransform);
        }

        public static bool IsActualTransformCurve(EditorCurveBinding curveBinding)
        {
            return curveBinding.type == typeof(Transform) || curveBinding.type == typeof(RectTransform) && (curveBinding.propertyName.StartsWith("m_LocalScale") ||
                curveBinding.propertyName.StartsWith("m_LocalRotation") ||
                curveBinding.propertyName.StartsWith("localEuler"));
        }

        public static bool ForceGrouping(EditorCurveBinding binding)
        {
            if (binding.type == typeof(Transform))
                return true;

            if (binding.type == typeof(RectTransform))
            {
                string group = GetPropertyGroupName(binding.propertyName);
                return group == "m_LocalPosition" || group == "m_LocalScale" || group == "m_LocalRotation" || group == "localEulerAnglesBaked" || group == "localEulerAngles" || group == "localEulerAnglesRaw";
            }

            if (typeof(Renderer).IsAssignableFrom(binding.type))
            {
                string group = GetPropertyGroupName(binding.propertyName);
                return group == "material._Color" || group == "material._BaseColor";
            }
            return false;
        }

        public static void ControllerChanged()
        {
            foreach (AnimationWindow animationWindow in AnimationWindow.GetAllAnimationWindows())
                animationWindow.OnControllerChange();
        }
    }
}
