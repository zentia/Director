using System;

namespace CinemaDirector
{
	public class CutsceneTrackAttribute : Attribute
	{
		private Type trackType;

		public Type TrackType
		{
			get
			{
				return trackType;
			}
		}

		public CutsceneTrackAttribute(Type type)
		{
			trackType = type;
		}
	}
}