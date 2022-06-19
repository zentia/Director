using System;

namespace CinemaDirector
{
	public class CutsceneTrackGroupAttribute : Attribute
	{
		private Type trackGroupType;

		public Type TrackGroupType
		{
			get
			{
				return trackGroupType;
			}
		}

		public CutsceneTrackGroupAttribute(Type type)
		{
			trackGroupType = type;
		}
	}
}