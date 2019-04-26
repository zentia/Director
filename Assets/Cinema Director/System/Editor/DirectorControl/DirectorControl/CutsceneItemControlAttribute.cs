using System;

public class CutsceneItemControlAttribute : Attribute
{
	private Type itemType;

	private int drawPriority;

	public Type ItemType
	{
		get
		{
			return this.itemType;
		}
	}

	public int DrawPriority
	{
		get
		{
			return this.drawPriority;
		}
	}

	public CutsceneItemControlAttribute(Type type, int drawPriority)
	{
		this.itemType = type;
		this.drawPriority = drawPriority;
	}

	public CutsceneItemControlAttribute(Type type)
	{
		this.itemType = type;
		this.drawPriority = 0;
	}
}
