using System;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneWrapper : UnityBehaviourWrapper
{
	private Dictionary<Behaviour, TrackGroupWrapper> TrackGroupMap = new Dictionary<Behaviour, TrackGroupWrapper>();

	private float duration;

	private float runningTime;

	private bool isPlaying;

	public float Duration
	{
		get
		{
			return this.duration;
		}
		set
		{
			this.duration = value;
		}
	}

	public float RunningTime
	{
		get
		{
			return this.runningTime;
		}
		set
		{
			this.runningTime = value;
		}
	}

	public bool IsPlaying
	{
		get
		{
			return this.isPlaying;
		}
		set
		{
			this.isPlaying = value;
		}
	}

	public IEnumerable<TrackGroupWrapper> TrackGroups
	{
		get
		{
			return this.TrackGroupMap.Values;
		}
	}

	public IEnumerable<Behaviour> Behaviours
	{
		get
		{
			return this.TrackGroupMap.Keys;
		}
	}

	public CutsceneWrapper(Behaviour behaviour) : base(behaviour)
	{
	}

	public void AddTrackGroup(Behaviour behaviour, TrackGroupWrapper wrapper)
	{
		this.TrackGroupMap.Add(behaviour, wrapper);
	}

	public TrackGroupWrapper GetTrackGroupWrapper(Behaviour behaviour)
	{
		return this.TrackGroupMap[behaviour];
	}

	public void RemoveTrackGroup(Behaviour behaviour)
	{
		this.TrackGroupMap.Remove(behaviour);
	}

	public bool ContainsTrackGroup(Behaviour behaviour, out TrackGroupWrapper trackGroupWrapper)
	{
		return this.TrackGroupMap.TryGetValue(behaviour, out trackGroupWrapper);
	}
}
