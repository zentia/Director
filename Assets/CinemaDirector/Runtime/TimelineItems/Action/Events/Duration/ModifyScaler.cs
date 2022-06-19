using UnityEngine;
using System;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{
	[Serializable]
	[EventCategory("ModifyScaler")]
	public class ModifyScaler : DurationEvent
	{
		[Template]
		public int modifyTargetId = 0;//要修改的目标

		public bool useReferenceTarget = false;//是否使用参照物
		public Vector3 targetScale = Vector3.one;//如果不使用参照物的目标缩放值
		[Template]
		public int referenceTarget = 0;//参照物

		private Vector3 _finalModifyScale;//最终真正要修改的scale
		private Vector3 _originScale;

		public override bool SupportEditMode()
		{
			return true;
		}

		public override void Enter(Action _action, Track _track)
		{
			GameObject modifyGo = _action.GetGameObject(modifyTargetId);
			if (modifyGo == null)
			{
				Log.LogE("AGE", "<color=red>[ ModifyScaler No modifyTargetId GameObject]</color> action:" + _action.actionName);
				return;
			}

			_originScale = modifyGo.transform.localScale;

			if (useReferenceTarget)
            {
				GameObject refGo = _action.GetGameObject(referenceTarget);
				if(refGo != null)
                {
					_finalModifyScale = refGo.transform.localScale;
                }
                else
                {
					Log.LogE("AGE", "<color=red>[ ModifyScaler No Ref GameObject]</color> action:" + _action.actionName);
				}
			}
			else
			{
				_finalModifyScale = targetScale;

			}
		}

		public override void Process(Action _action, Track _track, float _localTime)
		{
			GameObject modifyGo = _action.GetGameObject(modifyTargetId);
			if (modifyGo != null)
			{
				float percent = _localTime / length;
				Vector3 resultScaler = Vector3.Lerp(_originScale, _finalModifyScale, percent);
				modifyGo.transform.localScale = resultScaler;
			}
		}

		public override void Leave(Action _action, Track _track)
		{
			GameObject modifyGo = _action.GetGameObject(modifyTargetId);
			if (modifyGo != null)
			{
				modifyGo.transform.localScale = _finalModifyScale;
			}
		}


		protected override void CopyData(BaseEvent src)
		{
			base.CopyData(src);
			ModifyScaler r = src as ModifyScaler;
			modifyTargetId = r.modifyTargetId;
			useReferenceTarget = r.useReferenceTarget;
			targetScale = r.targetScale;
			referenceTarget = r.referenceTarget;
		}

		protected override void ClearData()
		{
			base.ClearData();
			modifyTargetId = -1;
			useReferenceTarget = true;
			targetScale = Vector3.one;
			referenceTarget = -1;
		}
	}
}
