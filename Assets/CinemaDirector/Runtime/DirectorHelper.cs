using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using AGE;

namespace CinemaDirector
{
    public static class DirectorHelper
    {
        public static CutsceneWrapper UpdateWrapper(Cutscene cutscene, CutsceneWrapper wrapper)
        {
            if (cutscene == null) 
                return null;

            if (wrapper == null || !cutscene.Equals(wrapper.Behaviour))
            {
                return CreateWrapper(cutscene);
            }
            wrapper.Behaviour = cutscene;
            wrapper.Duration = cutscene.Duration;
            wrapper.IsPlaying = cutscene.State == CutsceneState.PreviewPlaying || cutscene.State == CutsceneState.Playing;
            wrapper.RunningTime = cutscene.RunningTime;

            List<DirectorObject> itemsToRemove = new List<DirectorObject>();
            foreach (DirectorObject behaviour in wrapper.Behaviours)
            {
                bool found = false;
                foreach (TrackGroup group in cutscene.Children)
                {
                    if (behaviour.Equals(group))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found || behaviour == null)
                {
                    itemsToRemove.Add(behaviour);
                }
            }

            foreach (DirectorObject trackGroup in itemsToRemove)
            {
                wrapper.HasChanged = true;
                wrapper.RemoveTrackGroup(trackGroup);
            }

            foreach (TrackGroup tg in cutscene.Children)
            {
                TrackGroupWrapper tgWrapper = null;
                if (!wrapper.ContainsTrackGroup(tg, out tgWrapper))
                {

                    tgWrapper = new TrackGroupWrapper(tg);
                    tgWrapper.Ordinal = tg.Ordinal;
                    wrapper.AddTrackGroup(tg, tgWrapper);
                    wrapper.HasChanged = true;
                }

                foreach (TimelineTrack track in tg.GetTracks())
                {
                    TimelineTrackWrapper trackWrapper;
                    if (!tgWrapper.ContainsTrack(track, out trackWrapper))
                    {
                        trackWrapper = new TimelineTrackWrapper(track);
                        trackWrapper.Ordinal = tg.Ordinal;
                        tgWrapper.AddTrack(track, trackWrapper);
                        tgWrapper.HasChanged = true;
                    }

                    foreach (TimelineItem item in track.GetTimelineItems())
                    {
                        TimelineItemWrapper itemWrapper = null;
                        if (!trackWrapper.ContainsItem(item, out itemWrapper))
                        {
                            if (item is CinemaActorClipCurve)
                            {
                                CinemaClipCurve clip = item as CinemaClipCurve;
                                itemWrapper = new CinemaClipCurveWrapper(clip, clip.Firetime, clip.Duration);
                                trackWrapper.AddItem(clip, itemWrapper);
                            }
                            else if (item is TimelineAction)
                            {
                                TimelineAction action = item as TimelineAction;
                                itemWrapper = new CinemaActionWrapper(action, action.Firetime, action.Duration);
                                trackWrapper.AddItem(action, itemWrapper);
                            }
                            else
                            {
                                itemWrapper = new TimelineItemWrapper(item, item.Firetime);
                                trackWrapper.AddItem(item, itemWrapper);
                            }
                            trackWrapper.HasChanged = true;
                        }
                        else
                        {
                            if (GUIUtility.hotControl == 0)
                            {
                                if (itemWrapper is CinemaActionWrapper)
                                {
                                    TimelineAction action = item as TimelineAction;
                                    CinemaActionWrapper actionWrapper = itemWrapper as CinemaActionWrapper;
                                    actionWrapper.Firetime = action.Firetime;
                                    actionWrapper.Duration = action.Duration;
                                }
                                else
                                {
                                    itemWrapper.Firetime = item.Firetime;
                                }
                            }
                        }
                    }

                    // Remove missing track items
                    List<TimelineItem> itemRemovals = new List<TimelineItem>();
                    foreach (TimelineItem behaviour in trackWrapper.TimelineItems)
                    {
                        bool found = false;
                        foreach (TimelineItem item in track.GetTimelineItems())
                        {
                            if (behaviour.Equals(item))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found || behaviour == null)
                        {
                            itemRemovals.Add(behaviour);
                        }
                    }
                    foreach (var item in itemRemovals)
                    {
                        trackWrapper.HasChanged = true;
                        trackWrapper.RemoveItem(item);
                    }
                }

                // Remove missing tracks
                List<TimelineTrack> removals = new List<TimelineTrack>();
                foreach (var behaviour in tgWrapper.Behaviours)
                {
                    bool found = false;
                    foreach (TimelineTrack track in tg.GetTracks())
                    {
                        if (behaviour.Equals(track))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found || behaviour == null)
                    {
                        removals.Add(behaviour);
                    }
                }
                foreach (var track in removals)
                {
                    tgWrapper.HasChanged = true;
                    tgWrapper.RemoveTrack(track);
                }
            }

            return wrapper;
        }

