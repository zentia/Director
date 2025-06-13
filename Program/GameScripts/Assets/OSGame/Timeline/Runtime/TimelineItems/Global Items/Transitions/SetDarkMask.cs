using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarp;

namespace TimelineRuntime
{
    [TimelineItem("Transitions", "DarkMask", TimelineItemGenre.GlobalItem)]
    public class SetDarkMask : TimelineGlobalAction
    {
        public AnimationCurve animationCurve;
        [SerializeField, LabelText("不需要被压黑的轨道组列表")]
        private ActorTrackGroup[] actorTrackGroupList;

        public override void End()
        {
            InGameShowTime.instance.EndShowTime();
        }

        public override void Stop()
        {
            End();
        }

        public override void Trigger()
        {
            var result = new HashSet<List<Renderer>>();
            actorTrackGroupList.ForEach(item=>result.Add(item.GetRenderers()));
            InGameShowTime.instance.StartShowTime(result, Color.black);
        }

        public override void UpdateTime(float time, float deltaTime)
        {
            if (animationCurve != null)
            {
                InGameShowTime.instance.SetDarkMaskQuality(animationCurve.Evaluate(time));
            }
        }
    }
}
