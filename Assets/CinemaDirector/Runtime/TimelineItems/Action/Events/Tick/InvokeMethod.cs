using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using Assets.Plugins.Common;
using CinemaDirector;

namespace AGE
{

    [EventCategory("Utility")]
    public class InvokeMethod : TickEvent
    {
        public string scriptName = "";   //script name 
        public string methodName = "";      //method name 

        public enum ParamType
        {
            NoParam = 0,
            IntParam = 1,
            FloatParam = 2,
            StringParam = 3,
        };

        public ParamType paramType = ParamType.NoParam;

        public int intParam = 0;
        public float floatParam = 0.0f;
        public string stringParam = "";

        [Template]
        public int targetId = 0;

        public override void Process(Action _action, Track _track)
        {
            GameObject targetObject = _action.GetGameObject(targetId);
            if (targetObject == null) return;

            Component comp = targetObject.GetComponent(scriptName) as Component;
            if (comp == null)
            {
                Log.LogE("AGE", targetObject.name + " doesn't have a " + scriptName + " !");
                return;
            }

            System.Object[] args = new object[] { };
            switch (paramType)
            {
                case ParamType.IntParam:
                    {
                        args = new System.Object[] { intParam };
                        break;
                    }
                case ParamType.FloatParam:
                    {
                        args = new System.Object[] { floatParam };
                        break;
                    }
                case ParamType.StringParam:
                    {
                        args = new System.Object[] { stringParam };
                        break;
                    }
            }

            Type calledType = comp.GetType();
            calledType.InvokeMember(methodName,
                                    BindingFlags.NonPublic |
                                    BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod | BindingFlags.Public |
                                    BindingFlags.Static,
                                    null,
                                    comp,
                                    args);
        }

        protected override void CopyData(BaseEvent src)
        {
            var srcCopy = src as InvokeMethod;
            targetId = srcCopy.targetId;
            scriptName = srcCopy.scriptName;
            methodName = srcCopy.methodName;
            paramType = srcCopy.paramType;
            intParam = srcCopy.intParam;
            floatParam = srcCopy.floatParam;
            stringParam = srcCopy.stringParam;
        }

        protected override void ClearData()
        {
            targetId = -1;
            scriptName = "";
            methodName = "";
            paramType = ParamType.NoParam;
            intParam = 0;
            floatParam = 0.0f;
            stringParam = "";
        }
    }
}

