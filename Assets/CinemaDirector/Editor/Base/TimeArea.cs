using UnityEditor;
using UnityEngine;

namespace CinemaDirector
{
	public class TimeArea : ZoomableArea
	{
		private class TimeAreaStyle
		{
			public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
			public GUIStyle TimelineTick = "AnimationTimelineTick";
		}

		private static TimeAreaStyle styles;
        internal TickHandler HTicks { get; set; }

        internal DirectorControlSettings Settings => DirectorControlSettings.Instance;

		public TimeArea()
		{
			
            float[] tickModulos = new float[]
			{
				0.0005f,
				0.001f,
				0.005f,
				0.01f,
				0.05f,
				0.1f,
				0.5f,
				1f,
				5f,
				10f,
				50f,
				100f,
				500f,
				1000f,
				5000f,
				10000f
			};
			HTicks = new TickHandler();
			HTicks.SetTickModulos(tickModulos);
		}

		public void ApplySettings()
		{
            hRangeLocked = Settings.HRangeLocked;
            HRangeMin = Settings.HorizontalRangeMin;
            HRangeMax = Settings.HRangeMax;
			scaleWithWindow = Settings.ScaleWithWindow;
            HSlider = Settings.HSlider;
		}

		public float GetMajorTickDistance()
		{
			float result = 0f;
			for (int i = 0; i < HTicks.tickLevels; i++)
			{
				if (HTicks.GetStrengthOfLevel(i) > 0.5f)
				{
					return HTicks.GetPeriodOfLevel(i);
				}
			}
			return result;
		}

		public void DrawMajorTicks(Rect position, float frameRate)
		{
			Color color = Handles.color;
			GUI.BeginGroup(position);
			if (Event.current.type != EventType.Repaint)
			{
				GUI.EndGroup();
				return;
			}
            InitStyles();
			SetTickMarkerRanges();
			HTicks.SetTickStrengths(3f, 80f, true);
			Color textColor = styles.TimelineTick.normal.textColor;
			textColor.a = 0.3f;
			Handles.color = textColor;
			for (int i = 0; i < HTicks.tickLevels; i++)
			{
				float strengthOfLevel = HTicks.GetStrengthOfLevel(i);
				if (strengthOfLevel > 0.5f)
				{
					float[] ticksAtLevel = HTicks.GetTicksAtLevel(i, true);
					for (int j = 0; j < ticksAtLevel.Length; j++)
					{
						if (ticksAtLevel[j] >= 0f)
						{
							int num = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
							float num2 = FrameToPixel(num, frameRate, position);
							Handles.DrawLine(new Vector3(num2, 0f, 0f), new Vector3(num2, position.height, 0f));
							if (strengthOfLevel > 0.8f)
							{
								Handles.DrawLine(new Vector3(num2 + 1f, 0f, 0f), new Vector3(num2 + 1f, position.height, 0f));
							}
						}
					}
				}
			}
			GUI.EndGroup();
			Handles.color = color;
		}

		public string FormatFrame(int frame, float frameRate)
		{
			return string.Format("{0:N2}", frame / frameRate);
		}

		public float FrameToPixel(float i, float frameRate, Rect rect)
		{
			Rect shownArea = base.shownArea;
			return (i - shownArea.xMin * frameRate) * rect.width / (shownArea.width * frameRate);
		}

		private static void InitStyles()
		{
			if (styles == null)
			{
				styles = new TimeAreaStyle();
			}
		}

		public void SetTickMarkerRanges()
		{
			Rect shownArea = base.shownArea;
			HTicks.SetRanges(shownArea.xMin, shownArea.xMax, DrawRect.xMin, DrawRect.xMax);
		}

		public void TimeRuler(Rect position, float frameRate)
		{
			Color color = Handles.color;
			GUI.BeginGroup(position);
			if (Event.current.type != EventType.Repaint)
			{
				GUI.EndGroup();
				return;
			}
			InitStyles();
			SetTickMarkerRanges();
			HTicks.SetTickStrengths(3f, 80f, true);
			Color textColor = styles.TimelineTick.normal.textColor;
			textColor.a = 0.3f;
			Handles.color = textColor;
			for (int i = 0; i < HTicks.tickLevels; i++)
			{
				float strengthOfLevel = HTicks.GetStrengthOfLevel(i);
				if (strengthOfLevel > 0.2f)
				{
					float[] ticksAtLevel = HTicks.GetTicksAtLevel(i, true);
					for (int j = 0; j < ticksAtLevel.Length; j++)
					{
						if (ticksAtLevel[j] >= HRangeMin && ticksAtLevel[j] <= HRangeMax)
						{
							int num = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
							float num2 = position.height * Mathf.Min(1f, strengthOfLevel) * 0.7f;
							float num3 = FrameToPixel(num, frameRate, position);
							Handles.DrawLine(new Vector3(num3, position.height - num2 + 0.5f, 0f), new Vector3(num3, position.height - 0.5f, 0f));
							if (strengthOfLevel > 0.5f)
							{
								Handles.DrawLine(new Vector3(num3 + 1f, position.height - num2 + 0.5f, 0f), new Vector3(num3 + 1f, position.height - 0.5f, 0f));
							}
						}
					}
				}
			}
			GL.End();
			int levelWithMinSeparation = HTicks.GetLevelWithMinSeparation(40f);
			float[] ticksAtLevel2 = HTicks.GetTicksAtLevel(levelWithMinSeparation, false);
			for (int k = 0; k < ticksAtLevel2.Length; k++)
			{
				if (ticksAtLevel2[k] >= HRangeMin && ticksAtLevel2[k] <= HRangeMax)
				{
					int num4 = Mathf.RoundToInt(ticksAtLevel2[k] * frameRate);
					float arg_21E_0 = Mathf.Floor(FrameToPixel(num4, frameRate, rect));
					string text = FormatFrame(num4, frameRate);
					GUI.Label(new Rect(arg_21E_0 + 3f, -3f, 40f, 20f), text, styles.TimelineTick);
				}
			}
			GUI.EndGroup();
			Handles.color = (color);
		}
	}
}