        public static void ReflectChanges(Cutscene cutscene, CutsceneWrapper wrapper)
        {
            if (cutscene == null || wrapper == null) return;

            cutscene.Duration = wrapper.Duration;
            foreach (TrackGroupWrapper tgw in wrapper.TrackGroups)
            {
                TrackGroup tg = tgw.Behaviour as TrackGroup;
                tg.Ordinal = tgw.Ordinal;
                bool dirty = false;
                foreach (TimelineTrackWrapper trackWrapper in tgw.Tracks)
                {
                    TimelineTrack track = trackWrapper.Behaviour as TimelineTrack;
                    if (track.trackIndex != trackWrapper.Ordinal)
                    {
                        dirty = true;
                    }
                    track.trackIndex = trackWrapper.Ordinal;
                }
                if (dirty)
                {
                    tg.Children.Sort();
                    tg.Dirty = true;
                }
            }
        }

        public static CutsceneWrapper CreateWrapper(Cutscene cutscene)
        {
            CutsceneWrapper wrapper = new CutsceneWrapper(cutscene);
            if (cutscene != null)
            {
                wrapper.RunningTime = cutscene.RunningTime;
                wrapper.Duration = cutscene.Duration;
                wrapper.IsPlaying = cutscene.State == CutsceneState.PreviewPlaying || cutscene.State == CutsceneState.Playing;

                foreach (TrackGroup tg in cutscene.Children)
                {
                    TrackGroupWrapper tgWrapper = new TrackGroupWrapper(tg);
                    tgWrapper.Ordinal = tg.Ordinal;
                    wrapper.AddTrackGroup(tg, tgWrapper);

                    foreach (TimelineTrack track in tg.GetTracks())
                    {
                        TimelineTrackWrapper trackWrapper = new TimelineTrackWrapper(track);
                        trackWrapper.Ordinal = track.trackIndex;
                        tgWrapper.AddTrack(track, trackWrapper);

                        foreach (TimelineItem item in track.GetTimelineItems())
                        {
                            if (item is CinemaActorClipCurve)
                            {
                                var clip = item as CinemaActorClipCurve;
                                CinemaClipCurveWrapper clipWrapper = new CinemaClipCurveWrapper(clip, clip.Firetime, clip.Duration);
                                trackWrapper.AddItem(clip, clipWrapper);
                            }
                            else if (item is TimelineAction)
                            {
                                TimelineAction action = item as TimelineAction;
                                CinemaActionWrapper itemWrapper = new CinemaActionWrapper(action, action.Firetime, action.Duration);
                                trackWrapper.AddItem(action, itemWrapper);
                            }
                            else
                            {
                                TimelineItemWrapper itemWrapper = new TimelineItemWrapper(item, item.Firetime);
                                trackWrapper.AddItem(item, itemWrapper);
                            }
                        }
                    }
                }
            }
            return wrapper;
        }

        public enum TimeEnum
        {
            Minutes = 60,
            Seconds = 1
        }

        public static Cutscene LoadCutscene(string path, bool relative, string assetsPath)
        {
            var cutScene = ScriptableObject.CreateInstance<Cutscene>();
            cutScene.Load(path, relative, assetsPath);
            return cutScene;
        }

        public static object Create(Type targetType, object defaultValue)
        {
            if (Type.GetTypeCode(targetType) == TypeCode.String)
            {
                if (defaultValue == null)
                {
                    return string.Empty;
                }
                return defaultValue;
            }
            // get the default constructor and instantiate
            Type[] types = new Type[0];
            ConstructorInfo info = targetType.GetConstructor(types);
            object targetObject;
            if (info == null) //must not have found the constructor
                if (targetType.BaseType.UnderlyingSystemType.FullName.Contains("Enum"))
                    targetObject = Activator.CreateInstance(targetType);
                else
                {
                    return defaultValue;
                }
            else
                targetObject = info.Invoke(null);

