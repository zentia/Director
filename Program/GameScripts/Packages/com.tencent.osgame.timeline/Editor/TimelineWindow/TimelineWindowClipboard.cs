using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using TimelineEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TimelineEditorInternal
{
    // Classes for copy/paste support of animation window related things

    [Serializable]
    internal sealed class TimelineWindowEventClipboard
    {
        public float time = 0;
        public string functionName = "";
        public string stringParam = "";
        public int objectParam = 0;
        public float floatParam = 0;
        public int intParam = 0;
        public SendMessageOptions messageOptions = SendMessageOptions.RequireReceiver;

        public TimelineWindowEventClipboard(AnimationEvent e)
        {
            time = e.time;
            functionName = e.functionName;
            stringParam = e.stringParameter;
            objectParam = e.objectReferenceParameter ? e.objectReferenceParameter.GetInstanceID() : 0;
            floatParam = e.floatParameter;
            intParam = e.intParameter;
            messageOptions = e.messageOptions;
        }

        public static AnimationEvent FromClipboard(TimelineWindowEventClipboard e)
        {
            return new AnimationEvent
            {
                time = e.time,
                functionName = e.functionName,
                stringParameter = e.stringParam,
                objectReferenceParameter = InternalEditorUtility.GetObjectFromInstanceID(e.objectParam),
                floatParameter = e.floatParam,
                intParameter = e.intParam,
                messageOptions = e.messageOptions
            };
        }
    }

    [Serializable]
    internal class TimelineWindowEventsClipboard
    {
        public TimelineWindowEventClipboard[] events;

        internal static bool CanPaste()
        {
            return Clipboard.HasCustomValue<TimelineWindowEventsClipboard>();
        }

        internal static void CopyEvents(IList<AnimationEvent> allEvents, bool[] selected, int explicitIndex = -1)
        {
            var copyEvents = new List<TimelineWindowEventClipboard>();
            // If a selection already exists, copy selection instead of clicked index
            if (Array.Exists(selected, s => s))
            {
                for (var i = 0; i < selected.Length; ++i)
                {
                    if (selected[i])
                        copyEvents.Add(new TimelineWindowEventClipboard(allEvents[i]));
                }
            }
            // Else, only copy the clicked animation event
            else if (explicitIndex >= 0)
            {
                copyEvents.Add(new TimelineWindowEventClipboard(allEvents[explicitIndex]));
            }
            var data = new TimelineWindowEventsClipboard {events = copyEvents.ToArray()};

            // Animation keyframes right now do not go through regular clipboard machinery,
            // so when copying Events, make sure Keyframes are cleared from the clipboard, or things
            // get confusing.
            TimelineWindowState.ClearKeyframeClipboard();

            Clipboard.SetCustomValue(data);
        }

        internal static AnimationEvent[] AddPastedEvents(AnimationEvent[] events, float time, out bool[] selected)
        {
            selected = null;
            var data = Clipboard.GetCustomValue<TimelineWindowEventsClipboard>();
            if (data?.events == null || data.events.Length == 0)
                return null;

            var minTime = data.events.Min(e => e.time);

            var origEventsCount = events.Length;
            // Append new events to the end first,
            var newEvents = new List<AnimationEvent>();
            foreach (var e in data.events)
            {
                var t = e.time - minTime + time;
                var newEvent = TimelineWindowEventClipboard.FromClipboard(e);
                newEvent.time = t;
                newEvents.Add(newEvent);
            }
            events = events.Concat(newEvents).ToArray();

            // Re-sort events by time
            var order = new int[events.Length];
            for (var i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(events, order, new TimelineEventTimeLine.EventComparer());

            // Mark pasted ones as selected
            selected = new bool[events.Length];
            for (var i = 0; i < order.Length; ++i)
                selected[i] = order[i] >= origEventsCount;

            return events;
        }
    }
}
