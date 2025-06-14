using System;
using UnityEngine;

namespace TimelineEditor
{
    public class ZoomableArea
    {
        private static Vector2 m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
        private static readonly int zoomableAreaHash = "ZoomableArea".GetHashCode();
        private readonly float m_HScaleMax = 100000f;
        private readonly float m_HScaleMin = 0.001f;
        private readonly bool m_MinimalGUI = false;
        private readonly Styles styles = new Styles();
        private int horizontalScrollbarID;
        private Rect m_DrawArea = new Rect(0f, 0f, 100f, 100f);
        private bool m_HSlider = true;
        private Rect m_LastShownAreaInsideMargins = new Rect(0f, 0f, 100f, 100f);
        public float m_MarginLeft;
        private Vector2 m_Scale = new Vector2(1f, -1f);
        private Vector2 m_Translation = new Vector2(0f, 0f);

        public Vector2 Scale
        {
            get { return m_Scale; }

            set { m_Scale = value; }
        }

        public Vector2 Translation
        {
            get { return m_Translation; }

            set { m_Translation = value; }
        }

        internal float bottommargin { get; set; }

        internal virtual Bounds drawingBounds
        {
            get
            {
                var flag = hRangeMin > float.NegativeInfinity && hRangeMax < float.PositiveInfinity;
                return new Bounds(new Vector3(!flag ? HScrollMax * 0.5f : (hRangeMin + hRangeMax) * 0.5f, 0f, 0f), new Vector3(!flag ? HScrollMax : hRangeMax - hRangeMin, 2f, 1f));
            }
        }

        internal Rect drawRect =>
            m_DrawArea;

        internal bool hRangeLocked { get; set; }

        internal float hRangeMax { get; set; } = float.PositiveInfinity;

        internal float hRangeMin { get; set; } = float.NegativeInfinity;

        internal bool hSlider
        {
            get { return m_HSlider; }

            set
            {
                var rect = this.rect;
                m_HSlider = value;
                this.rect = rect;
            }
        }

        internal bool ignoreScrollWheelUntilClicked { get; set; }

        internal float leftmargin
        {
            get { return m_MarginLeft; }

            set { m_MarginLeft = value; }
        }

        public float margin
        {
            set { m_MarginLeft = rightmargin = topmargin = bottommargin = value; }
        }

        internal Vector2 mousePositionInDrawing =>
            ViewToDrawingTransformPoint(Event.current.mousePosition);

        public Rect rect
        {
            get { return new Rect(drawRect.x, drawRect.y, drawRect.width, drawRect.height + (!m_HSlider ? 0f : styles.visualSliderWidth)); }

            set
            {
                var rect = new Rect(value.x, value.y, value.width, value.height - (!m_HSlider ? 0f : styles.visualSliderWidth));
                if (rect != m_DrawArea)
                {
                    if (scaleWithWindow)
                    {
                        m_DrawArea = rect;
                        shownAreaInsideMargins = m_LastShownAreaInsideMargins;
                    }
                    else
                    {
                        m_Translation += new Vector2((rect.width - m_DrawArea.width) / 2f, 0f);
                        m_DrawArea = rect;
                    }
                }

                EnforceScaleAndRange();
            }
        }

        internal float rightmargin { get; set; }

        public Vector2 scale =>
            m_Scale;

        internal bool scaleWithWindow { get; set; }

        public float HScrollMax { get; set; }

        internal Rect shownArea
        {
            get { return new Rect(-m_Translation.x / m_Scale.x, -(m_Translation.y - drawRect.height) / m_Scale.y, drawRect.width / m_Scale.x, drawRect.height / -m_Scale.y); }

            set
            {
                m_Scale.x = drawRect.width / value.width;
                m_Translation.x = -value.x * m_Scale.x;
                m_Translation.y = drawRect.height - value.y * m_Scale.y;
                EnforceScaleAndRange();
            }
        }

        public Rect shownAreaInsideMargins
        {
            get { return shownAreaInsideMarginsInternal; }

            set
            {
                shownAreaInsideMarginsInternal = value;
                EnforceScaleAndRange();
            }
        }

