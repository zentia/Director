using System;

namespace TimelineRuntime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ObjectSpaceAttribute : Attribute
    {
    }

    [Serializable]
    public class ObjectSpace
    {
        public string path;
        public ActorTrackGroup group;
    }
}