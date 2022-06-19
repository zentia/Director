using UnityEngine;

namespace CinemaDirector
{
    public abstract class TimelineActionFixed:TimelineAction
    {
        [SerializeField]
        private float inTime = 0f;
        [SerializeField]
        private float outTime = 0f;
        [SerializeField]
        private float itemLength = 0f;

        public float ItemLength
        {
            get { return itemLength; }
            set { itemLength = value; }
        }
    }
}
