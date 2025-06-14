using TimelineRuntime;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using Object = UnityEngine.Object;

namespace TimelineEditor
{
    public static class TimelineHelper
    {
        private static void UpdateParam(TimelineWrapper timelineWrapper, Timeline timeline)
        {
            timelineWrapper.IsPlaying = timeline.state is Timeline.TimelineState.PreviewPlaying or Timeline.TimelineState.Playing;
            timelineWrapper.RunningTime = timeline.RunningTime;
        }
        public static void UpdateWrapper(Timeline timeline, TimelineControl timelineControl)
        {
            if (timeline == null)
            {
                timelineControl.Wrapper = null;
                return;
            }
            var trackGroups = timeline.trackGroups;
            timeline.GetComponentsInChildren(true, trackGroups);
            if (timelineControl.Wrapper == null || timelineControl.Wrapper.timeline != timeline)
            {
                CreateWrapper(timeline, timelineControl);
                timelineControl.Rescale();
                return;
            }

            UpdateParam(timelineControl.Wrapper, timeline);
            for (var i = trackGroups.Count - 1; i >= 0; i--)
            {
                var trackGroup = timeline.trackGroups[i];
                if (!timelineControl.Wrapper.ContainsTrackGroup(trackGroup, out var trackGroupWrapper))
                {
                    trackGroupWrapper = new TimelineTrackGroupWrapper(trackGroup);
                    timelineControl.Wrapper.AddTrackGroup(trackGroupWrapper);
                    timelineControl.Wrapper.HasChanged = true;
                }

                trackGroup.GetComponentsInChildren(true, trackGroup.timelineTracks);
                foreach (var track in trackGroup.timelineTracks)
                {
                    if (!trackGroupWrapper.ContainsTrack(track, out var trackWrapper))
                    {
                        trackWrapper = new TimelineTrackWrapper(track, trackGroupWrapper);
                        trackGroupWrapper.AddTrack(trackWrapper);
                        trackGroupWrapper.HasChanged = true;
                    }

                    track.GetComponentsInChildren(true, track.timelineItems);
                    foreach (var item in track.timelineItems)
                    {
                        if (!trackWrapper.ContainsItem(item, out var itemWrapper))
                        {
                            if (item.GetType().IsSubclassOf(typeof(TimelineCurveClip)))
                            {
                                var curveClip = item as TimelineCurveClip;
                                itemWrapper = new TimelineClipCurveWrapper(curveClip, curveClip.Firetime, curveClip.Duration);
                                trackWrapper.AddItem(itemWrapper);
                            }
                            else if (item.GetType().IsSubclassOf(typeof(TimelineActionFixed)))
                            {
                                var fixedAction = item as TimelineActionFixed;
                                itemWrapper = new TimelineActionFixedWrapper(fixedAction, fixedAction.Firetime, fixedAction.Duration, fixedAction.InTime, fixedAction.OutTime, fixedAction.ItemLength);
                                trackWrapper.AddItem(itemWrapper);
                            }
                            else if (item.GetType().IsSubclassOf(typeof(TimelineAction)))
                            {
                                var action = item as TimelineAction;
                                itemWrapper = new TimelineActionWrapper(action, action.Firetime, action.Duration);
                                trackWrapper.AddItem(itemWrapper);
                            }
                            else
                            {
                                itemWrapper = new TimelineItemWrapper(item, item.Firetime);
                                trackWrapper.AddItem(itemWrapper);
                            }
                            trackWrapper.HasChanged = true;
                        }
                        else
                        {
                            if (GUIUtility.hotControl == 0)
                            {
                                if (itemWrapper.GetType() == typeof(TimelineClipCurveWrapper))
                                {
                                    var curveClip = item as TimelineCurveClip;
                                    var clipWrapper = itemWrapper as TimelineClipCurveWrapper;
                                    clipWrapper.fireTime = curveClip.Firetime;
                                    clipWrapper.Duration = curveClip.Duration;
                                }
                                else if (itemWrapper.GetType() == typeof(TimelineActionFixedWrapper))
                                {
                                    var actionFixed = item as TimelineActionFixed;
                                    var actionFixedWrapper = itemWrapper as TimelineActionFixedWrapper;
                                    actionFixedWrapper.fireTime = actionFixed.Firetime;
                                    actionFixedWrapper.Duration = actionFixed.Duration;
                                    actionFixedWrapper.InTime = actionFixed.InTime;
                                    actionFixedWrapper.OutTime = actionFixed.OutTime;
                                    actionFixedWrapper.ItemLength = actionFixed.ItemLength;
                                }
                                else if (itemWrapper.GetType() == (typeof(TimelineActionWrapper)))
                                {
                                    var action = item as TimelineAction;
                                    var actionWrapper = itemWrapper as TimelineActionWrapper;
                                    actionWrapper.fireTime = action.Firetime;
                                    actionWrapper.Duration = action.Duration;
                                }
                                else
                                {
                                    itemWrapper.fireTime = item.Firetime;
                                }
                            }
                        }
                    }
                    for (int j = trackWrapper.Children.Count - 1; j >= 0; j--)
                    {
                        var timelineItemWrap = trackWrapper.Children[j];
                        if (timelineItemWrap.timelineItem == null)
                        {
                            trackWrapper.Children.RemoveAt(j);
                        }
                    }
                }
            }
        }

