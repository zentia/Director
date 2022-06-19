using System;

namespace CinemaDirector
{
	public class CutsceneItemControlAttribute : Attribute
	{
		private Type itemType;

		private int drawPriority;

		public Type ItemType
		{
			get
			{
				return itemType;
			}
		}

		public int DrawPriority
		{
			get
			{
				return drawPriority;
			}
		}

		public CutsceneItemControlAttribute(Type type, int drawPriority)
		{
			itemType = type;
			this.drawPriority = drawPriority;
		}

		public CutsceneItemControlAttribute(Type type)
		{
			itemType = type;
			drawPriority = 0;
		}
	}
}