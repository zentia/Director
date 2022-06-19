using System.Collections.Generic;

namespace CinemaDirector
{
	public class CutsceneWrapper : UnityBehaviourWrapper
	{
		private readonly Dictionary<DirectorObject, TrackGroupWrapper> TrackGroupMap = new Dictionary<DirectorObject, TrackGroupWrapper>();
        private bool isPlaying;

        public float Duration { get; set; }

        public float RunningTime { get; set; }

        public bool IsPlaying
		{
			get
			{
				return isPlaying;
			}
			set
			{
				isPlaying = value;
			}
		}

		public IEnumerable<TrackGroupWrapper> TrackGroups
		{
			get
			{
				return TrackGroupMap.Values;
			}
		}

		public IEnumerable<DirectorObject> Behaviours
		{
			get
			{
				return TrackGroupMap.Keys;
			}
		}

		public CutsceneWrapper(DirectorObject behaviour) : base(behaviour)
		{
		}

		public void AddTrackGroup(DirectorObject behaviour, TrackGroupWrapper wrapper)
		{
			TrackGroupMap.Add(behaviour, wrapper);
		}

		public TrackGroupWrapper GetTrackGroupWrapper(DirectorObject behaviour)
		{
			return TrackGroupMap[behaviour];
		}

		public void RemoveTrackGroup(DirectorObject behaviour)
		{
			TrackGroupMap.Remove(behaviour);
		}

		public bool ContainsTrackGroup(DirectorObject behaviour, out TrackGroupWrapper trackGroupWrapper)
		{
			return TrackGroupMap.TryGetValue(behaviour, out trackGroupWrapper);
		}
	}
}