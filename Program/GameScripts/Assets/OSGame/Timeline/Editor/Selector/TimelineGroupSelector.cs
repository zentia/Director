using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEditor;

namespace TimelineEditor
{
    public class TimelineGroupSelector:OdinSelector<ActorTrackGroup>
    {
        public static TimelineGroupSelector Create(Action<IEnumerable<ActorTrackGroup>> selectionConfirmed)
        {
            var selector = new TimelineGroupSelector();
            selector.SelectionConfirmed += selectionConfirmed;
            selector.EnableSingleClickToSelect();
            selector.ShowInPopup(200);
            return selector;
        }
        
        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Add("null", null);
            var timeline = Selection.activeGameObject.GetComponentInParent<Timeline>();
            foreach (var group in timeline.actorTrackGroups)
            {
                tree.Add(group.name, group);
            }    
        }
    }
}