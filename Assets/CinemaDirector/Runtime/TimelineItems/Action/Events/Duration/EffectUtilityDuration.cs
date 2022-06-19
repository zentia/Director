using UnityEngine;
using System.Collections;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
    [EventCategory("Alpha/Skill")]
    public class EffectUtilityDuration : DurationEvent
    {

        public override bool SupportEditMode()
        {
            return true;
        }

        [Template]
        public int targetId = -1;

        public bool enableDirScale = false;
        public int fromId = -1;
        public int toId = -1;
        public bool autoHide = false;

        private bool isHide = false;
        private SkinnedMeshRenderer[] fromObjectMesh = null;
        private SkinnedMeshRenderer[] toObjectMesh = null;

        public enum PivotType
        {
            DIR_X,
            DIR_Y,
            DIR_Z,
        }
        public PivotType pivotType = PivotType.DIR_X;
        public const int SELF_PLAYER_INDEX = 0;
        public const int SOURCE_PLAYER_INDEX = 0;

        public override void Enter(Action _action, Track _track)
        {
            if (autoHide)
            {
                GameObject fromGo = _action.GetGameObject(fromId);
                if(fromGo == null)
                {
                    return;
                }

                GameObject toGo = _action.GetGameObject(toId);
                if(toGo == null)
                {
                    return;
                }

                Transform fromObject = fromGo.transform;
                fromObjectMesh = fromObject.GetComponentsInChildren<SkinnedMeshRenderer>();
				Transform toObject = toGo.transform;
                toObjectMesh = toObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            }
        }

        public override void Leave(Action _action, Track _track)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null)
            {
                Log.LogE("AGE", " Failed to find targetObject: " + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
               "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                return;
            }
            if (autoHide)
            {
                bool hide = CheckAutoHide();
                if (hide)
                {
                    targetObject.SetActive(true);
                }
            }
        }

        public override void Process(Action _action, Track _track, float _localTime)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null )
            {
                Log.LogE("AGE", " Failed to find targetObject: " + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
               "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                return;
            }

            bool hide = CheckAutoHide();
            if (hide)
            {
                targetObject.SetActive(false);
                return;
            }
            else
            {
                targetObject.SetActive(true);
            }

            if (enableDirScale)
            {
                GameObject fromObject = _action.GetGameObject(fromId);
                GameObject toObject = _action.GetGameObject(toId);
                if (fromObject == null)
                {
                    Log.LogE("AGE", " Failed to find fromObject: " + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
                   "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                    return;
                }

                if (toObject == null)
                {
                    Log.LogE("AGE", " Failed to find toObject: " + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
                   "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                    return;
                }

                Transform toTrans = toObject.transform;
                Transform fromTrans = fromObject.transform;
                Transform targetTrans = targetObject.transform;

                targetTrans.LookAt(toTrans);

                float distance = Vector3.Distance(fromTrans.position, toTrans.position);
                distance = Mathf.Max(0.1f, distance);

                Vector3 scaling = targetTrans.localScale;

                if (pivotType == PivotType.DIR_X)
                {
                    scaling.x = distance;
                }
                else if (pivotType == PivotType.DIR_Y)
                {
                    scaling.y = distance;
                }
                else if (pivotType == PivotType.DIR_Z)
                {
                    scaling.z = distance;
                }
                else
                {
                    Log.LogE("AGE", " Error Pivot Type !  " + " for Event: <color=red>[ " + this.GetType().ToString() + " ] </color> " +
                   "by Action:<color=yellow>[ " + _action.name + " ] </color>");
                    return;
                }

                targetTrans.localScale = scaling;
                targetTrans.position = fromTrans.position;
            }
        }

        private bool CheckAutoHide()
        {
            if (!autoHide)
            {
                return false;
            }

            if (fromObjectMesh != null)
            {
                for (int i = 0; i < fromObjectMesh.Length; i++)
                {
                    if (fromObjectMesh[i] != null && !fromObjectMesh[i].enabled)
                    {
                        return true;
                    }
                }
            }

            if (toObjectMesh != null)
            {
                for (int i = 0; i < toObjectMesh.Length; i++)
                {
                    if (toObjectMesh[i] != null && !toObjectMesh[i].enabled)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void CopyData(BaseEvent src)
		{
			base.CopyData(src);
			var srcCopy = src as EffectUtilityDuration;
			targetId = srcCopy.targetId;
			enableDirScale = srcCopy.enableDirScale;
			fromId = srcCopy.fromId;
			toId = srcCopy.toId;
			pivotType = srcCopy.pivotType;
		    autoHide = srcCopy.autoHide;

		}
		
		protected override void ClearData()
		{
			base.ClearData ();
			targetId = -1;
			enableDirScale = false;
			fromId = -1;
			toId = -1;
			pivotType = PivotType.DIR_X;
		    autoHide = false;

		}
    }

}