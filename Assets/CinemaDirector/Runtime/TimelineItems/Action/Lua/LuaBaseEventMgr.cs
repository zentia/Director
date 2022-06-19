using AGE;
using AGE.Lua;
using Assets.Plugins.Common;
using System.Collections.Generic;
using XLua;

public static class LuaBaseEventMgr
{
    public enum BaseEventMethodFlag
    {
        None = 0,
        OnActionStart = 1,
        OnActionStop = 1 << 1,
        GetAssociatedAction = 1 << 2,
        GetAssociatedResources = 1 << 3,
        TickProcess = 1 << 4,
        TickProcessBlend = 1 << 5,
        TickPostProcess = 1 << 6,
        DurationEnter = 1 << 7,
        DurationLeave = 1 << 8,
        DurationProcess = 1 << 9,
    }

    public delegate void FuncActionVoid(LuaTable lua, Action action);
    public delegate bool FuncVoidBool(LuaTable lua);
    public delegate void FuncVoidTable(LuaTable lua, LuaTable lua2);
    public delegate LuaTable FuncStrEventTable(string eventType, BaseEvent eve, LuaTable srcTbl);
    public delegate bool FuncStrBool(string eventType);
    public delegate void FuncVoidStrObj(LuaTable lua, string str, object value);
    public delegate void FuncVoidList(LuaTable lua, List<string> list);
    public delegate void FuncVoidDic(LuaTable lua, Dictionary<string, bool> dic);
    public delegate void FuncTickProcessBlend(LuaTable lua, Action _action, Track _track, TickEvent _prevEvent, float _blendWeight);
    public delegate void FuncActionTrackVoid(LuaTable lua, Action _action, Track _track);
    public delegate void FuncActionTrackFloatVoid(LuaTable lua, Action _action, Track _track, float _localTime);

    //private static FuncStrBool _isSmoothEnable = null;
    private static FuncStrEventTable _getLuaBaseEvent = null;
    //private static FuncStrBool _isDuration = null;
    //private static FuncStrBool _isCondition = null;
    private static FuncVoidTable _copyData = null;
    //private static FuncStrBool _isLuaEvent = null;
    private static FuncVoidStrObj _setFieldInfo = null;
    private static FuncActionVoid _onActionStart = null;
    private static FuncActionVoid _onActionStop = null;
    private static FuncVoidList _getAssociatedAction = null;
    private static FuncVoidDic _getAssociatedResources = null;
    private static FuncActionTrackVoid _tickProcess = null;
    private static FuncTickProcessBlend _tickProcessBlend = null;
    private static FuncActionTrackFloatVoid _tickPostProcess = null;
    private static FuncActionTrackVoid _durationEnter = null;
    private static FuncActionTrackVoid _durationLeave = null;
    private static FuncActionTrackFloatVoid _durationProcess = null;
    private static FuncVoidBool _clearData = null;

    private static Dictionary<string, LuaBaseEventMeta> _allLuaEventTypeMeta = new Dictionary<string, LuaBaseEventMeta>();
    
    public class LuaBaseEventMeta
    {
        public bool IsDuration = false;
        public bool IsCondition = false;
        public bool IsSmoothEnable = false;
        public int MethodFlag = 0;

        public bool CheckMethod(BaseEventMethodFlag flag)
        {
            return (MethodFlag & (int)flag) != 0;
        }
    }

