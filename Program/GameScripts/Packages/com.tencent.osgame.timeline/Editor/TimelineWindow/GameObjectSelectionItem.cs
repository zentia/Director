using System;
using TimelineRuntime;
using UnityEngine;
using UnityEditor;

namespace TimelineEditorInternal
{
    internal class GameObjectSelectionItem : TimelineWindowSelectionItem
    {
        public static GameObjectSelectionItem Create(GameObject gameObject)
        {
            GameObjectSelectionItem selectionItem = CreateInstance(typeof(GameObjectSelectionItem)) as GameObjectSelectionItem;

            selectionItem.gameObject = gameObject;
            selectionItem.timeline = null;
            selectionItem.id = 0; // no need for id since there's only one item in selection.

            if (selectionItem.rootGameObject != null)
            {
                var timeline = selectionItem.rootGameObject.GetComponent<Timeline>();

                if (selectionItem.timeline == null && selectionItem.gameObject != null) // there is activeGO but clip is still null
                    selectionItem.timeline = timeline;
                else if (timeline != selectionItem.timeline)  // clip doesn't belong to the currently active GO
                    selectionItem.timeline = timeline;
            }

            return selectionItem;
        }

        public override AnimationClip animationClip
        {
            set
            {
                base.animationClip = value;
            }
            get
            {
                if (animationPlayer == null)
                    return null;

                return base.animationClip;
            }
        }

        public override Timeline timeline
        {
            get
            {
                return animationPlayer as Timeline;
            }
            set
            {
                base.timeline = value;
            }
        }

        public override void Synchronize()
        {
            if (rootGameObject != null)
            {
                AnimationClip[] allClips = AnimationUtility.GetAnimationClips(rootGameObject);
                if (allClips.Length > 0)
                {
                    if (!Array.Exists(allClips, x => x == animationClip))
                    {
                        animationClip = allClips[0];
                    }
                }
                else
                {
                    animationClip = null;
                }
            }
        }
    }
}
