using CinemaSuite.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CinemaDirector
{
    /// <summary>
    /// A helper class for getting useful data from Director Runtime objects.
    /// </summary>
    public static class DirectorRuntimeHelper
    {
        /// <summary>
        /// Returns a list of Track types that are associated with the given Track Group.
        /// </summary>
        /// <param name="trackGroup">The track group to be inspected</param>
        /// <returns>A list of track types that meet the genre criteria of the given track group.</returns>
        public static List<Type> GetAllowedTrackTypes(TrackGroup trackGroup)
        {
            // Get all the allowed Genres for this track group
            TimelineTrackGenre[] genres = new TimelineTrackGenre[0];

            foreach (TrackGroupAttribute attribute in ReflectionHelper.GetCustomAttributes<TrackGroupAttribute>(trackGroup.GetType(), true))
            {
                if (attribute != null)
                {
                    genres = attribute.AllowedTrackGenres;
                    break;
                }
            }

            List<Type> allowedTrackTypes = new List<Type>();
            foreach (Type type in GetAllSubTypes(typeof(TimelineTrack)))
            {
                foreach (TimelineTrackAttribute attribute in ReflectionHelper.GetCustomAttributes<TimelineTrackAttribute>(type, true))
                {
                    if (attribute != null)
                    {
                        foreach (TimelineTrackGenre genre in attribute.TrackGenres)
                        {
                            foreach (TimelineTrackGenre genre2 in genres)
                            {
                                if (genre == genre2)
                                {
                                    allowedTrackTypes.Add(type);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return allowedTrackTypes;
        }

        /// <summary>
        /// Returns a list of Cutscene Item types that are associated with the given Track.
        /// </summary>
        /// <param name="timelineTrack">The track to look up.</param>
        /// <returns>A list of valid cutscene item types.</returns>
        public static List<Type> GetAllowedItemTypes(TimelineTrack timelineTrack)
        {
            // Get all the allowed Genres for this track
            CutsceneItemGenre[] genres = new CutsceneItemGenre[0];
            
            foreach (TimelineTrackAttribute attribute in ReflectionHelper.GetCustomAttributes<TimelineTrackAttribute>(timelineTrack.GetType(), true))
            {
                if (attribute != null)
                {
                    genres = attribute.AllowedItemGenres;
                    break;
                }
            }

            List<Type> allowedItemTypes = new List<Type>();
            foreach (Type type in GetAllSubTypes(typeof(TimelineItem)))
            {
                foreach (CutsceneItemAttribute attribute in ReflectionHelper.GetCustomAttributes<CutsceneItemAttribute>(type, true))
                {
                    if (attribute != null)
                    {
                        foreach (CutsceneItemGenre genre in attribute.Genres)
                        {
                            foreach (CutsceneItemGenre genre2 in genres)
                            {
                                if (genre == genre2)
                                {
                                    allowedItemTypes.Add(type);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return allowedItemTypes;
        }

        /// <summary>
        /// Get all Sub types from the given parent type.
        /// </summary>
        /// <param name="ParentType">The parent type</param>
        /// <returns>all children types of the parent.</returns>
        private static Type[] GetAllSubTypes(Type ParentType)
        {
            List<Type> list = new List<Type>();
            foreach (Assembly a in ReflectionHelper.GetAssemblies())
            {
                foreach (Type type in ReflectionHelper.GetTypes(a))
                {
                    if (type != null && ReflectionHelper.IsSubclassOf(type, ParentType))
                    {
                        list.Add(type);
                    }
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// Retrieve all children of a parent Transform recursively.
        /// </summary>
        /// <param name="parent">The parent transform</param>
        /// <returns>All children of that parent.</returns>
        public static List<Transform> GetAllTransformsInHierarchy(Transform parent)
        {
            List<Transform> children = new List<Transform>();

            foreach (Transform child in parent)
            {
                children.AddRange(GetAllTransformsInHierarchy(child));
                children.Add(child);
            }
            return children;
        }

        static GameObject m_Root;
        public static GameObject Root
        {
            get 
            {
                
                return m_Root;
            }
        }
        public static void DestroyRoot()
        {
            if (m_Root != null)
            {
                UnityEngine.Object.Destroy(m_Root);
            }
        }
        public static GameObject FindSceneObject(string name)
        {
            var objects = GameObject.FindGameObjectsWithTag("Cutscene");
            if (objects != null && objects.Length > 0)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i].name == name)
                    {
                        return objects[i];
                    }
                }
            }
            return null;
        }

        public static void SetLayer(GameObject gameObject, int layer)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>();
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].gameObject.layer = layer;
                }
            }
        }
    }
}
