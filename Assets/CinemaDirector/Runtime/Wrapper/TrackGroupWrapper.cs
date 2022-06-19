using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
	[Serializable]
	public class TrackGroupWrapper : UnityBehaviourWrapper
	{
		[SerializeField]
		private int ordinal;

		private Dictionary<TimelineTrack, TimelineTrackWrapper> trackMap = new Dictionary<TimelineTrack, TimelineTrackWrapper>();

		public IEnumerable<TimelineTrackWrapper> Tracks
		{
			get
			{
				return trackMap.Values;
			}
		}

		public IEnumerable<TimelineTrack> Behaviours
		{
			get
			{
				return trackMap.Keys;
			}
		}

		public int Ordinal
		{
			get
			{
				return ordinal;
			}
			set
			{
				ordinal = value;
			}
		}

		public TrackGroupWrapper(TrackGroup behaviour) : base(behaviour)
		{
		}

		public void AddTrack(TimelineTrack behaviour, TimelineTrackWrapper wrapper)
		{
			trackMap.Add(behaviour, wrapper);
		}

		public TimelineTrackWrapper GetTrackWrapper(TimelineTrack behaviour)
		{
			return trackMap[behaviour];
		}

		public void RemoveTrack(TimelineTrack behaviour)
		{
			trackMap.Remove(behaviour);
		}

		public bool ContainsTrack(TimelineTrack behaviour, out TimelineTrackWrapper trackWrapper)
		{
			return trackMap.TryGetValue(behaviour, out trackWrapper);
		}
	}
}