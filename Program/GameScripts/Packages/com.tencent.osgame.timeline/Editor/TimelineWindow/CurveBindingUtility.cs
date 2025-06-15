using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace TimelineEditorInternal
{
    static internal class CurveBindingUtility
    {
        // Retrieve current value.  If bindings are available and value is animated, use bindings to get value.
        // Otherwise, evaluate AnimationWindowCurve at current time.
        public static object GetCurrentValue(TimelineWindowState state, TimelineWindowCurve curve)
        {
            if (state.previewing && curve.rootGameObject != null)
            {
                return TimelineWindowUtility.GetCurrentValue(curve.rootGameObject, curve.binding);
            }
            else
            {
                return curve.Evaluate(state.currentTime);
            }
        }

        // Retrieve Current Value.  Use specified bindings to do so.
        public static object GetCurrentValue(GameObject rootGameObject, EditorCurveBinding curveBinding)
        {
            if (rootGameObject != null)
            {
                return TimelineWindowUtility.GetCurrentValue(rootGameObject, curveBinding);
            }
            else
            {
                if (curveBinding.isPPtrCurve)
                {
                    // Cannot extract type of PPtrCurve.
                    return null;
                }
                else if (curveBinding.isDiscreteCurve)
                {
                    return 0;
                }
                else
                {
                    // Cannot extract type of AnimationCurve.  Default to float.
                    return 0.0f;
                }
            }
        }
    }
}
