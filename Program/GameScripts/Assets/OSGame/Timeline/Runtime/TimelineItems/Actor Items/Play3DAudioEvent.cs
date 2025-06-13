using System.Collections.Generic;
using Assets.Plugins.Common;
using Assets.Scripts.Sound;
using UnityEngine;

namespace TimelineRuntime
{
    [TimelineItem("Audio Source", "Play 3D Audio", TimelineItemGenre.ActorItem)]
    public class Play3DAudioEvent : TimelineActorEvent
    {
        public enum PlayEvent
        {
            Play = 0,
            Stop = 1,
        }

        public string SoundName = "";
        public PlayEvent playEvent = PlayEvent.Play;
        [Header("是否在Timeline结束的时候停止声音?")]
        public bool AutoStopWhenTimelineStop = false;

        [Header("是否程序停止声音")]
        public bool ManualStopWhenTimelineStop = false;

        private static HashSet<uint> DontStopSoundEventIDs = new();

        private static Dictionary<string, uint> SoundNameToId = new();
        private uint playId = 0;
        public override void Trigger(GameObject actor)
        {
#if UNITY_EDITOR
            if (!CSoundManager.HasInstance())
            {
                return;
            }
#endif
            switch (playEvent)
            {
                case PlayEvent.Play:
                    playId = CSoundManager.GetInstance().PostEvent(SoundName, actor);
                    SoundNameToId[SoundName] = playId;
                    break;
                case PlayEvent.Stop:
                    if (SoundNameToId.TryGetValue(SoundName, out playId))
                    {

                        CSoundManager.GetInstance().StopEvent(playId);
                        SoundNameToId.Remove(SoundName);
                    }
                    else
                    {
                        Log.LogE(LogTag.Unknown, "never play {0}", SoundName);
                    }
                    break;
            }
        }


        public override void Stop(GameObject actor)
        {
#if UNITY_EDITOR
            if (!CSoundManager.HasInstance())
            {
                return;
            }
#endif

            if (AutoStopWhenTimelineStop)
            {
                CSoundManager.GetInstance().StopEvent(playId);
            }
            else if (ManualStopWhenTimelineStop)
            {
                DontStopSoundEventIDs.Add(playId);
            }
        }
    }
}
