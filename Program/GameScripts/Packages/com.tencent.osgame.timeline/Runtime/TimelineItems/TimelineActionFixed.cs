using UnityEngine;

namespace TimelineRuntime
{
    public abstract class TimelineActionFixed : TimelineAction
    {
        [SerializeField]
        private float inTime = 0f;
        [SerializeField]
        private float outTime = 1f;
        [SerializeField]
        private float itemLength = 0f;

        public float InTime
        {
            get { return inTime; }
            set
            {
                inTime = value;
                Duration = outTime - inTime;
            }
        }

        public float OutTime
        {
            get { return outTime; }
            set
            {
                outTime = value;
                Duration = outTime - inTime;
            }
        }

        public float ItemLength
        {
            get { return itemLength; }
            set { itemLength = value; }
        }
    }
}