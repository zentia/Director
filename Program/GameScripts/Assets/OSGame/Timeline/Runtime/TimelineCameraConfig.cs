using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Plugins.Common;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TimelineEditor
{
    public enum TimelineCameraConfigType
    {
    }

    [Serializable]
    public struct TimelineCameraParam
    {
        public float fov;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable]
    public struct TimelineCameraConfigData
    {
        [ValueDropdown("GetTypeNames")]
        public string type;

        [Title("超宽,[2:1,~)"), HideLabel]
        public TimelineCameraParam ultraWideParam;

        [Title("宽,[16.9,2:1)"), HideLabel]
        public TimelineCameraParam wideParam;

        [Title("标准,[4:3,16.9)"), HideLabel]
        public TimelineCameraParam standardParam;

        [Title("方形,[1:1, 4:3)"), HideLabel]
        public TimelineCameraParam squareParam;

        public bool GetCameraParam(ScreenFitConfig.CameraCategory cameraFit, out TimelineCameraParam param)
        {
            switch (cameraFit)
            {
                case ScreenFitConfig.CameraCategory.Square:
                    param = squareParam;
                    break;
                case ScreenFitConfig.CameraCategory.Standard:
                    param = standardParam;
                    break;
                case ScreenFitConfig.CameraCategory.Wide:
                    param = wideParam;
                    break;
                case ScreenFitConfig.CameraCategory.UltraWide:
                    param = ultraWideParam;
                    break;
                default:
                    param = ultraWideParam;
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private string[] GetTypeNames => TimelineCameraConfig.GetInstance().AllConfigTypeNames;
#endif
    }

    [Serializable]
    public class TimelineCameraConfig : ScriptableObject
    {
        private static TimelineCameraConfig instance;
        private const string CONFIG_PATH = "GamePlay/project8/Prefab_Level/TimelineCameraConfig";

#if UNITY_EDITOR
        private string[] m_TypeNames;
#endif

        [SerializeField, ShowInInspector] private List<string> customConfigTypes = new();

        [SerializeField, ShowInInspector] private List<TimelineCameraConfigData> configList = new();

        public static TimelineCameraConfig GetInstance()
        {
            if (instance == null)
            {
                var resource = CResourceManager.Load<TimelineCameraConfig>(CONFIG_PATH, false);
                if (resource != null)
                {
                    instance = resource;
#if UNITY_EDITOR
                    List<string> list = new List<string>();
                    list.AddRange(instance.customConfigTypes);
                    list.AddRange(Enum.GetNames(typeof(TimelineCameraConfigType)));
                    instance.m_TypeNames = list.ToArray();
#endif
                }
            }
            return instance;
        }

#if UNITY_EDITOR
        public string[] AllConfigTypeNames => instance.m_TypeNames;
#endif

        public static bool GetParamByType(TimelineCameraConfigType type, out TimelineCameraParam param)
        {
            return GetParamByType(ScreenFitConfig.GetInstance().GetCameraCategory(), type, out param);
        }

        public static bool GetParamByType(ScreenFitConfig.CameraCategory cameraFit, TimelineCameraConfigType type, out TimelineCameraParam param)
        {
            return GetParamByType(cameraFit, type.ToString(), out param);
        }

        public static bool GetParamByType(ScreenFitConfig.CameraCategory cameraFit, string type,
            out TimelineCameraParam param)
        {
            if (GetInstance() != null)
            {
                for (int i = 0; i < instance.configList.Count; i++)
                {
                    if (instance.configList[i].type == type)
                    {
                        var config= instance.configList[i];
                        if (config.GetCameraParam(cameraFit, out param))
                        {
                            return true;
                        }
                        break;
                    }
                }
            }
            Log.LogE(LogTag.Unknown,"GetParamByType Error {0}", type.ToString());
            param = new TimelineCameraParam();
            return false;
        }

        public IEnumerable<TimelineCameraParam> GetAllParamByType(TimelineCameraConfigType type)
        {
            if (GetInstance() != null)
            {
                for (int i = 0; i < instance.configList.Count; i++)
                {
                    if (instance.configList[i].type == type.ToString())
                    {
                        var config= instance.configList[i];
                        yield return config.ultraWideParam;
                        yield return config.wideParam;
                        yield return config.standardParam;
                        yield return config.squareParam;
                        break;
                    }
                }
            }
        }

#if UNITY_EDITOR
        [Button]
        private static void ReloadConfig()
        {
            EditorUtility.SetDirty(instance);
            instance = null;
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(CONFIG_PATH));
        }
#endif

        public void SetConfigData(TimelineCameraConfigData data)
        {
            if (customConfigTypes.IndexOf(data.type) == -1)
            {
                customConfigTypes.Add(data.type);
            }

            var index = configList.FindIndex(o => o.type == data.type);
            if (index == -1)
            {
                configList.Add(data);
            }
            else
            {
                configList[index] = data;
            }
        }
    }
}
