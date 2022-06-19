using System;
using System.Collections.Generic;

namespace CinemaDirector
{
	[Serializable]
	public class TimelineTrackWrapper : UnityBehaviourWrapper
	{
		private int ordinal;

		private Dictionary<TimelineItem, TimelineItemWrapper> itemMap = new Dictionary<TimelineItem, TimelineItemWrapper>();

		public IEnumerable<TimelineItemWrapper> Items
		{
			get
			{
				return itemMap.Values;
			}
		}

		public IEnumerable<TimelineItem> TimelineItems
		{
			get
			{
				return itemMap.Keys;
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

		public TimelineTrackWrapper(TimelineTrack behaviour) : base(behaviour)
		{
		}

		public void AddItem(TimelineItem behaviour, TimelineItemWrapper wrapper)
		{
			itemMap.Add(behaviour, wrapper);
		}

		public TimelineItemWrapper GetItemWrapper(TimelineItem timelineItem)
		{
			return itemMap[timelineItem];
		}

		public void RemoveItem(TimelineItem behaviour)
		{
			itemMap.Remove(behaviour);
		}

		public bool ContainsItem(TimelineItem behaviour, out TimelineItemWrapper itemWrapper)
		{
			return itemMap.TryGetValue(behaviour, out itemWrapper);
		}
	}
}