// Author zentia
// Desc collect all types and cache
// Url https://zentia.github.io/2022/08/22/Engine/Unity/Timeline/
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TimelineRuntime
{
    public static class TimelineRuntimeHelper
    {
        public static List<Type> GetAllowedTrackTypes(TrackGroup trackGroup)
        {
            var type = trackGroup.GetType();
            if (s_CacheAllowedTrackTypes.ContainsKey(type))
            {
                return s_CacheAllowedTrackTypes[type];
            }

            TimelineTrackGenre[] genres = new TimelineTrackGenre[0];

            TimelineTrackGroupAttribute[] tga = GetTimelineTrackGroupAttributes(trackGroup.GetType(), true);
            for (int i = 0; i < tga.Length; i++)
            {
                if (tga[i] != null)
                {
                    genres = tga[i].AllowedTrackGenres;
                    break;
                }
            }

            Type[] subTypes = GetAllSubTypes(typeof(TimelineTrack));
            List<Type> allowedTrackTypes = new List<Type>();
            for (int i = 0; i < subTypes.Length; i++)
            {
                TimelineTrackAttribute[] customAttributes = GetTimelineTrackAttributes(subTypes[i], true);
                for (int j = 0; j < customAttributes.Length; j++)
                {
                    if (customAttributes[j] != null)
                    {
                        for (int k = 0; k < customAttributes[j].TrackGenres.Length; k++)
                        {
                            TimelineTrackGenre genre = customAttributes[j].TrackGenres[k];
                            for (int l = 0; l < genres.Length; l++)
                            {
                                if (genre == genres[l])
                                {
                                    allowedTrackTypes.Add(subTypes[i]);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            s_CacheAllowedTrackTypes.Add(type, allowedTrackTypes);
            return allowedTrackTypes;
        }

        public static List<Type> GetAllowedItemTypes(TimelineTrack timelineTrack)
        {
            var type = timelineTrack.GetType();
            if (s_CacheAllowedItemTypes.ContainsKey(type))
            {
                return s_CacheAllowedItemTypes[type];
            }
            TimelineItemGenre[] genres = new TimelineItemGenre[0];

            TimelineTrackAttribute[] tta = GetTimelineTrackAttributes(type, true);
            for (int i = 0; i < tta.Length; i++)
            {
                if (tta[i] != null)
                {
                    genres = tta[i].AllowedItemGenres;
                    break;
                }
            }

            Type[] subTypes = GetAllSubTypes(typeof(TimelineItem));
            List<Type> allowedItemTypes = new List<Type>();
            for (int i = 0; i < subTypes.Length; i++)
            {
                TimelineItemAttribute[] customAttributes = GetTimelineItemAttributes(subTypes[i], true);
                for (int j = 0; j < customAttributes.Length; j++)
                {
                    if (customAttributes[j] != null)
                    {
                        for (int k = 0; k < customAttributes[j].Genres.Length; k++)
                        {
                            TimelineItemGenre genre = customAttributes[j].Genres[k];
                            for (int l = 0; l < genres.Length; l++)
                            {
                                if (genre == genres[l])
                                {
                                    allowedItemTypes.Add(subTypes[i]);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            s_CacheAllowedItemTypes.Add(type, allowedItemTypes);
            return allowedItemTypes;
        }

        private static Type[] GetAllSubTypes(Type parentType)
        {
            if (s_CacheTypes.ContainsKey(parentType))
            {
                return s_CacheTypes[parentType];
            }
            var list = new List<Type>();
            Assembly[] assemblies = ReflectionHelper.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = ReflectionHelper.GetTypes(assemblies[i]);
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogErrorFormat("Could not load types from assembly \"{0}\"\n{1}\n{2}", assemblies[i], e.Message, e.StackTrace);
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j] != null && ReflectionHelper.IsSubclassOf(types[j], parentType))
                    {
                        list.Add(types[j]);
                    }
                }
            }

            var cacheType = list.ToArray();
            s_CacheTypes.Add(parentType, cacheType);
            return cacheType;
        }

        public static List<Transform> GetAllTransformsInHierarchy(Transform parent)
        {
            List<Transform> children = new List<Transform>();

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                children.AddRange(GetAllTransformsInHierarchy(child));
                children.Add(child);
            }
            return children;
        }

        private static TimelineTrackGroupAttribute[] GetTimelineTrackGroupAttributes(Type type, bool inherited)
        {
            if (s_TimelineTrackGroups.ContainsKey(type))
            {
                return s_TimelineTrackGroups[type];
            }
            var attributes = ReflectionHelper.GetCustomAttributes<TimelineTrackGroupAttribute>(type, inherited);
            s_TimelineTrackGroups.Add(type, attributes);
            return attributes;
        }

        private static TimelineTrackAttribute[] GetTimelineTrackAttributes(Type type, bool inherited)
        {
            if (s_TimelineTracks.ContainsKey(type))
            {
                return s_TimelineTracks[type];
            }

            var attributes = ReflectionHelper.GetCustomAttributes<TimelineTrackAttribute>(type, inherited);
            s_TimelineTracks.Add(type, attributes);
            return attributes;
        }

        private static TimelineItemAttribute[] GetTimelineItemAttributes(Type type, bool inherited)
        {
            if (s_TimelineItems.ContainsKey(type))
            {
                return s_TimelineItems[type];
            }

            var attributes = ReflectionHelper.GetCustomAttributes<TimelineItemAttribute>(type, inherited);
            s_TimelineItems.Add(type, attributes);
            return attributes;
        }

        private static Dictionary<Type, Type[]> s_CacheTypes = new();
        private static Dictionary<Type, List<Type>> s_CacheAllowedTrackTypes = new();
        private static Dictionary<Type, List<Type>> s_CacheAllowedItemTypes = new();
        private static Dictionary<Type, TimelineTrackGroupAttribute[]> s_TimelineTrackGroups = new();
        private static Dictionary<Type, TimelineTrackAttribute[]> s_TimelineTracks = new();
        private static Dictionary<Type, TimelineItemAttribute[]> s_TimelineItems = new();
        public static GameObject FindChildBFS(GameObject gameObj, string name, bool onlyFindActive = false)
        {
            if (gameObj == null)
                return null;

            List<GameObject> BfsList = new List<GameObject>();
            BfsList.Add(gameObj);

            GameObject objFound = null;
            int index = 0;
            while (index < BfsList.Count)
            {
                var go = BfsList[index++];

                if (go.name == name && (!onlyFindActive || go.activeInHierarchy))
                {
                    objFound = go;
                    break;
                }

                int childCount = go.transform.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    BfsList.Add(child);
                }
            }

            BfsList.Clear();
            return objFound;
        }
    }
}
