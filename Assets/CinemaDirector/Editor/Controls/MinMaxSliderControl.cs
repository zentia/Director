using System;
using UnityEngine;

internal class MinMaxSliderControl
{
	private class MinMaxSliderState
	{
		public float dragEndLimit;

		public float dragStartLimit;

		public float dragStartPos;

		public float dragStartSize;

		public float dragStartValue;

		public float dragStartValuesPerPixel;

		public int whereWeDrag = -1;
	}

	private static int firstScrollWait = 250;

	private static int kFirstScrollWait = 250;

	private static int kScrollWait = 30;

	private static float nextScrollStepTime = 0f;

	private static int repeatButtonHash = "repeatButton".GetHashCode();

	internal static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();

	private static MinMaxSliderControl.MinMaxSliderState s_MinMaxSliderState;

	private static DateTime s_NextScrollStepTime = DateTime.Now;

	private static int scrollControlID;

	private static int scrollWait = 30;

	internal static void DoMinMaxSlider(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
	{
		Event current = Event.current;
		bool flag = size == 0f;
		float num = Mathf.Min(visualStart, visualEnd);
		float num2 = Mathf.Max(visualStart, visualEnd);
		float num3 = Mathf.Min(startLimit, endLimit);
		float num4 = Mathf.Max(startLimit, endLimit);
		MinMaxSliderControl.MinMaxSliderState minMaxSliderState = MinMaxSliderControl.s_MinMaxSliderState;
		if (GUIUtility.hotControl == id && minMaxSliderState != null)
		{
			num = minMaxSliderState.dragStartLimit;
			num3 = minMaxSliderState.dragStartLimit;
			num2 = minMaxSliderState.dragEndLimit;
			num4 = minMaxSliderState.dragEndLimit;
		}
		float num5 = 0f;
		float num6 = Mathf.Clamp(value, num, num2);
		float num7 = Mathf.Clamp(value + size, num, num2) - num6;
		float num8 = (visualStart <= visualEnd) ? 1f : -1f;
		if (slider == null || thumb == null)
		{
			return;
		}
		float num10;
		Rect rect;
		Rect rect2;
		Rect rect3;
		float num11;
		if (horiz)
		{
			float num9 = (thumb.fixedWidth == 0f) ? ((float)thumb.padding.horizontal) : thumb.fixedWidth;
			num10 = (position.width - (float)slider.padding.horizontal - num9) / (num2 - num);
			rect = new Rect((num6 - num) * num10 + position.x + (float)slider.padding.left, position.y + (float)slider.padding.top, num7 * num10 + num9, position.height - (float)slider.padding.vertical);
			rect2 = new Rect(rect.x, rect.y, (float)thumb.padding.left, rect.height);
			rect3 = new Rect(rect.xMax - (float)thumb.padding.right, rect.y, (float)thumb.padding.right, rect.height);
			num11 = current.mousePosition.x - position.x;
		}
		else
		{
			float num12 = (thumb.fixedHeight == 0f) ? ((float)thumb.padding.vertical) : thumb.fixedHeight;
			num10 = (position.height - (float)slider.padding.vertical - num12) / (num2 - num);
			rect = new Rect(position.x + (float)slider.padding.left, (num6 - num) * num10 + position.y + (float)slider.padding.top, position.width - (float)slider.padding.horizontal, num7 * num10 + num12);
			rect2 = new Rect(rect.x, rect.y, rect.width, (float)thumb.padding.top);
			rect3 = new Rect(rect.x, rect.yMax - (float)thumb.padding.bottom, rect.width, (float)thumb.padding.bottom);
			num11 = current.mousePosition.y - position.y;
		}
		switch (current.GetTypeForControl(id))
		{
		case EventType.MouseDown:
			if (!position.Contains(current.mousePosition) || num - num2 == 0f)
			{
				return;
			}
			if (minMaxSliderState == null)
			{
				minMaxSliderState = (MinMaxSliderControl.s_MinMaxSliderState = new MinMaxSliderControl.MinMaxSliderState());
			}
			if (rect.Contains(current.mousePosition))
			{
				minMaxSliderState.dragStartPos = num11;
				minMaxSliderState.dragStartValue = value;
				minMaxSliderState.dragStartSize = size;
				minMaxSliderState.dragStartValuesPerPixel = num10;
				minMaxSliderState.dragStartLimit = startLimit;
				minMaxSliderState.dragEndLimit = endLimit;
				if (rect2.Contains(current.mousePosition))
				{
					minMaxSliderState.whereWeDrag = 1;
				}
				else if (rect3.Contains(current.mousePosition))
				{
					minMaxSliderState.whereWeDrag = 2;
				}
				else
				{
					minMaxSliderState.whereWeDrag = 0;
				}
				GUIUtility.hotControl=(id);
				current.Use();
				return;
			}
			if (slider != GUIStyle.none)
			{
				if (size != 0f & flag)
				{
					if (horiz)
					{
						if (num11 > rect.xMax - position.x)
						{
							value += size * num8 * 0.9f;
						}
						else
						{
							value -= size * num8 * 0.9f;
						}
					}
					else if (num11 > rect.yMax - position.y)
					{
						value += size * num8 * 0.9f;
					}
					else
					{
						value -= size * num8 * 0.9f;
					}
					minMaxSliderState.whereWeDrag = 0;
					GUI.changed=(true);
					MinMaxSliderControl.s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double)MinMaxSliderControl.kFirstScrollWait);
					float num13 = (!horiz) ? current.mousePosition.y : current.mousePosition.x;
					float num14 = (!horiz) ? rect.y : rect.x;
					minMaxSliderState.whereWeDrag = ((num13 <= num14) ? 3 : 4);
				}
				else
				{
					if (horiz)
					{
						value = (num11 - rect.width * 0.5f) / num10 + num - size * 0.5f;
					}
					else
					{
						value = (num11 - rect.height * 0.5f) / num10 + num - size * 0.5f;
					}
					minMaxSliderState.dragStartPos = num11;
					minMaxSliderState.dragStartValue = value;
					minMaxSliderState.dragStartSize = size;
					minMaxSliderState.whereWeDrag = 0;
					GUI.changed=(true);
				}
				GUIUtility.hotControl=(id);
				value = Mathf.Clamp(value, num3, num4 - size);
				current.Use();
			}
			return;
		case EventType.MouseUp:
			if (GUIUtility.hotControl == id)
			{
				current.Use();
				GUIUtility.hotControl=(0);
			}
			return;
		case EventType.MouseDrag:
			if (GUIUtility.hotControl == id)
			{
				float num15 = (num11 - minMaxSliderState.dragStartPos) / minMaxSliderState.dragStartValuesPerPixel;
				switch (minMaxSliderState.whereWeDrag)
				{
				case 0:
					value = Mathf.Clamp(minMaxSliderState.dragStartValue + num15, num3, num4 - size);
					break;
				case 1:
					value = minMaxSliderState.dragStartValue + num15;
					size = minMaxSliderState.dragStartSize - num15;
					if (value < num3)
					{
						size -= num3 - value;
						value = num3;
					}
					if (size < num5)
					{
						value -= num5 - size;
						size = num5;
					}
					break;
				case 2:
					size = minMaxSliderState.dragStartSize + num15;
					if (value + size > num4)
					{
						size = num4 - value;
					}
					if (size < num5)
					{
						size = num5;
					}
					break;
				}
				GUI.changed=(true);
				current.Use();
				return;
			}
			return;
		case EventType.Repaint:
			slider.Draw(position, GUIContent.none, id);
			thumb.Draw(rect, GUIContent.none, id);
			if (GUIUtility.hotControl != id || !position.Contains(current.mousePosition) || num - num2 == 0f)
			{
				return;
			}
			if (rect.Contains(current.mousePosition))
			{
				if (minMaxSliderState != null && (minMaxSliderState.whereWeDrag == 3 || minMaxSliderState.whereWeDrag == 4))
				{
					GUIUtility.hotControl=(0);
				}
				return;
			}
			if (DateTime.Now >= MinMaxSliderControl.s_NextScrollStepTime)
			{
				float num13 = (!horiz) ? current.mousePosition.y : current.mousePosition.x;
				float num14 = (!horiz) ? rect.y : rect.x;
				if (((num13 <= num14) ? 3 : 4) != minMaxSliderState.whereWeDrag)
				{
					return;
				}
				if (size != 0f & flag)
				{
					if (horiz)
					{
						if (num11 > rect.xMax - position.x)
						{
							value += size * num8 * 0.9f;
						}
						else
						{
							value -= size * num8 * 0.9f;
						}
					}
					else if (num11 > rect.yMax - position.y)
					{
						value += size * num8 * 0.9f;
					}
					else
					{
						value -= size * num8 * 0.9f;
					}
					minMaxSliderState.whereWeDrag = -1;
					GUI.changed=(true);
				}
				value = Mathf.Clamp(value, num3, num4 - size);
				MinMaxSliderControl.s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double)MinMaxSliderControl.kScrollWait);
			}
			return;
		default:
			return;
		}
	}

	private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
	{
		int controlID = GUIUtility.GetControlID(MinMaxSliderControl.repeatButtonHash, focusType, position);
		var typeForControl = Event.current.GetTypeForControl(controlID);
		if (typeForControl == EventType.MouseDown)
		{
			if (position.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl=(controlID);
				Event.current.Use();
			}
			return false;
		}
		if ((int)typeForControl != 1)
		{
			if ((int)typeForControl != 7)
			{
				return false;
			}
			style.Draw(position, content, controlID);
			return controlID == GUIUtility.hotControl && position.Contains(Event.current.mousePosition);
		}
		else
		{
			if (GUIUtility.hotControl == controlID)
			{
				GUIUtility.hotControl=(0);
				Event.current.Use();
				return position.Contains(Event.current.mousePosition);
			}
			return false;
		}
	}

	public static void MinMaxScroller(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
	{
		float num;
		if (horiz)
		{
			num = size * 10f / position.width;
		}
		else
		{
			num = size * 10f / position.height;
		}
		Rect position2;
		Rect rect;
		Rect rect2;
		if (horiz)
		{
			position2 = new Rect(position.x + leftButton.fixedWidth, position.y, position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height);
			rect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
			rect2 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
		}
		else
		{
			position2 = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight);
			rect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
			rect2 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
		}
		float num2 = Mathf.Min(visualStart, value);
		float num3 = Mathf.Max(visualEnd, value + size);
		MinMaxSliderControl.MinMaxSlider(position2, ref value, ref size, num2, num3, num2, num3, slider, thumb, horiz);
		bool flag = false;
		if (Event.current.type == EventType.MouseUp)
		{
			flag = true;
		}
		if (MinMaxSliderControl.ScrollerRepeatButton(id, rect, leftButton))
		{
			value -= num * ((visualStart >= visualEnd) ? -1f : 1f);
		}
		if (MinMaxSliderControl.ScrollerRepeatButton(id, rect2, rightButton))
		{
			value += num * ((visualStart >= visualEnd) ? -1f : 1f);
		}
		if (flag && Event.current.type == EventType.Used)
		{
			MinMaxSliderControl.scrollControlID = 0;
		}
		if (startLimit < endLimit)
		{
			value = Mathf.Clamp(value, startLimit, endLimit - size);
			return;
		}
		value = Mathf.Clamp(value, endLimit, startLimit - size);
	}

	public static void MinMaxSlider(Rect position, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
	{
		MinMaxSliderControl.DoMinMaxSlider(position, GUIUtility.GetControlID(MinMaxSliderControl.s_MinMaxSliderHash, (FocusType)2), ref value, ref size, visualStart, visualEnd, startLimit, endLimit, slider, thumb, horiz);
	}

	private static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
	{
		bool result = false;
		if (MinMaxSliderControl.DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
		{
			bool arg_22_0 = MinMaxSliderControl.scrollControlID != scrollerID;
			MinMaxSliderControl.scrollControlID = scrollerID;
			if (arg_22_0)
			{
				result = true;
				MinMaxSliderControl.nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)MinMaxSliderControl.firstScrollWait;
			}
			else if (Time.realtimeSinceStartup >= MinMaxSliderControl.nextScrollStepTime)
			{
				result = true;
				MinMaxSliderControl.nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)MinMaxSliderControl.scrollWait;
			}
		}
		return result;
	}
}
