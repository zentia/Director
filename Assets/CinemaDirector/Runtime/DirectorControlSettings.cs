using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CinemaDirector
{
	public class DirectorControlSettings : SerializedScriptableObject
    {
        private static DirectorControlSettings instance;

        public static DirectorControlSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<DirectorControlSettings>(Name);
                    if (instance == null)
                    {
                        instance = CreateInstance<DirectorControlSettings>();
#if UNITY_EDITOR
						UnityEditor.AssetDatabase.CreateAsset(instance, path);
#endif
					}
				}
				return instance;
            }
        }
		[Serializable]
		public struct Config
		{
			public string AssetsPath
            {
                get
                {
                    if (string.IsNullOrEmpty(assetsPath))
                    {
                        return "";
                    }
					var path = assetsPath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
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
			PlayerPrefs.SetInt(MODE, (int)m_mode);
        }
		public string assetsPath
        {
            get
            {
				return mode == Mode.inside ? inside.AssetsPath : outside.AssetsPath;
            }			
        }
		
		private float m_HRangeMax = float.PositiveInfinity;

		private float m_HRangeMin = float.NegativeInfinity;

		private bool m_HSlider = true;

		private bool m_ScaleWithWindow = true;
		public const string path = @"Assets\OSGame\Cinema Director\Resources\DirectorControlSettings.asset";
		public const string Name = "DirectorControlSettings";
		public const string actionPresent = "ActionPresent/";
		private const string COORDINATETYPE = "DirectorControlSettings.CoordinateType";
		private const string MODE = "DirectorControlSettings.Mode";
		public const string CUTSCENEPATH = "DirectorControl.CutscenePath";
		[ShowInInspector, OnValueChanged("OnChangedCorrdinate")]
		private CoordinateType m_CoordinateType;
		private void OnChangedCorrdinate()
        {
			PlayerPrefs.SetInt(COORDINATETYPE, (int)m_CoordinateType);
        }

#if UNITY_EDITOR
		[Button("SaveAndOpenExplore")]
        public void Save()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            UnityEditor.AssetDatabase.SaveSingleAsset(path);
            UnityEditor.EditorUtility.RevealInFinder(path);
        }
#endif


		public void OnEnable()
        {
			m_mode = (Mode)PlayerPrefs.GetInt(MODE, (int)Mode.outside);
			m_CoordinateType = (CoordinateType)PlayerPrefs.GetInt(COORDINATETYPE, (int)CoordinateType.World);
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
				PlayerPrefs.SetInt(COORDINATETYPE, (int)value);
            }
        }

        public bool HRangeLocked { get; set; }

        public float HRangeMax
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

		public float HorizontalRangeMin
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

		public bool HSlider
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

		public bool ScaleWithWindow
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