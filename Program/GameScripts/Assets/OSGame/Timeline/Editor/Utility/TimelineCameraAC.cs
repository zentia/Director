using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Plugins.Common;
using Assets.Scripts.GameLogic;
using TimelineRuntime;
using UnityEditor;
using UnityEngine;

namespace TimelineEditor
{
    public class TimelineCameraAC
    {
        private class CameraParamDiff
        {
            private TimelineCameraParam m_Param;

            public Vector3 pos { private set; get; }
            public float fov { private set; get; }

            public CameraParamDiff(TimelineCameraParam param)
            {
                m_Param = param;
            }

            public void Diff(TimelineCameraParam other)
            {
                pos = m_Param.position - other.position;
                fov = m_Param.fov - other.fov;
            }
        }

        public class CameraGenerateAssist
        {
            public string baseCameraAC { private set; get; }
            private ScreenFitConfig.CameraCategory m_BaseCategory;
            private CameraParamDiff m_Diff;

            public string cameraType
            {
                get => m_CameraType;
                set
                {
                    if (m_CameraType != value)
                    {
                        m_CameraType = value;
                        if (!string.IsNullOrEmpty(value) && GetCameraParam(value, m_BaseCategory, out var param))
                        {
                            m_Diff = new CameraParamDiff(param);
                        }
                        else
                        {
                            m_Diff = null;
                        }
                    }
                }
            }
            private string m_CameraType;

            public CameraGenerateAssist(ScreenFitConfig.CameraCategory baseCategory = ScreenFitConfig.CameraCategory.UltraWide)
            {
                baseCameraAC = ScreenFitConfig.GetTimelineSceneCameraTrackName(baseCategory);
                m_BaseCategory = baseCategory;
            }

            public bool GenCameraByType(Timeline timeline, ScreenFitConfig.CameraCategory toCategory)
            {
                if (null == m_Diff)
                    return false;

                List<GameObject> fromGoList = CollectGoList(timeline, baseCameraAC);
                if (fromGoList.Count == 0)
                    return false;

                var acKey = ScreenFitConfig.GetTimelineSceneCameraTrackName(toCategory);
                List<GameObject> goList = CollectGoList(timeline, acKey);
                foreach (var go in goList)
                {
                    UnityEngine.GameObject.DestroyImmediate(go);
                }

                if (!GetCameraParam(cameraType, toCategory, out var param))
                    return false;
                m_Diff.Diff(param);

                foreach (var go in fromGoList)
                {
                    GameObject newGo = GameObject.Instantiate(go.gameObject, go.transform.parent, true);
                    FixCameraCurve(newGo, toCategory);
                    newGo.name = acKey;
                    newGo.transform.SetSiblingIndex(fromGoList.Last().transform.GetSiblingIndex() + 1);
                }

                AlignLastKeyFrameValue(timeline, cameraType, toCategory);

                return true;
            }

