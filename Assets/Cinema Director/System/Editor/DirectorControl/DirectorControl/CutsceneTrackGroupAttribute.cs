using System;

public class CutsceneTrackGroupAttribute : Attribute
{
	private Type trackGroupType;

	public Type TrackGroupType
	{
		get
		{
			return this.trackGroupType;
		}
	}

	public CutsceneTrackGroupAttribute(Type type)
	{
		this.trackGroupType = type;
	}
}