        private static void CreateWrapper(Timeline timeline, TimelineControl control)
        {
            control.Wrapper = new TimelineWrapper(control)
            {
                timeline = timeline
            };
            UpdateParam(control.Wrapper, timeline);
            for (var i = 0; i < timeline.trackGroups.Count; i++)
            {
                var trackGroup = timeline.trackGroups[i];
                var timelineTrackGroupWrapper = new TimelineTrackGroupWrapper(trackGroup);
                control.Wrapper.AddTrackGroup(timelineTrackGroupWrapper);
                trackGroup.GetComponentsInChildren(true, trackGroup.timelineTracks);
                foreach (var track in trackGroup.timelineTracks)
                {
                    var trackWrapper = new TimelineTrackWrapper(track, timelineTrackGroupWrapper);
                    timelineTrackGroupWrapper.AddTrack(trackWrapper);
                    track.GetComponentsInChildren(true, track.timelineItems);
                    foreach (TimelineItem item in track.timelineItems)
                    {
                        if (item.GetType().IsSubclassOf(typeof(TimelineCurveClip)))
                        {
                            TimelineCurveClip curveClip = item as TimelineCurveClip;
                            TimelineClipCurveWrapper clipWrapper = new TimelineClipCurveWrapper(curveClip, curveClip.Firetime, curveClip.Duration);
                            trackWrapper.AddItem(clipWrapper);
                        }
                        else if (item.GetType().IsSubclassOf(typeof(TimelineActionFixed)))
                        {
                            TimelineActionFixed actionFixed = item as TimelineActionFixed;
                            TimelineActionFixedWrapper actionFixedWrapper = new TimelineActionFixedWrapper(actionFixed, actionFixed.Firetime, actionFixed.Duration, actionFixed.InTime, actionFixed.OutTime, actionFixed.ItemLength);
                            trackWrapper.AddItem(actionFixedWrapper);
                        }
                        else if (item.GetType().IsSubclassOf(typeof(TimelineAction)))
                        {
                            TimelineAction action = item as TimelineAction;
                            TimelineActionWrapper itemWrapper = new TimelineActionWrapper(action, action.Firetime, action.Duration);
                            trackWrapper.AddItem(itemWrapper);
                        }
                        else
                        {
                            TimelineItemWrapper itemWrapper = new TimelineItemWrapper(item, item.Firetime);
                            trackWrapper.AddItem(itemWrapper);
                        }
                    }
                }
            }
        }

        public static System.Type[] GetAllSubTypes(System.Type ParentType)
        {
            List<System.Type> list = new List<System.Type>();
            foreach (Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (System.Type type in a.GetTypes())
                {
                    if (type.IsSubclassOf(ParentType))
                    {
                        list.Add(type);
                    }
                }
            }
            return list.ToArray();
        }

        public enum TimeEnum
        {
            Minutes = 60,
            Seconds = 1
        }

        public static string GetTimelineItemName(GameObject parent, string name, System.Type type)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>();
            return GetTimelineItemName(children, name, type, 0);
        }

        public static string GetTimelineItemName(string name, Type type)
        {
            return GetTimelineItemName(name, type, 0);
        }

        private static string GetTimelineItemName(string name, Type type, int iteration)
        {
            var newName = name;
            if (iteration > 0)
            {
                newName = $"{name} {iteration}";
            }
            var isDuplicate = false;
            var items = UnityEngine.Object.FindObjectsOfType(type, true);
            foreach (var obj in items)
            {
                if (newName == obj.name)
                {
                    isDuplicate = true;
                }
            }

            if (isDuplicate)
            {
                return GetTimelineItemName(name, type, ++iteration);
            }
            return newName;
        }