            public bool HasCameraAC(Timeline timeline, string childName)
            {
                if (null != timeline)
                {
                    for (int i = 0, childCount = timeline.transform.childCount; i < childCount; i++)
                    {
                        var child = timeline.transform.GetChild(i);
                        if (null != child && child.name == childName)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private List<GameObject> CollectGoList(Timeline timeline, string name)
            {
                List<GameObject> goList = new List<GameObject>();
                if (null != timeline)
                {
                    for (int i = 0, childCount = timeline.transform.childCount; i < childCount; i++)
                    {
                        var child = timeline.transform.GetChild(i);
                        if (null != child && child.name == name)
                        {
                            goList.Add(child.gameObject);
                        }
                    }
                }

                return goList;
            }

            private void FixCameraCurve(GameObject go, ScreenFitConfig.CameraCategory category)
            {
                var curveClipList = go.GetComponentsInChildren<TimelineActorCurveClip>(true);
                foreach (var curveClip in curveClipList)
                {
                    foreach (var curveData in curveClip.CurveData)
                    {
                        if (curveData.IsProperty)
                        {
                            switch (curveData.PropertyName)
                            {
                                case "localPosition":
                                    const int fadeKeyFrameNum = 5;
                                    FixCurve(ref curveData.Curve1, m_Diff.pos.x, fadeKeyFrameNum);
                                    FixCurve(ref curveData.Curve2, m_Diff.pos.y, fadeKeyFrameNum);
                                    FixCurve(ref curveData.Curve3, m_Diff.pos.z, fadeKeyFrameNum);
                                    break;
                                case "fieldOfView":
                                    FixCurve(ref curveData.Curve1, m_Diff.fov);
                                    break;
                            }
                        }
                    }
                }
            }

            private void FixCurve(ref AnimationCurve curve, float diff, int fadeKeyFrameNum = 0)
            {
                if (null != curve.keys)
                {
                    if (curve.keys.Length == 0)
                        return;

                    int indexFrom = Math.Max(0, curve.keys.Length - 1 - fadeKeyFrameNum);
                    int indexTo = Math.Min(indexFrom + fadeKeyFrameNum, curve.keys.Length - 1);

                    Keyframe[] keyframes = new Keyframe[curve.keys.Length];
                    Array.Copy(curve.keys, keyframes, curve.keys.Length);
                    for (int i = 0; i < keyframes.Length; i++)
                    {
                        float fixedDiff = 0;
                        if ( i >= indexFrom && indexTo > indexFrom )
                        {
                            var kf = keyframes[i];
                            float totalTime = keyframes[indexTo].time - keyframes[indexFrom].time;
                            fixedDiff = UnityEngine.Mathf.Lerp(0, diff, (kf.time - keyframes[indexFrom].time) / totalTime);
                        }

                        keyframes[i].value -= fixedDiff;
                    }
                    curve.keys = keyframes;
                }
            }

            // 把相机曲线最后关键帧的值，对齐到相机配置参数生效的状态
            public bool AlignLastKeyFrameValue(Timeline timeline, string cameraType,
                ScreenFitConfig.CameraCategory toCategory)
            {
                if (!GetCameraParam(cameraType, toCategory, out var param))
                    return false;

                float fov = param.fov;
                Vector3 pos = param.position;
                Vector3 eulerAngles = param.rotation;

                var acKey = ScreenFitConfig.GetTimelineSceneCameraTrackName(toCategory);
                List<GameObject> goList = CollectGoList(timeline, acKey);
                foreach (var go in goList)
                {
                    var curveClipList = go.GetComponentsInChildren<TimelineActorCurveClip>(true);
                    foreach (var curveClip in curveClipList)
                    {
                        foreach (var curveData in curveClip.CurveData)
                        {
                            if (curveData.IsProperty)
                            {
                                switch (curveData.PropertyName)
                                {
                                    case "localPosition":
                                        AlignLastKeyFrameValue(ref curveData.Curve1, pos.x);
                                        AlignLastKeyFrameValue(ref curveData.Curve2, pos.y);
                                        AlignLastKeyFrameValue(ref curveData.Curve3, pos.z);
                                        break;
                                    case "localEulerAngles":
                                        AlignLastKeyFrameValue(ref curveData.Curve1, eulerAngles.x, true);
                                        AlignLastKeyFrameValue(ref curveData.Curve2, eulerAngles.y, true);
                                        AlignLastKeyFrameValue(ref curveData.Curve3, eulerAngles.z, true);
                                        break;
                                    case "localRotation":
                                        var tempRot = Quaternion.Euler(
                                            new Vector3(eulerAngles.x, eulerAngles.y + 360,
                                                eulerAngles.z)
                                        );
                                        AlignLastKeyFrameValue(ref curveData.Curve1, tempRot.x);
                                        AlignLastKeyFrameValue(ref curveData.Curve2, tempRot.y);
                                        AlignLastKeyFrameValue(ref curveData.Curve3, tempRot.z);
                                        AlignLastKeyFrameValue(ref curveData.Curve4, tempRot.w);
                                        break;
                                    case "fieldOfView":
                                        AlignLastKeyFrameValue(ref curveData.Curve1, fov);
                                        break;
                                }
                            }
                        }
                    }
                }

                return goList.Count > 0;
            }

            private void AlignLastKeyFrameValue(ref AnimationCurve curve, float value, bool isRot = false)
            {
                if (null != curve.keys)
                {
                    if (curve.keys.Length == 0)
                        return;

                    if (isRot)
                    {
                        var old = curve.keys[^1].value;
                        const float tolerance = 0.001f;
                        if (Math.Abs(value - old) < tolerance || Math.Abs(value + 360 - old) < 0.001f)
                        {
                            return;
                        }
                    }

                    Keyframe[] keyframes = new Keyframe[curve.keys.Length];
                    Array.Copy(curve.keys, keyframes, curve.keys.Length);
                    keyframes[keyframes.Length - 1].value = value;
                    curve.keys = keyframes;
                }
            }

            private bool GetCameraParam(string type, ScreenFitConfig.CameraCategory category, out TimelineCameraParam param)
            {
                if (type == CameraTypeBattle)
                {
                    if (Project8FreeCameraConfig.GetParamByType(category,
                            Project8FreeCameraConfig.Project8FreeCameraConfigType.Battle, out var cameraParam))
                    {
                        cameraParam.GetTransform(out var pos, out var rot);
                        param.fov = cameraParam.track.fov;
                        param = new TimelineCameraParam()
                        {
                            position = pos,
                            rotation = rot.eulerAngles,
                            fov = cameraParam.track.fov,
                        };
                        return true;
                    }
                }
                else
                {
                    if (TimelineCameraConfig.GetParamByType(category, type, out param))
                    {
                        return true;
                    }
                }

                param = new TimelineCameraParam();
                return false;
            }
        }

        public static string CameraTypeBattle = "Battle";

        private TimelineWindow m_Owner;
        private Timeline timeline => m_Owner.timeline;

        private GenericMenu m_CameraTypeMenu;
        private GUIContent m_CameraTypeTitle = new GUIContent("");
        private string m_CameraType;

        private GenericMenu m_Menu;

        private CameraGenerateAssist m_Assist = new CameraGenerateAssist(ScreenFitConfig.CameraCategory.UltraWide);

        private IEnumerable<ScreenFitConfig.CameraCategory> CameraTypeList =>
            Enum.GetValues(typeof(ScreenFitConfig.CameraCategory))
                .Cast<ScreenFitConfig.CameraCategory>()
                .Where(type => type != ScreenFitConfig.CameraCategory.UltraWide);

        public TimelineCameraAC(TimelineWindow owner)
        {
            m_Owner = owner;

            m_CameraTypeMenu = new GenericMenu();

            m_Menu = new GenericMenu();
            m_Menu.AddItem(new GUIContent("All"), false, GenAllTypeCamera);
            m_Menu.AddSeparator("");
            foreach (var type in CameraTypeList.Reverse())
            {
                m_Menu.AddItem(new GUIContent(ScreenFitConfig.GetTimelineSceneCameraTrackName(type)), false, GenCameraByType, type);
            }
        }

        public void Reset()
        {
            m_CameraTypeMenu = new GenericMenu();
            m_CameraTypeMenu.AddItem(new GUIContent(CameraTypeBattle), false, SetCameraType, CameraTypeBattle);
            foreach (var type in TimelineCameraConfig.GetInstance().AllConfigTypeNames)
            {
                m_CameraTypeMenu.AddItem(new GUIContent(type), false, SetCameraType, type);
            }
            SetCameraType(string.Empty);
        }

        public void Draw(ref Rect rect)
        {
            rect.x += rect.width;
            m_CameraTypeTitle.text = string.IsNullOrEmpty(m_CameraType) ? "请选择相机场景" : $"相机场景：{m_CameraType}";
            rect.width = EditorStyles.label.CalcSize(m_CameraTypeTitle).x + 20;
            if (GUI.Button(rect, m_CameraTypeTitle, EditorStyles.toolbarDropDown))
            {
                m_CameraTypeMenu.DropDown(new Rect(rect.x, 17, 0, 0));
            }

            rect.x += rect.width;
            rect.width = 80;

            GUI.enabled = !string.IsNullOrEmpty(m_CameraType) && m_Assist.HasCameraAC(timeline, m_Assist.baseCameraAC);
            if (GUI.Button(rect, "CameraAC", EditorStyles.toolbarDropDown))
            {
                m_Menu.DropDown(new Rect(rect.x, 17, 0, 0));
            }

            GUI.enabled = true;
        }

        private void SetCameraType(object userdata)
        {
            m_CameraType = (string)userdata;
            m_Assist.cameraType = m_CameraType;
        }

        private void GenAllTypeCamera()
        {
            foreach (var type in CameraTypeList)
            {
                GenCameraByType(type);
            }
        }

        private void GenCameraByType(object userData)
        {
            var category = (ScreenFitConfig.CameraCategory)userData;
            if (m_Assist.GenCameraByType(timeline, category))
            {
                timeline.OnValidate();
            }
            else
            {
                Log.LogE(LogTag.Unknown,
                    $"generate camera actor group failed! cameraType {m_CameraType}, category {category.ToString()}");
            }
        }
    }
}