        internal Rect shownAreaInsideMarginsInternal
        {
            get
            {
                var num = leftmargin / m_Scale.x;
                var num2 = rightmargin / m_Scale.x;
                var num3 = topmargin / m_Scale.y;
                var num4 = bottommargin / m_Scale.y;
                var shownArea = this.shownArea;
                shownArea.x += num;
                shownArea.y -= num3;
                shownArea.width -= num + num2;
                shownArea.height += num3 + num4;
                return shownArea;
            }
            set
            {
                m_Scale.x = (drawRect.width - leftmargin - rightmargin) / value.width;
                m_Translation.x = -value.x * m_Scale.x + leftmargin;
                m_Translation.y = drawRect.height - value.y * m_Scale.y - topmargin;
            }
        }

        internal float topmargin { get; set; }

        public void BeginViewGUI(bool handleUserInteraction)
        {
            if (styles.horizontalScrollbar == null) styles.InitGUIStyles();
            var drawArea = m_DrawArea;
            drawArea.x = 0f;
            drawArea.y = 0f;
            GUILayout.BeginArea(drawRect);
            if (handleUserInteraction)
            {
                var controlID = GUIUtility.GetControlID(zoomableAreaHash, FocusType.Native, drawArea);
                switch (Event.current.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        if (drawArea.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.keyboardControl = controlID;
                            if (IsZoomEvent() || IsPanEvent())
                            {
                                GUIUtility.hotControl = controlID;
                                m_MouseDownPosition = mousePositionInDrawing;
                                Event.current.Use();
                            }
                        }

                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            GUIUtility.hotControl = 0;
                            m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
                        }

                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                        {
                            if (!IsZoomEvent())
                            {
                                if (IsPanEvent())
                                {
                                    Pan();
                                    Event.current.Use();
                                }

                                break;
                            }

                            Zoom(m_MouseDownPosition, false);
                            Event.current.Use();
                        }

                        break;

                    case EventType.ScrollWheel:
                        if (drawArea.Contains(Event.current.mousePosition) && GUIUtility.keyboardControl == controlID && Event.current.control)
                        {
                            Zoom(mousePositionInDrawing, true);
                            Event.current.Use();
                        }

                        break;
                }
            }

            GUILayout.EndArea();
            horizontalScrollbarID = GUIUtility.GetControlID(MinMaxSliderControl.s_MinMaxSliderHash, FocusType.Passive);
            if (!m_MinimalGUI || Event.current.type != EventType.Repaint)
                SliderGUI();
        }

        internal Vector2 DrawingToViewTransformPoint(Vector2 lhs)
        {
            return new Vector2(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y);
        }

        internal Vector2 DrawingToViewTransformVector(Vector2 lhs)
        {
            return new Vector2(lhs.x * m_Scale.x, lhs.y * m_Scale.y);
        }

        public void EndViewGUI()
        {
            if (m_MinimalGUI && Event.current.type == EventType.Repaint) SliderGUI();
        }

