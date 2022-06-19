using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
	internal class TickHandler
	{
		private int m_BiggestTick = -1;
		private float m_MaxValue = 1f;
		private float m_MinValue;
		private float m_PixelRange = 1f;
		private int m_SmallestTick;
		private float[] m_TickModulos = new float[0];
		private float[] m_TickStrengths = new float[0];
		internal int tickLevels
		{
			get
			{
				return m_BiggestTick - m_SmallestTick + 1;
			}
		}

		internal int GetLevelWithMinSeparation(float pixelSeparation)
		{
			for (int i = 0; i < m_TickModulos.Length; i++)
			{
				if (this.m_TickModulos[i] * this.m_PixelRange / (this.m_MaxValue - this.m_MinValue) >= pixelSeparation)
				{
					return i - this.m_SmallestTick;
				}
			}
			return -1;
		}

		internal float GetPeriodOfLevel(int level)
		{
			return this.m_TickModulos[Mathf.Clamp(this.m_SmallestTick + level, 0, this.m_TickModulos.Length - 1)];
		}

		internal float GetStrengthOfLevel(int level)
		{
			return this.m_TickStrengths[this.m_SmallestTick + level];
		}

		internal float[] GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels)
		{
			int num = Mathf.Clamp(this.m_SmallestTick + level, 0, this.m_TickModulos.Length - 1);
			List<float> list = new List<float>();
			int arg_48_0 = Mathf.FloorToInt(this.m_MinValue / this.m_TickModulos[num]);
			int num2 = Mathf.CeilToInt(this.m_MaxValue / this.m_TickModulos[num]);
			for (int i = arg_48_0; i <= num2; i++)
			{
				if (!excludeTicksFromHigherlevels || num >= this.m_BiggestTick || i % Mathf.RoundToInt(this.m_TickModulos[num + 1] / this.m_TickModulos[num]) != 0)
				{
					list.Add((float)i * this.m_TickModulos[num]);
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

		internal void SetTickModulos(float[] tickModulos)
		{
			m_TickModulos = tickModulos;
		}

		internal void SetTickModulosForFrameRate(float frameRate)
		{
			if (frameRate != Mathf.Round(frameRate))
			{
				float[] tickModulos = new float[]
				{
					1f / frameRate,
					5f / frameRate,
					10f / frameRate,
					50f / frameRate,
					100f / frameRate,
					500f / frameRate,
					1000f / frameRate,
					5000f / frameRate,
					10000f / frameRate,
					50000f / frameRate,
					100000f / frameRate,
					500000f / frameRate
				};
				SetTickModulos(tickModulos);
				return;
			}
			List<int> list = new List<int>();
			int num = 1;
			while ((float)num < frameRate && (float)num != frameRate)
			{
				int num2 = Mathf.RoundToInt(frameRate / (float)num);
				if (num2 % 60 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 30 == 0)
				{
					num *= 3;
					list.Add(num);
				}
				else if (num2 % 20 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 10 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 5 == 0)
				{
					num *= 5;
					list.Add(num);
				}
				else if (num2 % 2 == 0)
				{
					num *= 2;
					list.Add(num);
				}
				else if (num2 % 3 == 0)
				{
					num *= 3;
					list.Add(num);
				}
				else
				{
					num = Mathf.RoundToInt(frameRate);
				}
			}
			float[] array = new float[9 + list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				float[] arg_17E_0 = array;
				int arg_17E_1 = i;
				float arg_17D_0 = 1f;
				List<int> expr_16C = list;
				arg_17E_0[arg_17E_1] = arg_17D_0 / (float)expr_16C[expr_16C.Count - i - 1];
			}
			float[] expr_190 = array;
			expr_190[expr_190.Length - 1] = 3600f;
			float[] expr_19C = array;
			expr_19C[expr_19C.Length - 2] = 1800f;
			float[] expr_1A8 = array;
			expr_1A8[expr_1A8.Length - 3] = 600f;
			float[] expr_1B4 = array;
			expr_1B4[expr_1B4.Length - 4] = 300f;
			float[] expr_1C0 = array;
			expr_1C0[expr_1C0.Length - 5] = 60f;
			float[] expr_1CC = array;
			expr_1CC[expr_1CC.Length - 6] = 30f;
			float[] expr_1D8 = array;
			expr_1D8[expr_1D8.Length - 7] = 10f;
			float[] expr_1E4 = array;
			expr_1E4[expr_1E4.Length - 8] = 5f;
			float[] expr_1F0 = array;
			expr_1F0[expr_1F0.Length - 9] = 1f;
			SetTickModulos(array);
		}

		internal void SetTickStrengths(float tickMinSpacing, float tickMaxSpacing, bool sqrt)
		{
			m_TickStrengths = new float[m_TickModulos.Length];
			this.m_SmallestTick = 0;
			this.m_BiggestTick = m_TickModulos.Length - 1;
			for (int i = this.m_TickModulos.Length - 1; i >= 0; i--)
			{
				float num = this.m_TickModulos[i] * this.m_PixelRange / (this.m_MaxValue - this.m_MinValue);
				this.m_TickStrengths[i] = (num - tickMinSpacing) / (tickMaxSpacing - tickMinSpacing);
				if (this.m_TickStrengths[i] >= 1f)
				{
					this.m_BiggestTick = i;
				}
				if (num <= tickMinSpacing)
				{
					this.m_SmallestTick = i;
					break;
				}
			}
			for (int j = this.m_SmallestTick; j <= this.m_BiggestTick; j++)
			{
				this.m_TickStrengths[j] = Mathf.Clamp01(this.m_TickStrengths[j]);
				if (sqrt)
				{
					this.m_TickStrengths[j] = Mathf.Sqrt(this.m_TickStrengths[j]);
				}
			}
		}
	}
}