    public static void Initialize(LuaEnv lua)
    {
        _getLuaBaseEvent = lua.Global.GetInPath<FuncStrEventTable>("ActionBaseEventMgr.GetLuaBaseEvent");
        //_isSmoothEnable = lua.Global.GetInPath<FuncStrBool>("ActionBaseEventMgr.IsSmoothEnable");
        _copyData = lua.Global.GetInPath<FuncVoidTable>("ActionBaseEventMgr.CopyData");
        //_isLuaEvent = lua.Global.GetInPath<FuncStrBool>("ActionBaseEventMgr.IsLuaEvent");
        _setFieldInfo = lua.Global.GetInPath<FuncVoidStrObj>("ActionBaseEventMgr.SetFieldInfo");
        _onActionStart = lua.Global.GetInPath<FuncActionVoid>("ActionBaseEventMgr.OnActionStart");
        _onActionStop = lua.Global.GetInPath<FuncActionVoid>("ActionBaseEventMgr.OnActionStop");
        _getAssociatedAction = lua.Global.GetInPath<FuncVoidList>("ActionBaseEventMgr.GetAssociatedAction");
        _getAssociatedResources = lua.Global.GetInPath<FuncVoidDic>("ActionBaseEventMgr.GetAssociatedResources");
        //_isDuration = lua.Global.GetInPath<FuncStrBool>("ActionBaseEventMgr.IsDuration");
        //_isCondition = lua.Global.GetInPath<FuncStrBool>("ActionBaseEventMgr.IsCondition");
        _tickProcess = lua.Global.GetInPath<FuncActionTrackVoid>("ActionBaseEventMgr.TickProcess");
        _tickProcessBlend = lua.Global.GetInPath<FuncTickProcessBlend>("ActionBaseEventMgr.TickProcessBlend");
        _tickPostProcess = lua.Global.GetInPath<FuncActionTrackFloatVoid>("ActionBaseEventMgr.TickPostProcess");
        _durationEnter = lua.Global.GetInPath<FuncActionTrackVoid>("ActionBaseEventMgr.DurationEnter");
        _durationLeave = lua.Global.GetInPath<FuncActionTrackVoid>("ActionBaseEventMgr.DurationLeave");
        _durationProcess = lua.Global.GetInPath<FuncActionTrackFloatVoid>("ActionBaseEventMgr.DurationProcess");
        _clearData = lua.Global.GetInPath<FuncVoidBool>("ActionBaseEventMgr.ClearData");

        _allLuaEventTypeMeta.Clear();
    }

    public static void Uninitialize()
    {
        _getLuaBaseEvent = null;
        //_isSmoothEnable = null;
        _copyData = null;
        //_isLuaEvent = null;
        _setFieldInfo = null;
        _onActionStart = null;
        _onActionStop = null;
        _getAssociatedAction = null;
        _getAssociatedResources = null;
        //_isDuration = null;
        //_isCondition = null;
        _tickProcess = null;
        _tickProcessBlend = null;
        _tickPostProcess = null;
        _durationEnter = null;
        _durationLeave = null;
        _durationProcess = null;
        _clearData = null;
        _allLuaEventTypeMeta.Clear();
    }

    public static void AddLuaBaseEventType(string eventType, bool isDuration, bool isCondition, bool isSmoothEnable, int methodFlag)
    {
        if(_allLuaEventTypeMeta.ContainsKey(eventType))
        {
            Log.LogE("AGE", "the dic has the eventType, {0}", eventType);
            return;
        }

        LuaBaseEventMeta meta = new LuaBaseEventMeta();
        meta.IsDuration = isDuration;
        meta.IsCondition = isCondition;
        meta.IsSmoothEnable = isSmoothEnable;
        meta.MethodFlag = methodFlag;
        _allLuaEventTypeMeta.Add(eventType, meta);
    }

    public static void RemoveAllLuaEventType()
    {
        _allLuaEventTypeMeta.Clear();
    }

    public static bool IsLuaEvent(string eventType)
    {
        if(_allLuaEventTypeMeta.ContainsKey(eventType))
        {
            return true;
        }

        return false;
    }

    public static bool IsDuration(string eventType)
    {
        if (!_allLuaEventTypeMeta.ContainsKey(eventType))
        {
            Log.LogE("AGE", "the luaEventDic no have the eventType, {0}", eventType);
            return false;
        }

        return _allLuaEventTypeMeta[eventType].IsDuration;
    }

    public static bool IsCondition(string eventType)
    {
        if (!_allLuaEventTypeMeta.ContainsKey(eventType))
        {
            Log.LogE("AGE", "the luaEventDic no have the eventType, {0}", eventType);
            return false;
        }

        return _allLuaEventTypeMeta[eventType].IsCondition;
    }

    public static bool IsSmoothEnable(string eventType)
    {
        if (!_allLuaEventTypeMeta.ContainsKey(eventType))
        {
            Log.LogE("AGE", "the luaEventDic no have the eventType, {0}", eventType);
            return false;
        }

        return _allLuaEventTypeMeta[eventType].IsSmoothEnable;
    }

    /// <summary>
    /// 获取一个新的luaevent对象
    /// </summary>
    /// <typeparam name="T">BaseEvent</typeparam>
    /// <param name="e"></param>
    /// <param name="eventType">对象类型</param>
    /// <param name="srcTbl">传空返回一个没有参数的对象类，传入一个table的话，会copy这个table的参数，并返回</param>
    /// <returns></returns>
    public static LuaTable GetNewLuaBaseEvent<T>(this T e, string eventType, LuaTable srcTbl = null) where T : BaseEvent
    {
        if (_getLuaBaseEvent != null)
        {
            return _getLuaBaseEvent(eventType, e, srcTbl);
        }

        return null;
    }

