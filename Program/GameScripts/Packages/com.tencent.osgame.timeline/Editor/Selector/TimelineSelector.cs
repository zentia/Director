using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TimelineEditor
{
    public class TimelineSelector:OdinSelector<Timeline>
    {
        public static TimelineSelector Create(Action<IEnumerable<Timeline>> selectionConfirmed)
        {
            var selector = new TimelineSelector();
            selector.SelectionConfirmed += selectionConfirmed;
            selector.EnableSingleClickToSelect();
            selector.ShowInPopup(200);
            return selector;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            var objects = Object.FindObjectsOfType<Timeline>(true);
            foreach (var timeline in objects)
            {
                tree.Add(timeline.name, timeline);
            }
        }
    }
}