            if (targetObject == null)
                return defaultValue;
            return targetObject;
        }

        public static bool GetPath(this Transform child, Transform root,out string  path)
        {
            path = "";
            while (child != null)
            {
                path = child.name + "/" + path; 
                if (child.parent == root)
                {
                    path = path.Substring(0, path.Length - 1);
                    return true;
                }
                child = child.parent;
            }
            return false;
        }
        
        public static Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }

            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }

            return r;
        }
        
        public static string getCutsceneItemName(DirectorObject parent, string name, System.Type type)
        {
            return getCutsceneItemName(parent.Children, name, type, 0);
        }

        public static string getCutsceneItemName(string name, System.Type type)
        {
            return getCutsceneItemName(name, type, 0);
        }

        private static string getCutsceneItemName(string name, System.Type type, int iteration)
        {
            string newName = name;
            if (iteration > 0)
            {
                newName = string.Format("{0} {1}", name, iteration);
            }
            bool isDuplicate = false;
            UnityEngine.Object[] items = UnityEngine.Object.FindObjectsOfType(type);
            foreach (UnityEngine.Object obj in items)
            {
                if (newName == obj.name)
                {
                    isDuplicate = true;
                }
            }

            if (isDuplicate)
            {
                return getCutsceneItemName(name, type, ++iteration);
            }
            return newName;
        }

        private static string getCutsceneItemName(List<DirectorObject> children, string name, System.Type type, int iteration)
        {
            string newName = name;
            if (iteration > 0)
            {
                newName = string.Format("{0} {1}", name, iteration);
            }
            bool isDuplicate = false;
            foreach (var obj in children)
            {
                if (newName == obj.name)
                {
                    isDuplicate = true;
                }
            }

            if (isDuplicate)
            {
                return getCutsceneItemName(children, name, type, ++iteration);
            }
            return newName;
        }

        public static bool IsTrackItemValidForTrack(DirectorObject behaviour, TimelineTrack track)
        {
            bool retVal = false;
            if (track.GetType() == (typeof(CurveTrack)) || track.GetType().IsSubclassOf(typeof(CurveTrack)))
            {
                if (behaviour.GetType()==(typeof(CinemaActorClipCurve)))
                {
                    retVal = true;
                }
            }
            return retVal;
        }

        public static string Trim(string name)
        {
            var idx = name.LastIndexOf("/") + 1;
            if (idx < 1)
                return name;

            return name.Substring(idx, name.Length - idx);
        }

        public static PropertyInfo[] getValidProperties(DirectorObject directorObject)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (PropertyInfo propertyInfo in directorObject.GetType().GetProperties())
            {
                if (UnityPropertyTypeInfo.GetMappedType(propertyInfo.PropertyType) != PropertyTypeInfo.None && propertyInfo.CanWrite)
                {
                    properties.Add(propertyInfo);
                }
            }
            return properties.ToArray();
        }

        public static FieldInfo[] getValidFields(DirectorObject directorObject)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (FieldInfo fieldInfo in directorObject.GetType().GetFields())
            {
                if (UnityPropertyTypeInfo.GetMappedType(fieldInfo.FieldType)!=PropertyTypeInfo.None)
                {
                    fields.Add(fieldInfo);
                }
            }
            return fields.ToArray();
        }

        public static MemberInfo[] getValidMembers(DirectorObject directorObject)
        {
            PropertyInfo[] properties = getValidProperties(directorObject);
            FieldInfo[] fields = getValidFields(directorObject);

            List<MemberInfo> members = new List<MemberInfo>();
            members.AddRange(properties);
            members.AddRange(fields);
            return members.ToArray();
        }

        public static string GetUserFriendlyName(DirectorObject component, MemberInfo memberInfo)
        {
            return GetUserFriendlyName(component.GetType().Name, memberInfo.Name);
        }

        public static string GetUserFriendlyName(string componentName, string memberName)
        {
            const string pattern = @"(?<=[^A-Z])(?=[A-Z])";
            memberName = Regex.Replace(memberName, pattern, " ", RegexOptions.None);
            memberName = memberName.Substring(0, 1).ToUpper() + memberName.Substring(1);
            return memberName;
        }
    }
}