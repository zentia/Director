using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace TimelineEditor
{
    public class TimelineTextSelector<T>:OdinSelector<T>
    {
        private Func<IEnumerable<T>> m_Collect;
        private Func<T, string> m_Path;
        public static TimelineTextSelector<T> Create(Action<IEnumerable<T>> selectionConfirmed, Func<IEnumerable<T>> collect, Func<T, string> path)
        {
            var selector = new TimelineTextSelector<T>();
            selector.SelectionConfirmed += selectionConfirmed;
            selector.m_Collect = collect;
            selector.m_Path = path;
            selector.EnableSingleClickToSelect();
            selector.ShowInPopup(300);
            return selector;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            var result = m_Collect();
            if (result == null)
                return;
            foreach (var item in result)
            {
                tree.Add(m_Path(item),item);
            }
        }
    }
}
