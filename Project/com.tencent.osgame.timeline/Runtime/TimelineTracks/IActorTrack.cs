
using System.Collections.Generic;
using UnityEngine;
namespace TimelineRuntime
{
    /// <summary>
    /// Interface to implement with any track that is made to be held in an actor track group.
    /// </summary>
    public interface IActorTrack
    {
        /// <summary>
        /// Get the single Actor associated with this Actor Track.
        /// </summary>
        Transform Actor
        {
            get;
        }

        List<Transform> Actors
        {
            get;
        }
    }
}
