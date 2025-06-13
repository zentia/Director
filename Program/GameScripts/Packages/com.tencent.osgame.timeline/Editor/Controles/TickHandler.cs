using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineEditor
{
    public class TickHandler
    {
        private int m_BiggestTick = -1;
        private float m_MaxValue = 1f;
        private float m_MinValue;
        private float m_PixelRange = 1f;
        private int m_SmallestTick;
        private float[] m_TickModulos = new float[] { 0.001f/3.0f, 0.01f/3.0f, 0.1f/3.0f, 1f/3.0f, 10f/3.0f, 100f/3.0f, 1000f/3.0f, 10000f/3.0f };
        private float[] m_TickStrengths = new float[0];

        internal int GetLevelWithMinSeparation(float pixelSeparation)
        {
            for (int i = 0; i < m_TickModulos.Length; i++)
            {
                if ((m_TickModulos[i] * m_PixelRange / (m_MaxValue - m_MinValue)) >= pixelSeparation)
                {
                    return i - m_SmallestTick;
                }
            }
            return -1;
        }

        internal float GetPeriodOfLevel(int level) =>
            m_TickModulos[Mathf.Clamp(m_SmallestTick + level, 0, m_TickModulos.Length - 1)];

        internal float GetStrengthOfLevel(int level) =>
            m_TickStrengths[m_SmallestTick + level];

        internal float[] GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels)
        {
            int index = Mathf.Clamp(m_SmallestTick + level, 0, m_TickModulos.Length - 1);
            List<float> list = new List<float>();
            int num2 = Mathf.CeilToInt(m_MaxValue / m_TickModulos[index]);
            for (int i = Mathf.FloorToInt(m_MinValue / m_TickModulos[index]); i <= num2; i++)
            {
                if (!excludeTicksFromHigherlevels || (index >= m_BiggestTick) || ((i % Mathf.RoundToInt(m_TickModulos[index + 1] / m_TickModulos[index])) != 0))
                {
                    list.Add(i * m_TickModulos[index]);
                }
            }
            return list.ToArray();
        }

        internal void SetRanges(float minValue, float maxValue, float minPixel, float maxPixel)
        {
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_PixelRange = maxPixel - minPixel;
        }

        internal void SetTickStrengths(float tickMinSpacing, float tickMaxSpacing, bool sqrt)
        {
            m_TickStrengths = new float[m_TickModulos.Length];
            m_SmallestTick = 0;
            m_BiggestTick = m_TickModulos.Length - 1;
            for (int i = m_TickModulos.Length - 1; i >= 0; i--)
            {
                float num2 = m_TickModulos[i] * m_PixelRange / (m_MaxValue - m_MinValue);
                m_TickStrengths[i] = (num2 - tickMinSpacing) / (tickMaxSpacing - tickMinSpacing);
                if (m_TickStrengths[i] >= 1f)
                {
                    m_BiggestTick = i;
                }
                if (num2 <= tickMinSpacing)
                {
                    m_SmallestTick = i;
                    break;
                }
            }
            for (int j = m_SmallestTick; j <= m_BiggestTick; j++)
            {
                m_TickStrengths[j] = Mathf.Clamp01(m_TickStrengths[j]);
                if (sqrt)
                {
                    m_TickStrengths[j] = Mathf.Sqrt(m_TickStrengths[j]);
                }
            }
        }

        internal int TickLevels => m_BiggestTick - m_SmallestTick + 1;
    }
}
