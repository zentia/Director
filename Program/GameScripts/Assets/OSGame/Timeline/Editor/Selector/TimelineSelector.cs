using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEngine;

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
            GameObject.FindObjectsOfType<Timeline>(true).ForEach(i=>tree.Add(i.name,i));
        }
    }
}
