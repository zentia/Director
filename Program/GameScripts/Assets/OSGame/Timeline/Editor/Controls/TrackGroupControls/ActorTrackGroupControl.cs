using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using TimelineRuntime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimelineEditor
{
    [TimelineTrackGroupControl(typeof(ActorTrackGroup))]
    public class ActorTrackGroupControl : TimelineTrackGroupControl
    {
        public ActorTrackGroupControl(TimelineTrackGroupWrapper wrapper) : base(wrapper)
        {

        }

        public override void Initialize()
        {
            base.Initialize();
            LabelPrefix = styles.ActorGroupIcon.normal.background;
        }

        private class TransformSelector : OdinSelector<Transform>
        {
            public static OdinSelector<Transform> Create(Rect rect, Action<IEnumerable<Transform>> onSelectionConfirmed)
            {
                var selector = new TransformSelector();
                selector.SelectionConfirmed += onSelectionConfirmed;
                selector.EnableSingleClickToSelect();
                rect.width = 200;
                selector.ShowInPopup(rect);
                return selector;
            }

            protected override void BuildSelectionTree(OdinMenuTree tree)
            {
                SceneManager.GetActiveScene().GetRootGameObjects().ForEach(e => tree.Add(e.name, e.transform));
            }
        }

        protected override void UpdateHeaderControl4(Rect position)
        {
            var actorTrackGroup = Wrapper.Data as ActorTrackGroup;
            if (actorTrackGroup == null)
            {
                return;
            }
            var actors = actorTrackGroup.Actors;
            var temp = GUI.color;
            if (actors.Count == 0)
            {
                GUI.color = Color.red;
                GenericSelector<Transform>.DrawSelectorDropdown(position, string.Empty, _ => TransformSelector.Create(position, selections =>
                {
                    actorTrackGroup.Actors = selections.ToList();
                }), styles.PickerStyle);
            }
            else
            {
                GUI.color = Color.green;
                if (GUI.Button(position, string.Empty, styles.PickerStyle))
                {
                    UnityEditor.Selection.activeGameObject = actors[0].gameObject;
                }
            }
            GUI.color = temp;
        }
    }
}
