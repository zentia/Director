using System;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace TimelineEditorInternal
{
    internal class TimelineClipSelectionItem : TimelineWindowSelectionItem
    {
        public static TimelineClipSelectionItem Create(AnimationClip animationClip, Object sourceObject)
        {
            TimelineClipSelectionItem selectionItem = CreateInstance(typeof(TimelineClipSelectionItem)) as TimelineClipSelectionItem;

            selectionItem.gameObject = sourceObject as GameObject;
            selectionItem.scriptableObject = sourceObject as ScriptableObject;
            selectionItem.animationClip = animationClip;
            selectionItem.id = 0; // no need for id since there's only one item in selection.

            return selectionItem;
        }

        public override bool canPreview { get { return false; } }

        public override bool canRecord { get { return false; } }

        public override bool canChangeAnimationClip { get { return false; } }

        public override bool canSyncSceneSelection { get { return false; } }
    }
}
