using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using TimelineRuntime;
using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    internal class TimelineWindowSelectionItem : ScriptableObject, System.IEquatable<TimelineWindowSelectionItem>, TimelineEditor.ISelectionBinding
    {
        [SerializeField] private int m_Id;
        [SerializeField] private GameObject m_GameObject;
        [SerializeField] private ScriptableObject m_ScriptableObject;
        [SerializeField] private AnimationClip m_AnimationClip;
        [SerializeField] private Timeline m_Timeline;

        private List<TimelineWindowCurve> m_CurvesCache = null;

        public virtual int id { get { return m_Id; } set { m_Id = value; } }

        public virtual GameObject gameObject { get { return m_GameObject; } set { m_GameObject = value; } }

        public virtual ScriptableObject scriptableObject { get { return m_ScriptableObject; } set { m_ScriptableObject = value; } }

        public virtual Object sourceObject { get { return (gameObject != null) ? (Object)gameObject : (Object)scriptableObject; } }

        public virtual AnimationClip animationClip { get { return m_AnimationClip; } set { m_AnimationClip = value; } }
        public virtual Timeline timeline
        {
            get { return m_Timeline; }
            set { m_Timeline = value; }
        }

        public virtual GameObject rootGameObject
        {
            get
            {
                Component animationPlayer = this.animationPlayer;
                if (animationPlayer != null)
                {
                    return animationPlayer.gameObject;
                }
                return null;
            }
        }

        public virtual Component animationPlayer
        {
            get
            {
                if (gameObject != null)
                    return TimelineWindowUtility.GetClosestAnimationPlayerComponentInParents(gameObject.transform);
                return null;
            }
        }

        public bool disabled
        {
            get
            {
                // To be editable, a selection must at least contain an animation clip.
                return (timeline == null);
            }
        }

        public virtual bool animationIsEditable
        {
            get
            {
                // Clip is imported and shouldn't be edited
                if (timeline && (timeline.hideFlags & HideFlags.NotEditable) != 0)
                    return false;

                // Object is a prefab - shouldn't be edited
                if (objectIsPrefab)
                    return false;

                return true;
            }
        }

        public virtual bool clipIsEditable
        {
            get
            {
                if (!timeline)
                    return false;
                // Clip is imported and shouldn't be edited
                if ((timeline.hideFlags & HideFlags.NotEditable) != 0)
                    return false;
                if (!AssetDatabase.IsOpenForEdit(animationClip, StatusQueryOptions.UseCachedIfPossible))
                    return false;

                return true;
            }
        }

        public virtual bool objectIsPrefab
        {
            get
            {
                // No gameObject selected
                if (!gameObject)
                    return false;

                if (EditorUtility.IsPersistent(gameObject))
                    return true;

                if ((gameObject.hideFlags & HideFlags.NotEditable) != 0)
                    return true;

                return false;
            }
        }

        public virtual bool objectIsOptimized
        {
            get
            {
                Animator animator = animationPlayer as Animator;
                if (animator == null)
                    return false;

                return animator.isOptimizable && !animator.hasTransformHierarchy;
            }
        }

        public virtual bool canPreview
        {
            get
            {
                if (rootGameObject != null)
                {
                    return !objectIsOptimized;
                }

                return false;
            }
        }

        public virtual bool canRecord
        {
            get
            {
                if (!animationIsEditable)
                    return false;

                return canPreview;
            }
        }

        public virtual bool canChangeAnimationClip
        {
            get
            {
                return rootGameObject != null;
            }
        }

        public virtual bool  canAddCurves
        {
            get
            {
                if (gameObject != null)
                {
                    return !objectIsPrefab && clipIsEditable;
                }
                else if (scriptableObject != null)
                {
                    return true;
                }

                return false;
            }
        }

        public virtual bool canCreateClips
        {
            get
            {
                Component animationPlayer = this.animationPlayer;
                if (animationPlayer == null)
                    return false;

                Animator animator = animationPlayer as Animator;
                if (animator != null)
                {
                    // Need a valid state machine to create clips in the Animator.
                    return (animator.runtimeAnimatorController != null);
                }

                return true;
            }
        }

        public virtual bool canSyncSceneSelection { get { return true; } }

        public List<TimelineWindowCurve> curves
        {
            get
            {
                if (m_CurvesCache == null)
                {
                    m_CurvesCache = new List<TimelineWindowCurve>();

                    if (timeline != null)
                    {
                        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);
                        EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(animationClip);

                        List<TimelineWindowCurve> transformCurves = new List<TimelineWindowCurve>();

                        foreach (EditorCurveBinding curveBinding in curveBindings)
                        {
                            if (TimelineWindowUtility.ShouldShowAnimationWindowCurve(curveBinding))
                            {
                                TimelineWindowCurve curve = new TimelineWindowCurve(animationClip, curveBinding, GetEditorCurveValueType(curveBinding));
                                curve.selectionBinding = this;

                                m_CurvesCache.Add(curve);

                                if (TimelineWindowUtility.IsActualTransformCurve(curveBinding))
                                {
                                    transformCurves.Add(curve);
                                }
                            }
                        }

                        foreach (EditorCurveBinding curveBinding in objectCurveBindings)
                        {
                            TimelineWindowCurve curve = new TimelineWindowCurve(animationClip, curveBinding, GetEditorCurveValueType(curveBinding));
                            curve.selectionBinding = this;

                            m_CurvesCache.Add(curve);
                        }

                        transformCurves.Sort();
                        if (transformCurves.Count > 0)
                        {
                            FillInMissingTransformCurves(transformCurves, ref m_CurvesCache);
                        }
                    }
                    // Curves need to be sorted with path/type/property name so it's possible to construct hierarchy from them
                    // Sorting logic in AnimationWindowCurve.CompareTo()
                    m_CurvesCache.Sort();
                }

                return m_CurvesCache;
            }
        }

        private void FillInMissingTransformCurves(List<TimelineWindowCurve> transformCurves, ref List<TimelineWindowCurve> curvesCache)
        {
            EditorCurveBinding lastBinding = transformCurves[0].binding;
            var propertyGroup = new EditorCurveBinding ? [3];
            string propertyGroupName;
            foreach (var transformCurve in transformCurves)
            {
                var transformBinding = transformCurve.binding;
                //if it's a new property group
                if (transformBinding.path != lastBinding.path
                    || TimelineWindowUtility.GetPropertyGroupName(transformBinding.propertyName) != TimelineWindowUtility.GetPropertyGroupName(lastBinding.propertyName))
                {
                    propertyGroupName = TimelineWindowUtility.GetPropertyGroupName(lastBinding.propertyName);

                    FillPropertyGroup(ref propertyGroup, lastBinding, propertyGroupName, ref curvesCache);

                    lastBinding = transformBinding;

                    propertyGroup = new EditorCurveBinding ? [3];
                }

                AssignBindingToRightSlot(transformBinding, ref propertyGroup);
            }
            FillPropertyGroup(ref propertyGroup, lastBinding, TimelineWindowUtility.GetPropertyGroupName(lastBinding.propertyName), ref curvesCache);
        }

        private void FillPropertyGroup(ref EditorCurveBinding?[] propertyGroup, EditorCurveBinding lastBinding, string propertyGroupName, ref List<TimelineWindowCurve> curvesCache)
        {
            var newBinding = lastBinding;
            newBinding.isPhantom = true;
            if (!propertyGroup[0].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".x";
                TimelineWindowCurve curve = new TimelineWindowCurve(animationClip, newBinding, GetEditorCurveValueType(newBinding));
                curve.selectionBinding = this;
                curvesCache.Add(curve);
            }

            if (!propertyGroup[1].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".y";
                TimelineWindowCurve curve = new TimelineWindowCurve(animationClip, newBinding, GetEditorCurveValueType(newBinding));
                curve.selectionBinding = this;
                curvesCache.Add(curve);
            }

            if (!propertyGroup[2].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".z";
                TimelineWindowCurve curve = new TimelineWindowCurve(animationClip, newBinding, GetEditorCurveValueType(newBinding));
                curve.selectionBinding = this;
                curvesCache.Add(curve);
            }
        }

        private void AssignBindingToRightSlot(EditorCurveBinding transformBinding, ref EditorCurveBinding?[] propertyGroup)
        {
            if (transformBinding.propertyName.EndsWith(".x"))
            {
                propertyGroup[0] = transformBinding;
            }
            else if (transformBinding.propertyName.EndsWith(".y"))
            {
                propertyGroup[1] = transformBinding;
            }
            else if (transformBinding.propertyName.EndsWith(".z"))
            {
                propertyGroup[2] = transformBinding;
            }
        }

        public static TimelineWindowSelectionItem Create()
        {
            TimelineWindowSelectionItem selectionItem = CreateInstance(typeof(TimelineWindowSelectionItem)) as TimelineWindowSelectionItem;
            return selectionItem;
        }

        public int GetRefreshHash()
        {
            return unchecked (id * 19603 ^
                (animationClip != null ? 729 * animationClip.GetHashCode() : 0) ^
                (rootGameObject != null ? 27 * rootGameObject.GetHashCode() : 0) ^
                (scriptableObject != null ? scriptableObject.GetHashCode() : 0));
        }

        public void ClearCache()
        {
            m_CurvesCache = null;
        }

        virtual public void Synchronize()
        {
            // nothing to do.
        }

        public bool Equals(TimelineWindowSelectionItem other)
        {
            return id == other.id &&
                animationClip == other.animationClip &&
                gameObject == other.gameObject &&
                scriptableObject == other.scriptableObject;
        }

        public Type GetEditorCurveValueType(EditorCurveBinding curveBinding)
        {
            if (rootGameObject != null)
            {
                return AnimationUtility.GetEditorCurveValueType(rootGameObject, curveBinding);
            }
            else if (scriptableObject != null)
            {
                return AnimationUtility.GetEditorCurveValueType(scriptableObject, curveBinding);
            }
            else
            {
                if (curveBinding.isPPtrCurve)
                {
                    // Cannot extract type of PPtrCurve.
                    return null;
                }
                else
                {
                    // Cannot extract type of AnimationCurve.  Default to float.
                    return typeof(float);
                }
            }
        }
    }
}
