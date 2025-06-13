using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelineRuntime
{
    [Serializable]
    public class LocalPositionParams
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    // TODO: fold screen

    /// <summary>
    /// 根据不同的屏幕长宽比设置不同的相机参数
    /// </summary>
    [TimelineItem("Transform", "Set Camera Transform", TimelineItemGenre.ActorItem, TimelineItemGenre.TransformItem)]
    public class SetCameraTransformEvent : TimelineActorEvent, IRecoverableObject
    {
        public LocalPositionParams NarrowScreen = new();
        public LocalPositionParams NormalScreen = new();
        // public Vector3 localPosition;
        // public Quaternion localRotation;
        public string ObjectSpaceId;

        // Options for reverting in editor.
        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        // Options for reverting during runtime.
        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        /// <summary>
        /// Cache the state of all actors related to this event.
        /// </summary>
        /// <returns></returns>
        public RevertInfo[] CacheState()
        {
            var actors = GetActor();
            List<RevertInfo> reverts = new List<RevertInfo>();
            {
                Transform go = actors;
                if (go != null)
                {
                    Transform t = go.GetComponent<Transform>();
                    if (t != null)
                    {
                        reverts.Add(new RevertInfo(this, t, "localPosition", t.localPosition));
                        reverts.Add(new RevertInfo(this, t, "localRotation", t.localRotation));
                        reverts.Add(new RevertInfo(this, t, "localScale", t.localScale));
                    }
                }
            }

            return reverts.ToArray();
        }

        public override void Trigger(GameObject actor)
        {
            if (actor == null)
            {
                return;
            }

            Vector3 localPosition = UnityCache.Screen.width / (float)UnityCache.Screen.height > ScreenFitConfig.GetInstance().NarrowScreenThreshold ? NormalScreen.localPosition : NarrowScreen.localPosition;
            Quaternion localRotation = UnityCache.Screen.width / (float)UnityCache.Screen.height > ScreenFitConfig.GetInstance().NarrowScreenThreshold ? NormalScreen.localRotation : NarrowScreen.localRotation;
            if (!string.IsNullOrEmpty(ObjectSpaceId))
            {
                var obj = timeline.GetActor(ObjectSpaceId);
                if (obj != null)
                {
                    actor.transform.position = obj.localToWorldMatrix.MultiplyPoint(localPosition);
                    actor.transform.rotation = obj.rotation * localRotation;
                    return;
                }
            }
            actor.transform.localPosition = localPosition;
            actor.transform.localRotation = localRotation;
        }

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Editor.
        /// </summary>
        public RevertMode EditorRevertMode
        {
            get { return editorRevertMode; }
            set { editorRevertMode = value; }
        }

        /// <summary>
        /// Option for choosing when this Event will Revert to initial state in Runtime.
        /// </summary>
        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}
