using System.Collections.Generic;
using UnityEngine;

public class TrackGroupWrapper : UnityBehaviourWrapper
{
	private int ordinal;

	private Dictionary<Behaviour, TimelineTrackWrapper> trackMap = new Dictionary<Behaviour, TimelineTrackWrapper>();

	public IEnumerable<TimelineTrackWrapper> Tracks
	{
		get
		{
			return trackMap.Values;
		}
	}

	public IEnumerable<Behaviour> Behaviours
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

	public TrackGroupWrapper(Behaviour behaviour) : base(behaviour)
	{
	}

	public void AddTrack(Behaviour behaviour, TimelineTrackWrapper wrapper)
	{
		trackMap.Add(behaviour, wrapper);
	}

	public TimelineTrackWrapper GetTrackWrapper(Behaviour behaviour)
	{
		return trackMap[behaviour];
	}

	public void RemoveTrack(Behaviour behaviour)
	{
		trackMap.Remove(behaviour);
	}

	public bool ContainsTrack(Behaviour behaviour, out TimelineTrackWrapper trackWrapper)
	{
		return trackMap.TryGetValue(behaviour, out trackWrapper);
	}
}
