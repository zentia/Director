using TimelineRuntime;
using UnityEngine;
using System.Collections.Generic;

namespace TimelineEditor
{
    public class TimelineClipCurveWrapper : TimelineActionWrapper
    {
        public List<TimelineMemberCurveWrapper> MemberCurves;

        public TimelineClipCurveWrapper(TimelineItem timelineItem, float fireTime, float duration) : base(timelineItem, fireTime, duration)
        {
            MemberCurves = new List<TimelineMemberCurveWrapper>();
        }

        internal int RowCount
        {
            get
            {
                var num = 0;
                foreach (var wrapper in MemberCurves)
                {
                    num++;
                    if (wrapper.IsFoldedOut)
                    {
                        var animationCurves = wrapper.AnimationCurves;
                        for (var i = 0; i < animationCurves.Count; i++)
                        {
                            num++;
                        }
                    }
                }

                return num;
            }
        }

        internal bool IsEmpty
        {
            get
            {
                if (MemberCurves != null && MemberCurves.Count != 0) 
                    return false;

                return true;
            }
        }

        internal void CropDuration(float newDuration)
        {
            if (newDuration > 0f)
            {
                if (newDuration > Duration)
                {
                    UpdateKeyframeTime(fireTime + Duration, fireTime + newDuration);
                }
                else
                {
                    var memberCurves = MemberCurves;
                    for (var i = 0; i < memberCurves.Count; i++)
                    {
                        var animationCurves = memberCurves[i].AnimationCurves;
                        for (var j = 0; j < animationCurves.Count; j++)
                            animationCurves[j].CollapseEnd(fireTime + Duration, fireTime + newDuration);
                    }
                }

                Duration = newDuration;
            }
        }

        internal void CropFireTime(float newFireTime)
        {
            if (newFireTime < fireTime + Duration)
            {
                if (newFireTime < fireTime)
                {
                    UpdateKeyframeTime(fireTime, newFireTime);
                }
                else
                {
                    var memberCurves = MemberCurves;
                    for (var i = 0; i < memberCurves.Count; i++)
                    {
                        var animationCurves = memberCurves[i].AnimationCurves;
                        for (var j = 0; j < animationCurves.Count; j++)
                            animationCurves[j].CollapseStart(fireTime, newFireTime);
                    }
                }

                Duration += fireTime - newFireTime;
                fireTime = newFireTime;
            }
        }

        internal void ScaleDuration(float newDuration)
        {
            if (newDuration > 0f)
            {
                var memberCurves = MemberCurves;
                for (var i = 0; i < memberCurves.Count; i++)
                {
                    var animationCurves = memberCurves[i].AnimationCurves;
                    for (var j = 0; j < animationCurves.Count; j++) 
                        animationCurves[j].ScaleEnd(Duration, newDuration);
                }

                Duration = newDuration;
            }
        }

        internal void ScaleFiretime(float newFiretime)
        {
            if (newFiretime < fireTime + Duration)
            {
                var memberCurves = MemberCurves;
                for (var i = 0; i < memberCurves.Count; i++)
                {
                    var animationCurves = memberCurves[i].AnimationCurves;
                    for (var j = 0; j < animationCurves.Count; j++)
                        animationCurves[j].ScaleStart(fireTime, Duration, newFiretime);
                }

                Duration += fireTime - newFiretime;
                fireTime = newFiretime;
            }
        }

        internal void TranslateCurves(float amount)
        {
            var frame = TimelineUtility.TimeToFrame(amount);
            if (frame == 0)
            {
                return;
            }
            amount = frame/30.0f;
            fireTime += amount;
            var memberCurves = MemberCurves;
            for (var i = 0; i < memberCurves.Count; i++)
            {
                foreach (var wrapper in memberCurves[i].AnimationCurves)
                {
                    if (amount > 0f)
                    {
                        for (var j = wrapper.KeyframeCount - 1; j >= 0; j--)
                        {
                            var keyframe = wrapper.GetKeyframe(j);
                            var time = keyframe.time + amount;
                            if (time < fireTime)
                                time = fireTime;
                            else if (time > fireTime + Duration)
                                time = fireTime + Duration;
                            var kf = new Keyframe(time, keyframe.value, keyframe.inTangent, keyframe.outTangent)
                            {
                                tangentMode = keyframe.tangentMode
                            };
                            wrapper.MoveKey(j, kf);
                        }
                    }
                    else
                    {
                        for (var j = 0; j < wrapper.KeyframeCount; j++)
                        {
                            var keyframe = wrapper.GetKeyframe(j);
                            var time = keyframe.time + amount;
                            if (time < fireTime)
                                time = fireTime;
                            else if (time > fireTime + Duration)
                                time = fireTime + Duration;
                            var kf = new Keyframe(time, keyframe.value, keyframe.inTangent, keyframe.outTangent)
                            {
                                tangentMode = keyframe.tangentMode
                            };
                            wrapper.MoveKey(j, kf);
                        }
                    }
                }
            }
        }

        public bool TryGetValue(string type, string propertyName, out TimelineMemberCurveWrapper memberWrapper)
        {
            memberWrapper = null;
            foreach (var wrapper in MemberCurves)
            {
                if (wrapper.Type == type && wrapper.PropertyName == propertyName)
                {
                    memberWrapper = wrapper;
                    return true;
                }
            }
            return false;
        }

        private void UpdateKeyframeTime(float oldTime, float newTime)
        {
            for (var i = 0; i < MemberCurves.Count; i++)
            {
                foreach (var wrapper in MemberCurves[i].AnimationCurves)
                {
                    for (var j = 0; j < wrapper.KeyframeCount; j++)
                    {
                        var keyframe = wrapper.GetKeyframe(j);
                        if (Mathf.Abs(keyframe.time - oldTime) < 1E-05)
                        {
                            var kf = new Keyframe(newTime, keyframe.value, keyframe.inTangent, keyframe.outTangent)
                            {
                                tangentMode = keyframe.tangentMode
                            };
                            wrapper.MoveKey(j, kf);
                        }
                    }
                }
            }
        }
    }
}
