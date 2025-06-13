using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimeArea : ZoomableArea
    {
        private TickHandler horizontalTicks;
        private TimelineControlSettings m_Settings = new ();
        private static TimeAreaStyle styles;
        public bool ShowCurves;

        protected TimeArea()
        {
            hTicks = new TickHandler();
        }

        private void ApplySettings()
        {
            hRangeLocked = settings.hRangeLocked;
            hRangeMin = settings.HorizontalRangeMin;
            hRangeMax = settings.hRangeMax;
            scaleWithWindow = settings.scaleWithWindow;
            hSlider = settings.hSlider;
        }

        public void DrawMajorTicks(Rect position, float frameRate)
        {
            Color color = Handles.color;
            GUI.BeginGroup(position);
            if (Event.current.type != EventType.Repaint)
            {
                GUI.EndGroup();
            }
            else
            {
                InitStyles();
                SetTickMarkerRanges();
                hTicks.SetTickStrengths(3f, 80f, true);
                Color textColor = styles.TimelineTick.normal.textColor;
                textColor.a = 0.3f;
                Handles.color = textColor;
                for (int i = 0; i < hTicks.TickLevels; i++)
                {
                    float strengthOfLevel = hTicks.GetStrengthOfLevel(i);
                    if (strengthOfLevel > 0.5f)
                    {
                        float[] ticksAtLevel = hTicks.GetTicksAtLevel(i, true);
                        for (int j = 0; j < ticksAtLevel.Length; j++)
                        {
                            if (ticksAtLevel[j] >= 0f)
                            {
                                int num4 = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
                                float x = FrameToPixel(num4, frameRate, position);
                                Handles.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, position.height, 0f));
                                if (strengthOfLevel > 0.8f)
                                {
                                    Handles.DrawLine(new Vector3(x + 1f, 0f, 0f), new Vector3(x + 1f, position.height, 0f));
                                }
                            }
                        }
                    }
                }
                GUI.EndGroup();
                Handles.color = color;
            }
        }

        public float FrameToPixel(float i, float frameRate, Rect rect)
        {
            Rect shownArea = base.shownArea;
            return (i - (shownArea.xMin * frameRate)) * rect.width / (shownArea.width * frameRate);
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
            hTicks.SetRanges(shownArea.xMin, shownArea.xMax, drawRect.xMin, drawRect.xMax);
        }

        public void TimeRuler(Rect position, float frameRate)
        {
            Color color = Handles.color;
            GUI.BeginGroup(position);
            if (Event.current.type != EventType.Repaint)
            {
                GUI.EndGroup();
            }
            else
            {
                InitStyles();
                SetTickMarkerRanges();
                hTicks.SetTickStrengths(3f, 80f, true);
                Color textColor = styles.TimelineTick.normal.textColor;
                textColor.a = 0.3f;
                Handles.color = textColor;
                for (int i = 0; i < hTicks.TickLevels; i++)
                {
                    float strengthOfLevel = hTicks.GetStrengthOfLevel(i);
                    if (strengthOfLevel > 0.2f)
                    {
                        float[] numArray2 = hTicks.GetTicksAtLevel(i, true);
                        for (int k = 0; k < numArray2.Length; k++)
                        {
                            if ((numArray2[k] >= hRangeMin) && (numArray2[k] <= hRangeMax))
                            {
                                int num5 = Mathf.RoundToInt(numArray2[k] * frameRate);
                                float num6 = position.height * Mathf.Min(1f, strengthOfLevel) * 0.7f;
                                float x = FrameToPixel(num5, frameRate, position);
                                Handles.DrawLine(new Vector3(x, position.height - num6 + 0.5f, 0f), new Vector3(x, position.height - 0.5f, 0f));
                                if (strengthOfLevel > 0.5f)
                                {
                                    Handles.DrawLine(new Vector3(x + 1f, position.height - num6 + 0.5f, 0f), new Vector3(x + 1f, position.height - 0.5f, 0f));
                                }
                            }
                        }
                    }
                }
                GL.End();
                int levelWithMinSeparation = hTicks.GetLevelWithMinSeparation(20f);
                float[] ticksAtLevel = hTicks.GetTicksAtLevel(levelWithMinSeparation, false);
                for (int j = 0; j < ticksAtLevel.Length; j++)
                {
                    if ((ticksAtLevel[j] >= hRangeMin) && (ticksAtLevel[j] <= hRangeMax))
                    {
                        int frame = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
                        GUI.Label(new Rect(Mathf.Floor(FrameToPixel(frame, frameRate, rect)) + 3f, -3f, 40f, 20f), frame.ToString(), styles.TimelineTick);
                    }
                }
                GUI.EndGroup();
                Handles.color = color;
            }
        }

        internal TickHandler hTicks
        {
            get
            {
                return horizontalTicks;
            }

            set
            {
                horizontalTicks = value;
            }

        }

        public TimelineControlSettings settings
        {
            get
            {
                return m_Settings;
            }

            set
            {
                if (value != null)
                {
                    m_Settings = value;
                    ApplySettings();
                }
            }
        }

        private class TimeAreaStyle
        {
            public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
            public GUIStyle TimelineTick = "AnimationTimelineTick";
        }
    }
}
