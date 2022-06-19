using System;
using UnityEngine;

namespace CinemaDirector
{
	public class ZoomableArea
	{
		[Serializable]
		internal class Styles
		{
			public GUIStyle horizontalMinMaxScrollbarThumb;

			public GUIStyle horizontalScrollbar;

			public GUIStyle horizontalScrollbarLeftButton;

			public GUIStyle horizontalScrollbarRightButton;

			public float sliderWidth;

			public float visualSliderWidth;

			public Styles()
			{
				visualSliderWidth = 15f;
				sliderWidth = 15f;
			}

			public void InitGUIStyles()
			{
				horizontalMinMaxScrollbarThumb = "horizontalMinMaxScrollbarThumb";
                horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
				horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
                horizontalScrollbar = GUI.skin.horizontalScrollbar;
			}
		}

		private int horizontalScrollbarID;

		private Rect m_DrawArea;
        private float m_HRangeMin;

		private float m_HScaleMax;

		private float m_HScaleMin;

		private float m_hScrollMax;

		private bool m_HSlider;

		private bool m_IgnoreScrollWheelUntilClicked;

		private Rect m_LastShownAreaInsideMargins;
        public float m_MarginLeft;

		private float m_MarginRight;

		private float m_MarginTop;

		private readonly bool m_MinimalGUI;

		private static Vector2 m_MouseDownPosition = new Vector2(-1000000f, -1000000f);

		private Vector2 m_Scale;

		private bool m_ScaleWithWindow;

		private Vector2 m_Translation;

		private readonly Styles styles;

		private static readonly int zoomableAreaHash = "ZoomableArea".GetHashCode();

		internal Vector2 Scale
		{
			get
			{
				return m_Scale;
			}
			set
			{
				m_Scale = value;
			}
		}

		internal Vector2 Translation
		{
			get
			{
				return m_Translation;
			}
			set
			{
				m_Translation = value;
			}
		}

        internal float Bottommargin { get; set; }

        internal virtual Bounds DrawingBounds
		{
			get
			{
				bool flag = HRangeMin > float.NegativeInfinity && HRangeMax < float.PositiveInfinity;
				return new Bounds(new Vector3((!flag) ? (HScrollMax * 0.5f) : ((HRangeMin + HRangeMax) * 0.5f), 0f, 0f), new Vector3((!flag) ? HScrollMax : (HRangeMax - HRangeMin), 2f, 1f));
			}
		}

		internal Rect DrawRect
		{
			get
			{
				return m_DrawArea;
			}
		}

        internal bool hRangeLocked { get; set; }

        internal float HRangeMax { get; set; }

        internal float HRangeMin
		{
			get
			{
				return m_HRangeMin;
			}
			set
			{
				m_HRangeMin = value;
			}
		}

		internal bool HSlider
		{
			get
			{
				return m_HSlider;
			}
			set
			{
				Rect rect = this.rect;
				m_HSlider = value;
				this.rect = rect;
			}
		}

		internal bool IgnoreScrollWheelUntilClicked
		{
			get
			{
				return m_IgnoreScrollWheelUntilClicked;
			}
			set
			{
				m_IgnoreScrollWheelUntilClicked = value;
			}
		}

		internal float Leftmargin
		{
			get
			{
				return m_MarginLeft;
			}
			set
			{
				m_MarginLeft = value;
			}
		}

		internal float Margin
		{
			set
			{
				Bottommargin = value;
				m_MarginTop = value;
				m_MarginRight = value;
				m_MarginLeft = value;
			}
		}

		internal Vector2 mousePositionInDrawing
		{
			get
			{
				return ViewToDrawingTransformPoint(Event.current.mousePosition);
			}
		}

