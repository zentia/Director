// Cinema Suite 2014

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

            TrackGroupAttribute[] tga = (TrackGroupAttribute[])trackGroup.GetType().GetCustomAttributes(typeof(TrackGroupAttribute), true);
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
                TimelineTrackAttribute[] customAttributes = (TimelineTrackAttribute[])subTypes[i].GetCustomAttributes(typeof(TimelineTrackAttribute), true);
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

            return allowedTrackTypes;
        }

        /// <summary>
        /// Returns a list of Cutscene Item types that are associated with the given Track.
        /// </summary>
        /// <param name="timelineTrack">The track to look up.</param>
        /// <returns>A list of valid cutscene item types.</returns>
        public static List<Type> GetAllowedItemTypes(TimelineTrack timelineTrack)
        {            
            // Get all the allowed Genres for this track group
            CutsceneItemGenre[] genres = new CutsceneItemGenre[0];

            TimelineTrackAttribute[] tta = (TimelineTrackAttribute[])timelineTrack.GetType().GetCustomAttributes(typeof(TimelineTrackAttribute), true);
            for (int i = 0; i < tta.Length; i++)
            {
                if (tta[i] != null)
                {
                    genres = tta[i].AllowedItemGenres;
                    break;
                }
            }

            Type[] subTypes = DirectorRuntimeHelper.GetAllSubTypes(typeof(TimelineItem));
            List<Type> allowedTrackTypes = new List<Type>();
            for (int i = 0; i < subTypes.Length; i++)
            {
                CutsceneItemAttribute[] customAttributes = (CutsceneItemAttribute[])subTypes[i].GetCustomAttributes(typeof(CutsceneItemAttribute), true);
                for (int j = 0; j < customAttributes.Length; j++)
                {
                    if (customAttributes[j] != null)
                    {
                        for (int k = 0; k < customAttributes[j].Genres.Length; k++)
                        {
                            CutsceneItemGenre genre = customAttributes[j].Genres[k];
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

            return allowedTrackTypes;
        }

        /// <summary>
        /// Get all Sub types from the given parent type.
        /// </summary>
        /// <param name="ParentType">The parent type</param>
        /// <returns>all children types of the parent.</returns>
        private static Type[] GetAllSubTypes(System.Type ParentType)
        {
            List<System.Type> list = new List<System.Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types = new Type[]{};
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogError("Cinema Director: Could not load types from assembly \"" + assemblies[i] + "\"\n" + e.Message + "\n" + e.StackTrace);
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j] != null && types[j].IsSubclassOf(ParentType))
                    {
                        list.Add(types[j]);
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
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                children.AddRange(GetAllTransformsInHierarchy(child));
                children.Add(child);
            }
            return children;
        }

    }
}