    public static void LuaSetFieldInfo<T>(this T e, string fieldName, object value) where T : BaseEvent
    {
        if (_setFieldInfo != null && e != null)
        {
            _setFieldInfo(e.LuaBaseEvent, fieldName, value);
        }
    }

    public static bool LuaClearData<T>(this T e) where T : BaseEvent
    {
        if (_clearData != null)
        {
#if UNITY_EDITOR
            if(e.LuaBaseEvent != null && e.LuaBaseEvent["..classname"] != null)
            {
                if(e.LuaBaseEvent["CSEvent"] == null)
                {
                    Log.LogE("AGE", "the event is error", e.LuaBaseEvent["..classname"]);
                }
            }
#endif

            return _clearData(e.LuaBaseEvent);
        }

        return true;
    }

    public static void LuaCopyData<T>(this T e, BaseEvent src) where T : BaseEvent
    {
        T lbe = src as T;
        if (_copyData != null && lbe != null)
        {
            _copyData(e.LuaBaseEvent, lbe.LuaBaseEvent);
        }
    }

    public static void LuaOnActionStart<T>(this T e, Action _action) where T : BaseEvent
    {
        if(e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if(!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.OnActionStart))
        {
            return;
        }

        if (_onActionStart != null)
        {
            _onActionStart(e.LuaBaseEvent, _action);
        }
    }

    public static void LuaOnActionStop<T>(this T e, Action _action) where T : BaseEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.OnActionStop))
        {
            return;
        }

        if (_onActionStop != null)
        {
            _onActionStop(e.LuaBaseEvent, _action);
        }
    }

    public static List<string> LuaGetAssociatedAction<T>(this T e) where T : BaseEvent
    {
        List<string> result = new List<string>();
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return result;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.GetAssociatedAction))
        {
            return result;
        }

        if (_getAssociatedAction != null)
        {
            _getAssociatedAction(e.LuaBaseEvent, result);
        }

        return result;
    }

    public static Dictionary<string, bool> LuaGetAssociatedResources<T>(this T e) where T : BaseEvent
    {
        Dictionary<string, bool> result = new Dictionary<string, bool>();
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return result;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.GetAssociatedResources))
        {
            return result;
        }

        if (_getAssociatedResources != null)
        {
            _getAssociatedResources(e.LuaBaseEvent, result);
        }
        return result;
    }

    public static void TickProcess<T>(this T e, Action action, Track track) where T : LuaTickEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.TickProcess))
        {
            return;
        }

        if (_tickProcess != null)
        {
            _tickProcess(e.LuaBaseEvent, action, track);
        }
    }

    public static void TickProcessBlend<T>(this T e, Action _action, Track _track, TickEvent _prevEvent, float _blendWeight) where T : LuaTickEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.TickProcessBlend))
        {
            return;
        }

        if (_tickProcessBlend != null)
        {
            _tickProcessBlend(e.LuaBaseEvent, _action, _track, _prevEvent, _blendWeight);
        }
    }

    public static void TickPostProcess<T>(this T e, Action _action, Track _track, float _localTime) where T : LuaTickEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.TickPostProcess))
        {
            return;
        }

        if (_tickPostProcess != null)
        {
            _tickPostProcess(e.LuaBaseEvent, _action, _track, _localTime);
        }
    }

    public static void DurationEnter<T>(this T e, Action _action, Track _track) where T : LuaDurationEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.DurationEnter))
        {
            return;
        }

        if (_durationEnter != null)
        {
            _durationEnter(e.LuaBaseEvent, _action, _track);
        }
    }

    public static void DurationLeave<T>(this T e, Action _action, Track _track) where T : LuaDurationEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.DurationLeave))
        {
            return;
        }

        if (_durationLeave != null)
        {
            _durationLeave(e.LuaBaseEvent, _action, _track);
        }
    }

    public static void DurationProcess<T>(this T e, Action _action, Track _track, float _localTime) where T : LuaDurationEvent
    {
        if (e.track == null || !_allLuaEventTypeMeta.ContainsKey(e.track.EventTypeName))
        {
            return;
        }

        if (!_allLuaEventTypeMeta[e.track.EventTypeName].CheckMethod(BaseEventMethodFlag.DurationProcess))
        {
            return;
        }

        if (_durationProcess != null)
        {
            _durationProcess(e.LuaBaseEvent, _action, _track, _localTime);
        }
    }
}
