using UnityEditor;
using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CinemaDirector
{
	internal class DirectorControlSettings : SerializedScriptableObject
	{
		[Serializable]
		public struct Config
		{
			public string metaPath;
			public string AssetsPath
            {
                get
                {
					string path = assetsPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
					if (!path.EndsWith(new string(System.IO.Path.DirectorySeparatorChar, 1)))
                    {
						return path + System.IO.Path.DirectorySeparatorChar;
                    }
					return path;
                }
            }
            [SerializeField, FolderPath]
            private string assetsPath;
        }
		[HideLabel, BoxGroup("局内配置"), SerializeField]
		private Config inside;
		[HideLabel, BoxGroup("局外配置"), SerializeField]
		private Config outside;
		public enum Mode
        {
			[LabelText("局内")]
			inside,
			[LabelText("局外")]
			outside,
        }
		[OnValueChanged("OnChangedMode"), LabelText("工作模式"), ShowInInspector]
		private Mode m_mode;
		public Mode mode 
		{ 
			get 
			{ 
				return m_mode;
			}
		}
		private void OnChangedMode()
        {
			EditorPrefs.SetInt(MODE, (int)m_mode);
        }
		public string assetsPath
        {
            get
            {
				return mode == Mode.inside ? inside.AssetsPath : outside.AssetsPath;
            }			
        }
		public string metaPath
        {
            get
            {
				return mode == Mode.inside ? inside.metaPath : outside.metaPath;
            }
        }
		private float m_HRangeMax = float.PositiveInfinity;

		private float m_HRangeMin = float.NegativeInfinity;

		private bool m_HSlider = true;

		private bool m_ScaleWithWindow = true;
		public const string path = @"Assets\OSGame\Cinema Director\Editor\Resources\DirectorControlSettings.asset";
		public const string Name = "DirectorControlSettings";
		public const string actionPresent = "ActionPresent/";
		private const string COORDINATETYPE = "DirectorControlSettings.CoordinateType";
		private const string MODE = "DirectorControlSettings.Mode";
		public const string CUTSCENEPATH = "DirectorControl.CutscenePath";
		[ShowInInspector, OnValueChanged("OnChangedCorrdinate")]
		private CoordinateType m_CoordinateType;
		private void OnChangedCorrdinate()
        {
			EditorPrefs.SetInt(COORDINATETYPE, (int)m_CoordinateType);
        }
		
		[Button("SaveAndOpenExplore")]
		public void Save()
        {
			var path = AssetDatabase.GetAssetPath(this);
			AssetDatabase.SaveSingleAsset(path);
			EditorUtility.RevealInFinder(path);
        }

		public void OnEnable()
        {
			m_mode = (Mode)EditorPrefs.GetInt(MODE, (int)Mode.outside);
			m_CoordinateType = (CoordinateType)EditorPrefs.GetInt(COORDINATETYPE, (int)CoordinateType.World);
        }
		public CoordinateType coordinateType
        {
            get
            {
				return m_CoordinateType;
            }
			set
            {
				if (value == m_CoordinateType)
					return;
				m_CoordinateType = value;
				EditorPrefs.SetInt(COORDINATETYPE, (int)value);
            }
        }

        internal bool HRangeLocked { get; set; }

        internal float HRangeMax
		{
			get
			{
				return m_HRangeMax;
			}
			set
			{
				m_HRangeMax = value;
			}
		}

		internal float HorizontalRangeMin
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
				m_HSlider = value;
			}
		}

		internal bool ScaleWithWindow
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
	}
}