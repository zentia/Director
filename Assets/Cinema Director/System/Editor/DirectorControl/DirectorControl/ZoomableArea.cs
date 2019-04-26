using System;
using UnityEngine;

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
			this.sliderWidth = 15f;
		}

		public void InitGUIStyles()
		{
			horizontalMinMaxScrollbarThumb = "horizontalMinMaxScrollbarThumb";
			this.horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
			this.horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
			this.horizontalScrollbar = GUI.skin.horizontalScrollbar;
		}
	}

	private int horizontalScrollbarID;

	private Rect m_DrawArea;

	private bool m_HRangeLocked;

	private float m_HRangeMax;

	private float m_HRangeMin;

	private float m_HScaleMax;

	private float m_HScaleMin;

	private float m_hScrollMax;

	private bool m_HSlider;

	private bool m_IgnoreScrollWheelUntilClicked;

	private Rect m_LastShownAreaInsideMargins;

	private float m_MarginBottom;

	public float m_MarginLeft;

	private float m_MarginRight;

	private float m_MarginTop;

	private bool m_MinimalGUI;

	private static Vector2 m_MouseDownPosition = new Vector2(-1000000f, -1000000f);

	private Vector2 m_Scale;

	private bool m_ScaleWithWindow;

	private Vector2 m_Translation;

	private ZoomableArea.Styles styles;

	private static int zoomableAreaHash = "ZoomableArea".GetHashCode();

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

	internal float bottommargin
	{
		get
		{
			return m_MarginBottom;
		}
		set
		{
			this.m_MarginBottom = value;
		}
	}

	internal virtual Bounds drawingBounds
	{
		get
		{
			bool flag = hRangeMin > float.NegativeInfinity && this.hRangeMax < float.PositiveInfinity;
			return new Bounds(new Vector3((!flag) ? (this.HScrollMax * 0.5f) : ((this.hRangeMin + this.hRangeMax) * 0.5f), 0f, 0f), new Vector3((!flag) ? this.HScrollMax : (this.hRangeMax - this.hRangeMin), 2f, 1f));
		}
	}

	internal Rect drawRect
	{
		get
		{
			return this.m_DrawArea;
		}
	}

	internal bool hRangeLocked
	{
		get
		{
			return this.m_HRangeLocked;
		}
		set
		{
			this.m_HRangeLocked = value;
		}
	}

	internal float hRangeMax
	{
		get
		{
			return this.m_HRangeMax;
		}
		set
		{
			this.m_HRangeMax = value;
		}
	}

	internal float hRangeMin
	{
		get
		{
			return this.m_HRangeMin;
		}
		set
		{
			this.m_HRangeMin = value;
		}
	}

	internal bool hSlider
	{
		get
		{
			return this.m_HSlider;
		}
		set
		{
			Rect rect = this.rect;
			this.m_HSlider = value;
			this.rect = rect;
		}
	}

	internal bool ignoreScrollWheelUntilClicked
	{
		get
		{
			return this.m_IgnoreScrollWheelUntilClicked;
		}
		set
		{
			this.m_IgnoreScrollWheelUntilClicked = value;
		}
	}

	internal float leftmargin
	{
		get
		{
			return this.m_MarginLeft;
		}
		set
		{
			this.m_MarginLeft = value;
		}
	}

	internal float margin
	{
		set
		{
			this.m_MarginBottom = value;
			this.m_MarginTop = value;
			this.m_MarginRight = value;
			this.m_MarginLeft = value;
		}
	}

	internal Vector2 mousePositionInDrawing
	{
		get
		{
			return this.ViewToDrawingTransformPoint(Event.current.mousePosition);
		}
	}

	internal Rect rect
	{
		get
		{
			return new Rect(this.drawRect.x, this.drawRect.y, this.drawRect.width, this.drawRect.height + ((!this.m_HSlider) ? 0f : this.styles.visualSliderWidth));
		}
		set
		{
			Rect rect = new Rect(value.x, value.y, value.width, value.height - ((!this.m_HSlider) ? 0f : this.styles.visualSliderWidth));
			if (rect != this.m_DrawArea)
			{
				if (m_ScaleWithWindow)
				{
					this.m_DrawArea = rect;
					this.shownAreaInsideMargins = this.m_LastShownAreaInsideMargins;
				}
				else
				{
					m_Translation += new Vector2((rect.width - m_DrawArea.width) / 2f, 0f);
					this.m_DrawArea = rect;
				}
			}
			this.EnforceScaleAndRange();
		}
	}

	internal float rightmargin
	{
		get
		{
			return this.m_MarginRight;
		}
		set
		{
			this.m_MarginRight = value;
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
			return this.m_ScaleWithWindow;
		}
		set
		{
			this.m_ScaleWithWindow = value;
		}
	}

	internal float HScrollMax
	{
		get
		{
			return this.m_hScrollMax;
		}
		set
		{
			this.m_hScrollMax = value;
		}
	}

	internal Rect shownArea
	{
		get
		{
			return new Rect(-this.m_Translation.x / this.m_Scale.x, -(this.m_Translation.y - this.drawRect.height) / this.m_Scale.y, this.drawRect.width / this.m_Scale.x, this.drawRect.height / -this.m_Scale.y);
		}
		set
		{
			this.m_Scale.x = this.drawRect.width / value.width;
			this.m_Translation.x = -value.x * this.m_Scale.x;
			this.m_Translation.y = this.drawRect.height - value.y * this.m_Scale.y;
			this.EnforceScaleAndRange();
		}
	}

	internal Rect shownAreaInsideMargins
	{
		get
		{
			return this.shownAreaInsideMarginsInternal;
		}
		set
		{
			this.shownAreaInsideMarginsInternal = value;
			this.EnforceScaleAndRange();
		}
	}

	internal Rect shownAreaInsideMarginsInternal
	{
		get
		{
			float num = this.leftmargin / this.m_Scale.x;
			float num2 = this.rightmargin / this.m_Scale.x;
			float num3 = this.topmargin / this.m_Scale.y;
			float num4 = this.bottommargin / this.m_Scale.y;
			Rect shownArea = this.shownArea;
			shownArea.x=(shownArea.x + num);
			shownArea.y=(shownArea.y - num3);
			shownArea.width=(shownArea.width - (num + num2));
			shownArea.height=(shownArea.height + (num3 + num4));
			return shownArea;
		}
		set
		{
			this.m_Scale.x = (this.drawRect.width - this.leftmargin - this.rightmargin) / value.width;
			this.m_Translation.x = -value.x * this.m_Scale.x + this.leftmargin;
			this.m_Translation.y = this.drawRect.height - value.y * this.m_Scale.y - this.topmargin;
		}
	}

	internal float topmargin
	{
		get
		{
			return this.m_MarginTop;
		}
		set
		{
			this.m_MarginTop = value;
		}
	}

	public ZoomableArea()
	{
		this.m_HRangeMin = float.NegativeInfinity;
		this.m_HRangeMax = float.PositiveInfinity;
		this.m_HScaleMin = 0.001f;
		this.m_HScaleMax = 100000f;
		this.m_HSlider = true;
		this.m_DrawArea = new Rect(0f, 0f, 100f, 100f);
		this.m_Scale = new Vector2(1f, -1f);
		this.m_Translation = new Vector2(0f, 0f);
		this.m_LastShownAreaInsideMargins = new Rect(0f, 0f, 100f, 100f);
		this.m_MinimalGUI = false;
		this.styles = new ZoomableArea.Styles();
	}

	public void BeginViewGUI(bool handleUserInteraction)
	{
		if (this.styles.horizontalScrollbar == null)
		{
			this.styles.InitGUIStyles();
		}
		Rect drawArea = this.m_DrawArea;
		drawArea.x=(0f);
		drawArea.y=(0f);
		GUILayout.BeginArea(this.drawRect);
		if (handleUserInteraction)
		{
			int controlID = GUIUtility.GetControlID(ZoomableArea.zoomableAreaHash, 0, drawArea);
			switch ((int)Event.current.GetTypeForControl(controlID))
			{
			case 0:
				if (drawArea.Contains(Event.current.mousePosition))
				{
					GUIUtility.keyboardControl=(controlID);
					if (this.IsZoomEvent() || this.IsPanEvent())
					{
						GUIUtility.hotControl=(controlID);
						ZoomableArea.m_MouseDownPosition = this.mousePositionInDrawing;
						Event.current.Use();
					}
				}
				break;
			case 1:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl=(0);
					ZoomableArea.m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
				}
				break;
			case 3:
				if (GUIUtility.hotControl == controlID)
				{
					if (this.IsZoomEvent())
					{
						this.Zoom(ZoomableArea.m_MouseDownPosition, false);
						Event.current.Use();
					}
					else if (this.IsPanEvent())
					{
						this.Pan();
						Event.current.Use();
					}
				}
				break;
			case 6:
				if (drawArea.Contains(Event.current.mousePosition) && GUIUtility.keyboardControl == controlID)
				{
					this.Zoom(this.mousePositionInDrawing, true);
					Event.current.Use();
				}
				break;
			}
		}
		GUILayout.EndArea();
		this.horizontalScrollbarID = GUIUtility.GetControlID(MinMaxSliderControl.s_MinMaxSliderHash, (FocusType)2);
		if (!this.m_MinimalGUI || Event.current.type != (EventType)7)
		{
			this.SliderGUI();
		}
	}

	public void EndViewGUI()
	{
		if (this.m_MinimalGUI && Event.current.type == (EventType)7)
		{
			this.SliderGUI();
		}
	}

	private void EnforceScaleAndRange()
	{
		float hScaleMin = this.m_HScaleMin;
		float num = this.m_HScaleMax;
		if (this.hRangeMax != float.PositiveInfinity && this.hRangeMin != float.NegativeInfinity)
		{
			num = Mathf.Min(this.m_HScaleMax, this.hRangeMax - this.hRangeMin);
		}
		Rect lastShownAreaInsideMargins = this.m_LastShownAreaInsideMargins;
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
			if (shownAreaInsideMargins.xMin < this.hRangeMin)
			{
				shownAreaInsideMargins.x=(this.hRangeMin);
			}
			if (shownAreaInsideMargins.xMax > this.hRangeMax)
			{
				shownAreaInsideMargins.x=(this.hRangeMax - shownAreaInsideMargins.width);
			}
			this.shownAreaInsideMarginsInternal = shownAreaInsideMargins;
			this.m_LastShownAreaInsideMargins = shownAreaInsideMargins;
		}
	}

	private bool IsPanEvent()
	{
		return ((int)Event.current.button == 0 && Event.current.alt) || ((int)Event.current.button == 2 && !Event.current.command);
	}

	private bool IsZoomEvent()
	{
		return Event.current.button == 1 && Event.current.alt;
	}

	private void Pan()
	{
		if (!this.m_HRangeLocked)
		{
			this.m_Translation.x = this.m_Translation.x + Event.current.delta.x;
		}
		this.EnforceScaleAndRange();
	}

	internal void SetShownHRange(float min, float max)
	{
		this.m_Scale.x = this.drawRect.width / (max - min);
		this.m_Translation.x = -min * this.m_Scale.x;
		this.EnforceScaleAndRange();
	}

	internal void SetShownHRangeInsideMargins(float min, float max)
	{
		this.m_Scale.x = (this.drawRect.width - this.leftmargin - this.rightmargin) / (max - min);
		this.m_Translation.x = -min * this.m_Scale.x + this.leftmargin;
		this.EnforceScaleAndRange();
	}

	private void SliderGUI()
	{
		if (this.m_HSlider)
		{
			Bounds drawingBounds = this.drawingBounds;
			Rect shownAreaInsideMargins = this.shownAreaInsideMargins;
			float num = this.styles.sliderWidth - this.styles.visualSliderWidth;
			float num2 = (!this.hSlider) ? 0f : num;
			if (this.m_HSlider)
			{
				Rect arg_FE_0 = new Rect(this.drawRect.x, this.drawRect.yMax - num, this.drawRect.width - num2, this.styles.sliderWidth);
				float width = shownAreaInsideMargins.width;
				float xMin = shownAreaInsideMargins.xMin;
				MinMaxSliderControl.MinMaxScroller(arg_FE_0, this.horizontalScrollbarID, ref xMin, ref width, drawingBounds.min.x, drawingBounds.max.x, float.NegativeInfinity, float.PositiveInfinity, this.styles.horizontalScrollbar, this.styles.horizontalMinMaxScrollbarThumb, this.styles.horizontalScrollbarLeftButton, this.styles.horizontalScrollbarRightButton, true);
				float num3 = xMin;
				float num4 = xMin + width;
				if (num3 > shownAreaInsideMargins.xMin)
				{
					num3 = Mathf.Min(num3, num4 - this.m_HScaleMin);
				}
				if (num4 < shownAreaInsideMargins.xMax)
				{
					num4 = Mathf.Max(num4, num3 + this.m_HScaleMin);
				}
				this.SetShownHRangeInsideMargins(num3, num4);
			}
		}
	}

	internal float TimeToPixel(float time, Rect rect)
	{
		Rect shownArea = this.shownArea;
		return (time - shownArea.x) / shownArea.width * rect.width + rect.x;
	}

	internal float TimeToPixel(float time)
	{
		Rect shownArea = this.shownArea;
		return (time - shownArea.x) / shownArea.width * this.m_DrawArea.width + this.m_DrawArea.x;
	}

	internal Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
	{
		return new Vector2((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y);
	}

	internal Vector2 ViewToDrawingTransformVector(Vector2 lhs)
	{
		return new Vector2(lhs.x / this.m_Scale.x, lhs.y / this.m_Scale.y);
	}

	internal Vector2 DrawingToViewTransformVector(Vector2 lhs)
	{
		return new Vector2(lhs.x * this.m_Scale.x, lhs.y * this.m_Scale.y);
	}

	internal Vector2 DrawingToViewTransformPoint(Vector2 lhs)
	{
		return new Vector2(lhs.x * this.m_Scale.x + this.m_Translation.x, lhs.y * this.m_Scale.y + this.m_Translation.y);
	}

	private void Zoom(Vector2 zoomAround, bool scrollwhell)
	{
		float num = Event.current.delta.x + Event.current.delta.y;
		if (scrollwhell)
		{
			num = -num;
		}
		float num2 = Mathf.Max(0.01f, 1f + num * 0.01f);
		if (!m_HRangeLocked)
		{
			this.m_Translation.x = this.m_Translation.x - zoomAround.x * (num2 - 1f) * this.m_Scale.x;
			this.m_Scale.x = this.m_Scale.x * num2;
		}
		this.EnforceScaleAndRange();
	}
}
