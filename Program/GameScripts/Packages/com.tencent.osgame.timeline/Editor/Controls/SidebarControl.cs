using UnityEditor;

namespace TimelineEditor
{
    public abstract class SidebarControl : TimelineBehaviourControl
    {
        public int expandedSize = 2;
        public bool isExpanded = true;

        public event SidebarControlHandler DuplicateRequest;
        public SidebarControlHandler SelectRequest;

        public void RequestDuplicate()
        {
            DuplicateRequest?.Invoke(this, new SidebarControlEventArgs(behaviour, this));
        }

        protected void RequestSelect()
        {
            SelectRequest?.Invoke(this, new SidebarControlEventArgs(behaviour, this));
        }

        internal void Select()
        {
            var gameObjects = Selection.gameObjects;
            ArrayUtility.Add(ref gameObjects, behaviour.gameObject);
            Selection.objects = gameObjects;
        }
    }
}