        private void EnforceScaleAndRange()
        {
            var hScaleMin = m_HScaleMin;
            var hScaleMax = m_HScaleMax;
            if (hRangeMax != float.PositiveInfinity && hRangeMin != float.NegativeInfinity) hScaleMax = Mathf.Min(m_HScaleMax, hRangeMax - hRangeMin);
            var lastShownAreaInsideMargins = m_LastShownAreaInsideMargins;
            var shownAreaInsideMargins = this.shownAreaInsideMargins;
            if (shownAreaInsideMargins != lastShownAreaInsideMargins)
            {
                var num3 = 1E-05f;
                if (shownAreaInsideMargins.width < lastShownAreaInsideMargins.width - num3)
                {
                    var t = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, hScaleMin);
                    var x = Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, t);
                    var width = Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, t);
                    shownAreaInsideMargins = new Rect(x, shownAreaInsideMargins.y, width, shownAreaInsideMargins.height);
                }

                if (shownAreaInsideMargins.height < lastShownAreaInsideMargins.height - num3)
                {
                    var t = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, 1f);
                    var y = Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, t);
                    shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, y, shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, t));
                }

                if (shownAreaInsideMargins.width > lastShownAreaInsideMargins.width + num3)
                {
                    var t = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, hScaleMax);
                    var x = Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, t);
                    var width = Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, t);
                    shownAreaInsideMargins = new Rect(x, shownAreaInsideMargins.y, width, shownAreaInsideMargins.height);
                }

                if (shownAreaInsideMargins.height > lastShownAreaInsideMargins.height + num3)
                {
                    var t = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, 1f);
                    var y = Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, t);
                    shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, y, shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, t));
                }

                if (shownAreaInsideMargins.xMin < hRangeMin) shownAreaInsideMargins.x = hRangeMin;
                if (shownAreaInsideMargins.xMax > hRangeMax) shownAreaInsideMargins.x = hRangeMax - shownAreaInsideMargins.width;
                shownAreaInsideMarginsInternal = shownAreaInsideMargins;
                m_LastShownAreaInsideMargins = shownAreaInsideMargins;
            }
        }

        private bool IsPanEvent()
        {
            return (Event.current.button == 0 && Event.current.alt) || (Event.current.button == 2 && !Event.current.command);
        }

        private bool IsZoomEvent()
        {
            return Event.current.button == 1 && Event.current.alt;
        }

        private void Pan()
        {
            if (!hRangeLocked) m_Translation.x += Event.current.delta.x;
            EnforceScaleAndRange();
        }

        internal void SetShownHRange(float min, float max)
        {
            m_Scale.x = drawRect.width / (max - min);
            m_Translation.x = -min * m_Scale.x;
            EnforceScaleAndRange();
        }

        public void SetShownHRangeInsideMargins(float min, float max)
        {
            m_Scale.x = (drawRect.width - leftmargin - rightmargin) / (max - min);
            m_Translation.x = -min * m_Scale.x + leftmargin;
            EnforceScaleAndRange();
        }

        private void SliderGUI()
        {
            if (m_HSlider)
            {
                var drawingBounds = this.drawingBounds;
                var shownAreaInsideMargins = this.shownAreaInsideMargins;
                var num3 = styles.sliderWidth - styles.visualSliderWidth;
                var num4 = !hSlider ? 0f : num3;
                if (m_HSlider)
                {
                    var width = shownAreaInsideMargins.width;
                    var xMin = shownAreaInsideMargins.xMin;
                    MinMaxSliderControl.MinMaxScroller(new Rect(drawRect.x, drawRect.yMax - num3, drawRect.width - num4, styles.sliderWidth), horizontalScrollbarID, ref xMin, ref width, drawingBounds.min.x, drawingBounds.max.x,
                        float.NegativeInfinity, float.PositiveInfinity,
                        styles.horizontalScrollbar, styles.horizontalMinMaxScrollbarThumb, styles.horizontalScrollbarLeftButton, styles.horizontalScrollbarRightButton, true);
                    var a = xMin;
                    var num2 = xMin + width;
                    if (a > shownAreaInsideMargins.xMin) a = Mathf.Min(a, num2 - m_HScaleMin);
                    if (num2 < shownAreaInsideMargins.xMax) num2 = Mathf.Max(num2, a + m_HScaleMin);
                    SetShownHRangeInsideMargins(a, num2);
                }
            }
        }

        internal float TimeToPixel(float time)
        {
            var shownArea = this.shownArea;
            return (time - shownArea.x) / shownArea.width * m_DrawArea.width + m_DrawArea.x;
        }

        internal float TimeToPixel(float time, Rect rect)
        {
            var shownArea = this.shownArea;
            return (time - shownArea.x) / shownArea.width * rect.width + rect.x;
        }

        public Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
        {
            return new Vector2((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y);
        }

        internal Vector2 ViewToDrawingTransformVector(Vector2 lhs)
        {
            return new Vector2(lhs.x / m_Scale.x, lhs.y / m_Scale.y);
        }

        private void Zoom(Vector2 zoomAround, bool scrollwhell)
        {
            var num = Event.current.delta.x + Event.current.delta.y;
            if (scrollwhell) num = -num;
            var num2 = Mathf.Max(0.01f, 1f + num * 0.01f);
            if (!hRangeLocked)
            {
                m_Translation.x -= zoomAround.x * (num2 - 1f) * m_Scale.x;
                m_Scale.x *= num2;
            }

            EnforceScaleAndRange();
        }

        [Serializable]
        internal class Styles
        {
            public GUIStyle horizontalMinMaxScrollbarThumb;
            public GUIStyle horizontalScrollbar;
            public GUIStyle horizontalScrollbarLeftButton;
            public GUIStyle horizontalScrollbarRightButton;
            public float sliderWidth = 15f;
            public float visualSliderWidth = 15f;

            public void InitGUIStyles()
            {
                horizontalMinMaxScrollbarThumb = "horizontalMinMaxScrollbarThumb";
                horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
                horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
                horizontalScrollbar = GUI.skin.horizontalScrollbar;
            }
        }
    }
}
