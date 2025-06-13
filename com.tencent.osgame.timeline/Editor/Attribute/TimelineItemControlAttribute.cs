using System;

namespace TimelineEditor
{
    public class TimelineItemControlAttribute : Attribute
    {
        private Type itemType;
        private int drawPriority;

        public TimelineItemControlAttribute(Type type)
        {
            itemType = type;
            drawPriority = 0;
        }

        public TimelineItemControlAttribute(Type type, int drawPriority)
        {
            itemType = type;
            this.drawPriority = drawPriority;
        }

        public Type ItemType => itemType;

        public int DrawPriority => drawPriority;
    }
}
