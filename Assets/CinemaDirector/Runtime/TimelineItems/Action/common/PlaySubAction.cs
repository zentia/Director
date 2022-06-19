using Assets.Plugins.Common;
using UnityEngine;

namespace AGE
{
    [EventCategory("Alpha/common")]
    public class PlaySubAction : TickEvent
    {
        [ActionReference]
        public string actionName = "";

        public override void Process(Action _action, Track _track)
        {
            Action subAction = _action.PlaySubAction(actionName);
            if(subAction == null)
            {
                Log.LogE("AGE", "[PlaySubAction]播子AGE失败， path = " + actionName);
                return;
            }

            subAction.ResetGameObjects(new ListView<GameObject>(_action.gameObjects));
            object obj = _action.refParams.GetRefParamValue(ActionRefParam.S_ParentActionInstanceID);
            int instanceID = obj == null ? _action.GetInstanceID() : (int)obj;
            subAction.refParams.AddRefParamReset(ActionRefParam.S_ParentActionInstanceID, instanceID);
            subAction.refParams.CopyRefParamFrom(_action);
        }

        public override bool SupportEditMode()
        {
            return true;
        }

        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as PlaySubAction;
            actionName = srcCopy.actionName;
        }
        protected override void ClearData()
        {
            actionName = "";
        }
    }
}

