using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
    [CutsceneTrack(typeof(TimelineTrack))]
    public class GenericTrackControl : TimelineTrackControl
    {
        private int controlID;
        protected override void updateHeaderControl4(Rect position)
        {
            TimelineTrack track = TargetTrack.Behaviour as TimelineTrack;
            if (track == null)
            {
                return;
            }

            Color temp = GUI.color;
            GUI.color = (track.GetTimelineItems().Count > 0) ? Color.green : Color.red;
            if (GUI.Button(position, string.Empty, TrackGroupControl.styles.addIcon))
            {
                List<Type> trackTypes = track.GetAllowedCutsceneItems();

                if (trackTypes.Count == 1)
                {
                    ContextData data = getContextData(trackTypes[0]);
                    if (data.PairedType == null)
                    {
                        addCutsceneItem(data);
                    }
                }
                else if (trackTypes.Count > 1)
                {
                    GenericMenu createMenu = new GenericMenu();
                    for (int i = 0; i < trackTypes.Count; i++)
                    {
                        ContextData data = getContextData(trackTypes[i]);

                        createMenu.AddItem(new GUIContent($"{data.Category}/{data.Label}"), false, addCutsceneItem, data);
                    }
                    createMenu.ShowAsContext();
                }
            }

            GUI.color = temp;
        }

        private void addCutsceneItem(object userData)
        {
            ContextData data = userData as ContextData;
            if (data != null)
            {
                if (data.PairedType == null)
                {
                    var item = CutsceneItemFactory.CreateCutsceneItem(data.Track, data.Type, data.Label, data.FireTime);
                    Undo.RegisterCreatedObjectUndo(item, $"Create {item.name}");
                }
            }
        }

        private ContextData getContextData(Type type)
        {
            MemberInfo info = type;
            string label = string.Empty;
            string category = string.Empty;
            Type requiredObject = null;
            foreach (CutsceneItemAttribute customAttribute in info.GetCustomAttributes(typeof(CutsceneItemAttribute), true))
            {
                label = customAttribute.Label;
                category = customAttribute.Category;
                requiredObject = customAttribute.RequiredObjectType;
                break;
            }

            return new ContextData(this.controlID, type, requiredObject, (TargetTrack.Behaviour as TimelineTrack),
                category, label, state.ScrubberPosition);
        }

        private class ContextData
        {
            public int ControlID;
            public Type Type;
            public Type PairedType;
            public TimelineTrack Track;
            public string Category;
            public string Label;
            public float FireTime;

            public ContextData(int controlId, Type type, Type pairedType, TimelineTrack track, string category,
                string label, float firetime)
            {
                ControlID = controlId;
                Type = type;
                PairedType = pairedType;
                Track = track;
                Category = category;
                Label = label;
                FireTime = firetime;
            }
        }
    }
}