using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TimelineRuntime;

namespace TimelineEditor
{
    [TimelineItemControl(typeof(TimelineActorCurveClip))]
    public class TimelineActorCurveClipControl : TimelineCurveClipControl
    {
        protected override void OnCurvesChanged(object sender, CurveClipWrapperEventArgs e)
        {
            if (e.wrapper == null)
                return;
            TimelineClipCurveWrapper wrapper = e.wrapper;
            var curveClip = wrapper.timelineItem as TimelineActorCurveClip;
            if (curveClip == null)
                return;
            Undo.RecordObject(curveClip, string.Format("Changed {0}", curveClip.name));
            for (int i = 0; i < curveClip.CurveData.Count; i++)
            {
                var memberCurve = curveClip.CurveData[i];
                TimelineMemberCurveWrapper memberWrapper;
                if (wrapper.TryGetValue(memberCurve.Type, memberCurve.PropertyName, out memberWrapper))
                {
                    int showingCurves = UnityPropertyTypeInfo.GetCurveCount(memberCurve.PropertyType);

                    for (int j = 0; j < showingCurves; j++)
                    {
                        memberCurve.SetCurve(j, new AnimationCurve(memberWrapper.AnimationCurves[j].Curve.keys));
                    }
                }
            }
            curveClip.Firetime = wrapper.fireTime;
            curveClip.Duration = wrapper.Duration;
            EditorUtility.SetDirty(curveClip);
            timelineControl.Repaint();
        }

        public override void PostUpdate(TimelineControlState state)
        {
            base.PostUpdate(state);
            var clip = Wrapper.timelineItem as TimelineActorCurveClip;
            if (clip == null)
                return;
            if (clip.timeline == null && clip.timelineTrack != null)
                clip.timeline = clip.timelineTrack.timeline;
            hasUndoRedoBeenPerformed = Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed";
            if ((curvesChanged || hasUndoRedoBeenPerformed) && state.IsInPreviewMode)
            {
                foreach (var transform in clip.GetActors())
                {
                    clip.SampleTime(transform, state.scrubberPositionFrame);
                }
                curvesChanged = false;
            }
        }

        protected override void ShowContextMenu(Behaviour behaviour)
        {
            var createMenu = new GenericMenu();
            createMenu.AddItem(new GUIContent("Rename"), false, RenameItem, behaviour);
            createMenu.AddItem(new GUIContent("Copy"), false, CopyItem, behaviour);
            createMenu.AddItem(new GUIContent("Delete"), false, DeleteItem, behaviour);
            AddCurveMenu(behaviour, createMenu);
            createMenu.ShowAsContext();
        }

        private void AddCurveMenu(Behaviour behaviour, GenericMenu createMenu)
        {
            TimelineActorCurveClip clip = behaviour as TimelineActorCurveClip;
            if (clip == null)
                return;

            List<KeyValuePair<string, string>> currentCurves = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < clip.CurveData.Count; i++)
            {
                MemberCurveClipData data = clip.CurveData[i];
                KeyValuePair<string, string> curveStrings = new KeyValuePair<string, string>(data.Type, data.PropertyName);
                currentCurves.Add(curveStrings);
            }

            if (clip.Actor != null)
            {
                createMenu.AddSeparator(string.Empty);
                Component[] components = TimelineHelper.GetValidComponents(clip.Actor.gameObject);

                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null)
                    {
                        continue;
                    }
                    MemberInfo[] members = TimelineHelper.GetValidMembers(component);
                    for (int j = 0; j < members.Length; j++)
                    {
                        AddCurveContext context = new AddCurveContext();
                        context.clip = clip;
                        context.component = component;
                        context.memberInfo = members[j];
                        if (!currentCurves.Contains(new KeyValuePair<string, string>(component.GetType().Name, members[j].Name)))
                        {
                            createMenu.AddItem(new GUIContent(string.Format("Add Curve/{0}/{1}", component.GetType().Name, TimelineHelper.GetUserFriendlyName(component, members[j]))), false, AddCurve, context);
                        }
                    }
                }
            }
        }

        private static void AddCurve(object userData)
        {
            var arg = userData as AddCurveContext;
            if (arg != null)
            {
                Type t = null;
                PropertyInfo property = arg.memberInfo as PropertyInfo;
                FieldInfo field = arg.memberInfo as FieldInfo;
                bool isProperty = false;
                if (property != null)
                {
                    t = property.PropertyType;
                    isProperty = true;
                }
                else if (field != null)
                {
                    t = field.FieldType;
                }
                Undo.RecordObject(arg.clip, "Added Curve");
                arg.clip.AddClipCurveData(arg.component, arg.memberInfo.Name, isProperty, t);
                if (arg.clip.timeline)
                {
                    arg.clip.timeline.Recache();
                }
                EditorUtility.SetDirty(arg.clip);
            }
        }

        private class AddCurveContext
        {
            public TimelineActorCurveClip clip;
            public Component component;
            public MemberInfo memberInfo;
        }
    }
}
