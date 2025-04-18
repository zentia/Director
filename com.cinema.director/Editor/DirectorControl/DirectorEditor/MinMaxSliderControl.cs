using System;
using UnityEngine;

internal class MinMaxSliderControl
{
    private static int firstScrollWait = 250;
    private static int kFirstScrollWait = 250;
    private static int kScrollWait = 30;
    private static float nextScrollStepTime = 0f;
    private static int repeatButtonHash = "repeatButton".GetHashCode();
    internal static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();
    private static MinMaxSliderState s_MinMaxSliderState;
    private static DateTime s_NextScrollStepTime = DateTime.Now;
    private static int scrollControlID;
    private static int scrollWait = 30;

    internal static void DoMinMaxSlider(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
    {
        Event current = Event.current;
        bool flag = size == 0f;
        float min = Mathf.Min(visualStart, visualEnd);
        float max = Mathf.Max(visualStart, visualEnd);
        float dragStartLimit = Mathf.Min(startLimit, endLimit);
        float dragEndLimit = Mathf.Max(startLimit, endLimit);
        MinMaxSliderState state = s_MinMaxSliderState;
        if ((GUIUtility.hotControl == id) && (state != null))
        {
            min = state.dragStartLimit;
            dragStartLimit = state.dragStartLimit;
            max = state.dragEndLimit;
            dragEndLimit = state.dragEndLimit;
        }
        float num9 = 0f;
        float num10 = Mathf.Clamp(value, min, max);
        float num11 = Mathf.Clamp(value + size, min, max) - num10;
        float num12 = (visualStart <= visualEnd) ? 1f : -1f;
        if ((slider != null) && (thumb != null))
        {
            float num;
            float num2;
            Rect rect;
            Rect rect2;
            Rect rect3;
            float num3;
            float num4;
            if (horiz)
            {
                float num13 = (thumb.fixedWidth == 0f) ? ((float) thumb.padding.horizontal) : thumb.fixedWidth;
                num = ((position.width - slider.padding.horizontal) - num13) / (max - min);
                rect = new Rect((((num10 - min) * num) + position.x) + slider.padding.left, position.y + slider.padding.top, (num11 * num) + num13, position.height - slider.padding.vertical);
                rect2 = new Rect(rect.x, rect.y, (float) thumb.padding.left, rect.height);
                rect3 = new Rect(rect.xMax - thumb.padding.right, rect.y, (float) thumb.padding.right, rect.height);
                num2 = current.mousePosition.x - position.x;
            }
            else
            {
                float num14 = (thumb.fixedHeight == 0f) ? ((float) thumb.padding.vertical) : thumb.fixedHeight;
                num = ((position.height - slider.padding.vertical) - num14) / (max - min);
                rect = new Rect(position.x + slider.padding.left, (((num10 - min) * num) + position.y) + slider.padding.top, position.width - slider.padding.horizontal, (num11 * num) + num14);
                rect2 = new Rect(rect.x, rect.y, rect.width, (float) thumb.padding.top);
                rect3 = new Rect(rect.x, rect.yMax - thumb.padding.bottom, rect.width, (float) thumb.padding.bottom);
                num2 = current.mousePosition.y - position.y;
            }
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && ((min - max) != 0f))
                    {
                        if (state == null)
                        {
                            state = s_MinMaxSliderState = new MinMaxSliderState();
                        }
                        if (!rect.Contains(current.mousePosition))
                        {
                            if (slider != GUIStyle.none)
                            {
                                if ((size != 0f) & flag)
                                {
                                    if (horiz)
                                    {
                                        if (num2 > (rect.xMax - position.x))
                                        {
                                            value += (size * num12) * 0.9f;
                                        }
                                        else
                                        {
                                            value -= (size * num12) * 0.9f;
                                        }
                                    }
                                    else if (num2 > (rect.yMax - position.y))
                                    {
                                        value += (size * num12) * 0.9f;
                                    }
                                    else
                                    {
                                        value -= (size * num12) * 0.9f;
                                    }
                                    state.whereWeDrag = 0;
                                    GUI.changed = true;
                                    s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double) kFirstScrollWait);
                                    num3 = !horiz ? current.mousePosition.y : current.mousePosition.x;
                                    num4 = !horiz ? rect.y : rect.x;
                                    state.whereWeDrag = (num3 <= num4) ? 3 : 4;
                                }
                                else
                                {
                                    if (horiz)
                                    {
                                        value = (((num2 - (rect.width * 0.5f)) / num) + min) - (size * 0.5f);
                                    }
                                    else
                                    {
                                        value = (((num2 - (rect.height * 0.5f)) / num) + min) - (size * 0.5f);
                                    }
                                    state.dragStartPos = num2;
                                    state.dragStartValue = value;
                                    state.dragStartSize = size;
                                    state.whereWeDrag = 0;
                                    GUI.changed = true;
                                }
                                GUIUtility.hotControl = id;
                                value = Mathf.Clamp(value, dragStartLimit, dragEndLimit - size);
                                current.Use();
                            }
                            return;
                        }
                        state.dragStartPos = num2;
                        state.dragStartValue = value;
                        state.dragStartSize = size;
                        state.dragStartValuesPerPixel = num;
                        state.dragStartLimit = startLimit;
                        state.dragEndLimit = endLimit;
                        if (rect2.Contains(current.mousePosition))
                        {
                            state.whereWeDrag = 1;
                        }
                        else if (rect3.Contains(current.mousePosition))
                        {
                            state.whereWeDrag = 2;
                        }
                        else
                        {
                            state.whereWeDrag = 0;
                        }
                        GUIUtility.hotControl = id;
                        current.Use();
                    }
                    return;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        current.Use();
                        GUIUtility.hotControl = 0;
                    }
                    return;

                case EventType.MouseMove:
                case EventType.KeyDown:
                case EventType.KeyUp:
                case EventType.ScrollWheel:
                    return;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        float num15 = (num2 - state.dragStartPos) / state.dragStartValuesPerPixel;
                        switch (state.whereWeDrag)
                        {
                            case 0:
                                value = Mathf.Clamp(state.dragStartValue + num15, dragStartLimit, dragEndLimit - size);
                                break;

                            case 1:
                                value = state.dragStartValue + num15;
                                size = state.dragStartSize - num15;
                                if (value < dragStartLimit)
                                {
                                    size -= dragStartLimit - value;
                                    value = dragStartLimit;
                                }
                                if (size < num9)
                                {
                                    value -= num9 - size;
                                    size = num9;
                                }
                                break;

                            case 2:
                                size = state.dragStartSize + num15;
                                if ((value + size) > dragEndLimit)
                                {
                                    size = dragEndLimit - value;
                                }
                                if (size < num9)
                                {
                                    size = num9;
                                }
                                break;
                        }
                        GUI.changed = true;
                        current.Use();
                    }
                    return;

                case EventType.Repaint:
                    slider.Draw(position, GUIContent.none, id);
                    thumb.Draw(rect, GUIContent.none, id);
                    if (((GUIUtility.hotControl == id) && position.Contains(current.mousePosition)) && ((min - max) != 0f))
                    {
                        if (!rect.Contains(current.mousePosition))
                        {
                            if (DateTime.Now >= s_NextScrollStepTime)
                            {
                                num3 = !horiz ? current.mousePosition.y : current.mousePosition.x;
                                num4 = !horiz ? rect.y : rect.x;
                                if (((num3 <= num4) ? 3 : 4) != state.whereWeDrag)
                                {
                                    return;
                                }
                                if ((size != 0f) & flag)
                                {
                                    if (horiz)
                                    {
                                        if (num2 > (rect.xMax - position.x))
                                        {
                                            value += (size * num12) * 0.9f;
                                        }
                                        else
                                        {
                                            value -= (size * num12) * 0.9f;
                                        }
                                    }
                                    else if (num2 > (rect.yMax - position.y))
                                    {
                                        value += (size * num12) * 0.9f;
                                    }
                                    else
                                    {
                                        value -= (size * num12) * 0.9f;
                                    }
                                    state.whereWeDrag = -1;
                                    GUI.changed = true;
                                }
                                value = Mathf.Clamp(value, dragStartLimit, dragEndLimit - size);
                                s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double) kScrollWait);
                            }
                            return;
                        }
                        if ((state != null) && ((state.whereWeDrag == 3) || (state.whereWeDrag == 4)))
                        {
                            GUIUtility.hotControl = 0;
                        }
                    }
                    return;
            }
        }
    }

    private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
    {
        int controlID = GUIUtility.GetControlID(repeatButtonHash, focusType, position);
        EventType typeForControl = Event.current.GetTypeForControl(controlID);
        switch (typeForControl)
        {
            case EventType.MouseDown:
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                return false;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                    return position.Contains(Event.current.mousePosition);
                }
                return false;
        }
        if (typeForControl == EventType.Repaint)
        {
            style.Draw(position, content, controlID);
            if (controlID == GUIUtility.hotControl)
            {
                return position.Contains(Event.current.mousePosition);
            }
        }
        return false;
    }

    public static void MinMaxScroller(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
    {
        float num;
        Rect rect;
        Rect rect2;
        Rect rect3;
        if (horiz)
        {
            num = (size * 10f) / position.width;
        }
        else
        {
            num = (size * 10f) / position.height;
        }
        if (horiz)
        {
            rect = new Rect(position.x + leftButton.fixedWidth, position.y, (position.width - leftButton.fixedWidth) - rightButton.fixedWidth, position.height);
            rect2 = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
            rect3 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
        }
        else
        {
            rect = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, (position.height - leftButton.fixedHeight) - rightButton.fixedHeight);
            rect2 = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
            rect3 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
        }
        float num2 = Mathf.Min(visualStart, value);
        float num3 = Mathf.Max(visualEnd, value + size);
        MinMaxSlider(rect, ref value, ref size, num2, num3, num2, num3, slider, thumb, horiz);
        bool flag = false;
        if (Event.current.type == EventType.MouseUp)
        {
            flag = true;
        }
        if (ScrollerRepeatButton(id, rect2, leftButton))
        {
            value -= num * ((visualStart >= visualEnd) ? -1f : 1f);
        }
        if (ScrollerRepeatButton(id, rect3, rightButton))
        {
            value += num * ((visualStart >= visualEnd) ? -1f : 1f);
        }
        if (flag && (Event.current.type == EventType.Used))
        {
            scrollControlID = 0;
        }
        if (startLimit < endLimit)
        {
            value = Mathf.Clamp(value, startLimit, endLimit - size);
        }
        else
        {
            value = Mathf.Clamp(value, endLimit, startLimit - size);
        }
    }

    public static void MinMaxSlider(Rect position, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
    {
        DoMinMaxSlider(position, GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive), ref value, ref size, visualStart, visualEnd, startLimit, endLimit, slider, thumb, horiz);
    }

    private static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
    {
        bool flag = false;
        if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
        {
            scrollControlID = scrollerID;
            if (scrollControlID != scrollerID)
            {
                flag = true;
                nextScrollStepTime = Time.realtimeSinceStartup + (0.001f * firstScrollWait);
                return flag;
            }
            if (Time.realtimeSinceStartup >= nextScrollStepTime)
            {
                flag = true;
                nextScrollStepTime = Time.realtimeSinceStartup + (0.001f * scrollWait);
            }
        }
        return flag;
    }

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
}

