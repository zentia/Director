using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    [CutsceneTrackGroup(typeof(TrackGroup))]
    public class GenericTrackGroupControl : TrackGroupControl
    {
        protected override void addTrackContext()
        {
            TrackGroup trackGroup = TrackGroup.Behaviour as TrackGroup;
            if (trackGroup != null)
            {
                List<Type> trackTypes = trackGroup.GetAllowedTrackTypes();

                GenericMenu createMenu = new GenericMenu();

                foreach (var trackType in trackTypes)
                {
                    MemberInfo info = trackType;
                    string label = String.Empty;
                    foreach (TimelineTrackAttribute attribute in info.GetCustomAttributes(typeof(TimelineTrackAttribute), true))
                    {
                        label = attribute.Label;
                        break;
                    }

                    createMenu.AddItem(new GUIContent(string.Format("Add {0}", label)), false, addTrack, new TrackContextData(label, trackType, trackGroup));
                }

                createMenu.ShowAsContext();
            }
        }

        private void addTrack(object userData)
        {
            TrackContextData trackData = userData as TrackContextData;
            if (trackData != null)
            {
                var item = CutsceneItemFactory.CreateTimelineTrack(trackData.TrackGroup, trackData.Type, trackData.Label);
                isExpanded = true;
                Undo.RegisterCreatedObjectUndo(item, string.Format("Create {0}", item.name));
            }
        }

        private class TrackContextData
        {
            public string Label;
            public Type Type;
            public TrackGroup TrackGroup;

            public TrackContextData(string label, Type type, TrackGroup trackGroup)
            {
                this.Label = label;
                this.Type = type;
                this.TrackGroup = trackGroup;
            }
        }
    }

}
