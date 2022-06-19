
using UnityEngine;

namespace AGE
{
    public class RandomTickCondition : TickCondition
    {
        public static bool AlwaysFalse = false;

        public int randomValue = 100;

        private int value = 0;

        public override void Process(Action _action, Track _track)
        {
            value = Random.Range(0, 100);

            base.Process(_action, _track);
        }

        public override bool Check(Action _action, Track _track)
        {
            return AlwaysFalse ? false : value <= randomValue;
        }

        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as RandomTickCondition;
            randomValue = srcCopy.randomValue;
            value = 0;
        }

        protected override void ClearData()
        {
            randomValue = 100;
            value = 0;
        }

        protected override uint GetPoolInitCount()
        {
            return 1;
        }
    }
}
