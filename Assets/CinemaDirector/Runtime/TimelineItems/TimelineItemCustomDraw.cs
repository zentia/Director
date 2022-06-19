using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    internal static class TimelineItemCustomDraw
    {
        public static string DrawAnimation(AGE.Action action, int targetID, string clipName, GUIContent content)
        {
            var clipNames = new List<GUIContent> { new GUIContent(clipName) };
#if UNITY_EDITOR
            var go = action.GetGameObject(targetID);
            if (go != null)
            {
                var animation = go.GetComponent<Animation>();
                if (animation != null)
                {
                    foreach (AnimationState animationState in animation)
                    {
                        if (animationState.name == clipName)
                        {
                            continue;
                        }
                        clipNames.Add(new GUIContent(animationState.name));
                    }   
                }
            }

            return clipNames[UnityEditor.EditorGUILayout.Popup(content, 0, clipNames.ToArray())].text;
#else
            return String.Empty;
#endif
        }
    }
}