        private static string GetTimelineItemName(Transform[] children, string name, System.Type type, int iteration)
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
                return GetTimelineItemName(children, name, type, ++iteration);
            }
            return newName;
        }

        public static Component[] GetValidComponents(GameObject actor)
        {
            return actor.GetComponents<Component>();
        }

        public static PropertyInfo[] getValidProperties(Component component)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (PropertyInfo propertyInfo in component.GetType().GetProperties())
            {
                if (UnityPropertyTypeInfo.GetMappedType(propertyInfo.PropertyType) != PropertyTypeInfo.None && propertyInfo.CanWrite)
                {
                    properties.Add(propertyInfo);
                }
            }
            return properties.ToArray();
        }

        public static FieldInfo[] getValidFields(Component component)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (FieldInfo field in component.GetType().GetFields())
            {
                if (UnityPropertyTypeInfo.GetMappedType(field.FieldType) != PropertyTypeInfo.None)
                {
                    fields.Add(field);
                }
            }
            return fields.ToArray();
        }

        public static MemberInfo[] GetValidMembers(Component component)
        {
            PropertyInfo[] properties = getValidProperties(component);
            FieldInfo[] fields = getValidFields(component);

            List<MemberInfo> members = new List<MemberInfo>();
            if (component.GetType() == typeof(Transform))
            {
                foreach (PropertyInfo propertyInfo in properties)
                {
                    if (propertyInfo.Name == "localPosition" || propertyInfo.Name == "localEulerAngles" || propertyInfo.Name == "localScale")
                    {
                        members.Add(propertyInfo);
                    }
                }
            }
            else
            {
                members.AddRange(properties);
                members.AddRange(fields);
            }
            return members.ToArray();
        }

        public static string GetUserFriendlyName(Component component, MemberInfo memberInfo)
        {
            return GetUserFriendlyName(component.GetType().Name, memberInfo.Name);
        }

        public static string GetUserFriendlyName(string componentName, string memberName)
        {
            string name = memberName;
            if (componentName == "Transform")
            {
                if (memberName == "localPosition")
                {
                    name = "Position";
                }
                else if (memberName == "localEulerAngles")
                {
                    name = "Rotation";
                }
                else if (memberName == "localScale")
                {
                    name = "Scale";
                }
            }
            else
            {
                //'camelCase' to 'Title Case'
                const string pattern = @"(?<=[^A-Z])(?=[A-Z])";
                name = Regex.Replace(memberName, pattern, " ", RegexOptions.None);
                name = name.Substring(0, 1).ToUpper() + name.Substring(1);
            }
            return name;
        }

        public static bool IsTrackItemValidForTrack(Behaviour behaviour, TimelineTrack track)
        {
            bool retVal = false;
            if (track.GetType() == (typeof(GlobalTrack)) || track.GetType().IsSubclassOf(typeof(GlobalTrack)))
            {
                if (behaviour.GetType().IsSubclassOf(typeof(TimelineGlobalAction)) || behaviour.GetType().IsSubclassOf(typeof(TimelineGlobalEvent)))
                {
                    retVal = true;
                }
            }
            else if (track.GetType() == (typeof(TimelineActorTrack)) || track.GetType().IsSubclassOf(typeof(TimelineActorTrack)))
            {
                if (behaviour.GetType().IsSubclassOf(typeof(TimelineActorAction)) || behaviour.GetType().IsSubclassOf(typeof(TimelineActorEvent)))
                {
                    retVal = true;
                }
            }
            else if (track.GetType() == (typeof(TimelineCurveTrack)) || track.GetType().IsSubclassOf(typeof(TimelineCurveTrack)))
            {
                if (behaviour.GetType() == typeof(TimelineActorCurveClip) || behaviour.GetType().IsSubclassOf(typeof(TimelineMaterialCurveTrack)))
                {
                    retVal = true;
                }
            }
            return retVal;
        }

        private static string CheckIllegalComponent(GameObject go, params Type[] excludeType)
        {
            var type = typeof(Transform);
            var components = go.GetComponents<Component>();
            for (var i = components.Length - 1; i >= 0; i--)
            {
                var t = components[i].GetType();
                if (t == type || t.IsSubclassOf(type))
                {
                    continue;
                }

                if (!excludeType.Any(item => t == item || t.IsSubclassOf(item)))
                {
                    return $"非法的组件{go.name}-{t.Name}";
                }

            }

            return components.Length == 2 ? null : $"非法的对象{go.name}";
        }

        [MenuItem("Window/Timeline/CheckAll")]
        public static void CheckAll()
        {
            foreach (var rootDir in RootDirList)
            {
                var assets = AssetDatabase.FindAssets("t:Prefab", new [] { "Assets/CustomResources/" + rootDir });
                foreach (var guid in assets)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    Check(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                }
            }
        }

        [MenuItem("Window/Timeline/CheckAllIllegalDir")]
        public static void CheckIllegalDir()
        {
            var assets = AssetDatabase.FindAssets("t:Prefab", IllegalDirList);
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.GetComponent<Timeline>())
                {
                    Error("该目录不能存放Timeline", path);
                }
            }
        }

        [MenuItem("Window/Timeline/Refresh")]
        public static void RefreshTimeline()
        {
            foreach (var rootDir in RootDirList)
            {
                var assets = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/CustomResources/" + rootDir });
                foreach (var guid in assets)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    Refresh(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Window/Timeline/Convert")]
        public static void Convert()
        {
            foreach (var rootDir in RootDirList)
            {
                var assets = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/CustomResources/" + rootDir });
                foreach (var guid in assets)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    Convert(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                }
            }
        }

        private static void Convert(GameObject timeline, string path)
        {
            var inst = Object.Instantiate(timeline);
            bool dirty = false;

            if (dirty)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(inst, path, InteractionMode.UserAction);
            }
            else
            {
                Object.DestroyImmediate(inst);
            }

        }

        private static void Refresh(GameObject timeline, string path)
        {
            var items = timeline.GetComponentsInChildren<TimelineItem>(true);
            foreach (var timelineItem in items)
            {
                timelineItem.Refresh();
            }
            EditorUtility.SetDirty(timeline);
        }

        private static void Error(string msg, string path = null)
        {
            if (path != null)
            {
                Debug.LogError($"{path}:{msg}");
                EditorUtility.DisplayDialog(path, msg, "ok");
            }
            else
            {
                Debug.LogError(msg);
                EditorUtility.DisplayDialog("Timeline", msg, "ok");
            }
        }

        private static bool ContainsChineseCharacters(string input)
        {
            var regex = new Regex("[\u4e00-\u9fa5]");
            return regex.IsMatch(input);
        }

        public static bool Check(GameObject go, string path = null)
        {
            var timeline = go.GetComponent<Timeline>();
            if (timeline == null)
                return true;
            if (ContainsChineseCharacters(go.name))
            {
                Error("不能包含中文", path);
                return false;
            }
            var transform = timeline.transform;
            var ret = CheckIllegalComponent(transform.gameObject, typeof(Timeline));
            if (ret != null)
            {
                Error(ret, path);
                return false;
            }
            for (var i = 0; i < transform.childCount; i++)
            {
                var group = transform.GetChild(i);
                ret = CheckIllegalComponent(group.gameObject, typeof(TrackGroup));
                if (ret != null)
                {
                    Error(ret, path);
                    return false;
                }
                for (var j = 0; j < group.childCount; j++)
                {
                    var track = group.GetChild(j);
                    ret = CheckIllegalComponent(track.gameObject, typeof(TimelineTrack));
                    if (ret != null)
                    {
                        Error(ret, path);
                        return false;
                    }
                    for (var k = 0; k < track.childCount; k++)
                    {
                        var item = track.GetChild(k);
                        ret = CheckIllegalComponent(item.gameObject, typeof(TimelineItem));
                        if (ret != null)
                        {
                            Error(ret, path);
                            return false;
                        }
                    }
                    var t = track.GetComponent<TimelineActorTrack>();
                    if (t != null)
                    {
                        var g = group.GetComponent<ActorTrackGroup>();
                        if (g == null)
                        {
                            Error($"非法的Parent{group.name}", path);
                            return false;
                        }
                    }
                }
            }
            return true;
        }



        public static readonly string[] RootDirList =
        {
            "GamePlay/LobbyScenePrefab",
            "GamePlay/project8/Prefab_Characters/Prefab_Lord",
            "GamePlay/project8/Prefab_Characters/Prefab_Sence",
            "GamePlay/project8/Prefab_Characters/Prefab_Common",
        };
        private static readonly string[] IllegalDirList = { "Assets/CustomResources/GamePlay/project8/OSG_Prefab_Effects" };
    }
}
