using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TimelineRuntime
{
    [TimelineItem("Transform", "Set Transform", TimelineItemGenre.ActorItem, TimelineItemGenre.TransformItem)]
    public class SetTransformEvent : TimelineActorEvent, IRecoverableObject
    {
        public enum TransformSpace
        {
            Object,
            Local,
            World
        }

        [FormerlySerializedAs("localPosition")] public Vector3 Position;
        [FormerlySerializedAs("localRotation")] public Quaternion Rotation;
        [ObjectSpace]
        public ObjectSpace ObjectSpaceId = new();

        public TransformSpace transformSpace = TransformSpace.Object;

        [SerializeField]
        private RevertMode editorRevertMode = RevertMode.Revert;

        [SerializeField]
        private RevertMode runtimeRevertMode = RevertMode.Revert;

        public RevertInfo[] CacheState()
        {
            var actors = (GetActor());
            List<RevertInfo> reverts = new List<RevertInfo>();
            {
                Transform go = actors;
                if (go != null)
                {
                    Transform t = go.GetComponent<Transform>();
                    if (t != null)
                    {
                        switch (transformSpace)
                        {
                            case TransformSpace.Object:
                                reverts.Add(new RevertInfo(this, t, "position", t.position));
                                reverts.Add(new RevertInfo(this, t, "rotation", t.rotation));
                                reverts.Add(new RevertInfo(this, t, "localScale", t.localScale));
                                break;
                            case TransformSpace.Local:
                                reverts.Add(new RevertInfo(this, t, "localPosition", t.localPosition));
                                reverts.Add(new RevertInfo(this, t, "localRotation", t.localRotation));
                                reverts.Add(new RevertInfo(this, t, "localScale", t.localScale));
                                break;
                            case TransformSpace.World:
                                reverts.Add(new RevertInfo(this, t, "position", t.position));
                                reverts.Add(new RevertInfo(this, t, "rotation", t.rotation));
                                reverts.Add(new RevertInfo(this, t, "localScale", t.localScale));
                                break;
                        }
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

            switch (transformSpace)
            {
                case TransformSpace.Object:
                    var obj = timeline.GetActor(ObjectSpaceId);
                    if (obj != null)
                    {
                        actor.transform.position = obj.localToWorldMatrix.MultiplyPoint(Position);
                        actor.transform.rotation = obj.rotation * Rotation;
                        return;
                    }
                    actor.transform.localPosition = Position;
                    actor.transform.localRotation = Rotation;
                    break;
                case TransformSpace.Local:
                    actor.transform.localPosition = Position;
                    actor.transform.localRotation = Rotation;
                    break;
                case TransformSpace.World:
                    actor.transform.position = Position;
                    actor.transform.rotation = Rotation;
                    break;
            }
        }

        public RevertMode EditorRevertMode
        {
            get { return editorRevertMode; }
            set { editorRevertMode = value; }
        }

        public RevertMode RuntimeRevertMode
        {
            get { return runtimeRevertMode; }
            set { runtimeRevertMode = value; }
        }
    }
}