		internal Rect rect
		{
			get
			{
				return new Rect(DrawRect.x, DrawRect.y, DrawRect.width, DrawRect.height + ((!m_HSlider) ? 0f : styles.visualSliderWidth));
			}
			set
			{
				Rect rect = new Rect(value.x, value.y, value.width, value.height - ((!m_HSlider) ? 0f : styles.visualSliderWidth));
				if (rect != m_DrawArea)
				{
					if (m_ScaleWithWindow)
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

		internal float rightmargin
		{
			get
			{
				return m_MarginRight;
			}
			set
			{
				m_MarginRight = value;
			}
		}

		public Vector2 scale
		{
			get
			{
				return m_Scale;
			}
		}

		internal bool scaleWithWindow
		{
			get
			{
				return m_ScaleWithWindow;
			}
			set
			{
				m_ScaleWithWindow = value;
			}
		}

		internal float HScrollMax
		{
			get
			{
				return m_hScrollMax;
			}
			set
			{
				m_hScrollMax = value;
			}
		}

		internal Rect shownArea
		{
			get
			{
				return new Rect(-m_Translation.x / m_Scale.x, -(m_Translation.y - DrawRect.height) / m_Scale.y, DrawRect.width / m_Scale.x, DrawRect.height / -m_Scale.y);
			}
			set
			{
				m_Scale.x = DrawRect.width / value.width;
				m_Translation.x = -value.x * m_Scale.x;
				m_Translation.y = DrawRect.height - value.y * m_Scale.y;
				EnforceScaleAndRange();
			}
		}

		internal Rect shownAreaInsideMargins
		{
			get
			{
				return shownAreaInsideMarginsInternal;
			}
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
				float num = Leftmargin / m_Scale.x;
				float num2 = rightmargin / m_Scale.x;
				float num3 = topmargin / m_Scale.y;
				float num4 = Bottommargin / m_Scale.y;
				Rect shownArea = this.shownArea;
				shownArea.x += num;
				shownArea.y -= num3;
				shownArea.width -= num + num2;
				shownArea.height += num3 + num4;
				return shownArea;
			}
			set
			{
				m_Scale.x = (DrawRect.width - Leftmargin - rightmargin) / value.width;
				m_Translation.x = -value.x * m_Scale.x + Leftmargin;
				m_Translation.y = DrawRect.height - value.y * m_Scale.y - topmargin;
			}
		}

		internal float topmargin
		{
			get
			{
				return m_MarginTop;
			}
			set
			{
				m_MarginTop = value;
			}
		}

		public ZoomableArea()
		{
			m_HRangeMin = float.NegativeInfinity;
			HRangeMax = float.PositiveInfinity;
			m_HScaleMin = 0.001f;
			m_HScaleMax = 100000f;
			m_HSlider = true;
			m_DrawArea = new Rect(0f, 0f, 100f, 100f);
			m_Scale = new Vector2(1f, -1f);
			m_Translation = new Vector2(0f, 0f);
			m_LastShownAreaInsideMargins = new Rect(0f, 0f, 100f, 100f);
			m_MinimalGUI = false;
			styles = new Styles();
		}

		public void BeginViewGUI(bool handleUserInteraction)
		{
			if (styles.horizontalScrollbar == null)
			{
				styles.InitGUIStyles();
			}
			var drawArea = m_DrawArea;
			drawArea.x = 0f;
			drawArea.y = 0f;
			GUILayout.BeginArea(DrawRect);
			if (handleUserInteraction)
			{
				int controlID = GUIUtility.GetControlID(zoomableAreaHash, 0, drawArea);
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
					case EventType.MouseMove:
						if (drawArea.Contains(Event.current.mousePosition) && GUIUtility.keyboardControl == controlID)
						{
							
						}
						break;
					case EventType.MouseDrag:
						if (GUIUtility.hotControl == controlID)
						{
							if (IsZoomEvent())
							{
								Zoom(m_MouseDownPosition, false);
								Event.current.Use();
							}
							else if (IsPanEvent())
							{
								Pan();
								Event.current.Use();
							}
						}
						break;
					case EventType.ScrollWheel:
						if (drawArea.Contains(Event.current.mousePosition) && GUIUtility.keyboardControl == controlID)
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
			{
				SliderGUI();
			}
		}

		public void EndViewGUI()
		{
			if (m_MinimalGUI && Event.current.type == EventType.Repaint)
			{
				SliderGUI();
			}
		}

		private void EnforceScaleAndRange()
		{
			float hScaleMin = m_HScaleMin;
			float num = m_HScaleMax;
			if (HRangeMax != float.PositiveInfinity && HRangeMin != float.NegativeInfinity)
			{
				num = Mathf.Min(m_HScaleMax, HRangeMax - HRangeMin);
			}
			Rect lastShownAreaInsideMargins = m_LastShownAreaInsideMargins;
			Rect shownAreaInsideMargins = this.shownAreaInsideMargins;
			if (shownAreaInsideMargins != lastShownAreaInsideMargins)
			{
				float num2 = 1E-05f;
				if (shownAreaInsideMargins.width < lastShownAreaInsideMargins.width - num2)
				{
					float num3 = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, hScaleMin);
					float num4 = Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, num3);
					float num5 = Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, num3);
					shownAreaInsideMargins = new Rect(num4, shownAreaInsideMargins.y, num5, shownAreaInsideMargins.height);
				}
				if (shownAreaInsideMargins.height < lastShownAreaInsideMargins.height - num2)
				{
					float num6 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, 1f);
					float num7 = Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, num6);
					shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, num7, shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, num6));
				}
				if (shownAreaInsideMargins.width > lastShownAreaInsideMargins.width + num2)
				{
					float num8 = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, num);
					float num9 = Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, num8);
					float num10 = Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, num8);
					shownAreaInsideMargins = new Rect(num9, shownAreaInsideMargins.y, num10, shownAreaInsideMargins.height);
				}
				if (shownAreaInsideMargins.height > lastShownAreaInsideMargins.height + num2)
				{
					float num11 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, 1f);
					float num12 = Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, num11);
					shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, num12, shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, num11));
				}
				if (shownAreaInsideMargins.xMin < HRangeMin)
				{
					shownAreaInsideMargins.x = HRangeMin;
				}
				if (shownAreaInsideMargins.xMax > HRangeMax)
				{
					shownAreaInsideMargins.x = HRangeMax - shownAreaInsideMargins.width;
				}
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
			if (!hRangeLocked)
			{
				m_Translation.x = m_Translation.x + Event.current.delta.x;
			}
			EnforceScaleAndRange();
		}

		internal void SetShownHRange(float min, float max)
		{
			m_Scale.x = DrawRect.width / (max - min);
			m_Translation.x = -min * m_Scale.x;
			EnforceScaleAndRange();
		}

		internal void SetShownHRangeInsideMargins(float min, float max)
		{
			m_Scale.x = (DrawRect.width - Leftmargin - rightmargin) / (max - min);
			m_Translation.x = -min * m_Scale.x + Leftmargin;
			EnforceScaleAndRange();
		}

		private void SliderGUI()
		{
			if (m_HSlider)
			{
				var drawingBounds = DrawingBounds;
				var num = styles.sliderWidth - styles.visualSliderWidth;
				var num2 = (!HSlider) ? 0f : num;
				if (m_HSlider)
				{
					var sliderRect = new Rect(DrawRect.x, DrawRect.yMax - num, DrawRect.width - num2, styles.sliderWidth);
					var width = shownAreaInsideMargins.width;
					var xMin = shownAreaInsideMargins.xMin;
					MinMaxSliderControl.MinMaxScroller(sliderRect, horizontalScrollbarID, ref xMin, ref width, drawingBounds.min.x, drawingBounds.max.x, float.NegativeInfinity, float.PositiveInfinity, styles.horizontalScrollbar, styles.horizontalMinMaxScrollbarThumb, styles.horizontalScrollbarLeftButton, styles.horizontalScrollbarRightButton, true);
					float num3 = xMin;
					float num4 = xMin + width;
					if (num3 > shownAreaInsideMargins.xMin)
					{
						num3 = Mathf.Min(num3, num4 - m_HScaleMin);
					}
					if (num4 < shownAreaInsideMargins.xMax)
					{
						num4 = Mathf.Max(num4, num3 + m_HScaleMin);
					}
					SetShownHRangeInsideMargins(num3, num4);
				}
			}
		}

		internal float TimeToPixel(float time, Rect rect)
		{
			return (time - shownArea.x) / shownArea.width * rect.width + rect.x;
		}

		internal Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
		{
			return new Vector2((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y);
		}

		internal Vector2 ViewToDrawingTransformVector(Vector2 lhs)
		{
			return new Vector2(lhs.x / m_Scale.x, lhs.y / m_Scale.y);
		}

		internal Vector2 DrawingToViewTransformVector(Vector2 lhs)
		{
			return new Vector2(lhs.x * m_Scale.x, lhs.y * m_Scale.y);
		}

		internal Vector2 DrawingToViewTransformPoint(Vector2 lhs)
		{
			return new Vector2(lhs.x * m_Scale.x + m_Translation.x, lhs.y * m_Scale.y + m_Translation.y);
		}

		private void Zoom(Vector2 zoomAround, bool scrollwhell)
		{
			float num = Event.current.delta.x + Event.current.delta.y;
			if (scrollwhell)
			{
				num = -num;
			}
			float num2 = Mathf.Max(0.01f, 1f + num * 0.01f);
			if (!hRangeLocked)
			{
				m_Translation.x = m_Translation.x - zoomAround.x * (num2 - 1f) * m_Scale.x;
				m_Scale.x = m_Scale.x * num2;
			}
			EnforceScaleAndRange();
		}
	}
}