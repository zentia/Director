using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Assets.Plugins.Common;
using CinemaDirector;
using Sirenix.OdinInspector;

namespace AGE
{
    [Serializable]
    public class Track: DirectorObject, PooledActionClass
    {
        public Track()
        {

        }
        public Track(Action _action, string _eventTypeName)
        {
            InitTrackExtraParam(_action, _eventTypeName);
        }

        public void InitTrackExtraParam(Action _action, string _eventTypeName)
        {
            eventTypeName = _eventTypeName;
            eventType = Type.GetType(_eventTypeName);
            curTime = 0;
            lastExecPos = 0;
        }
        public void OnUse(PooledActionClass clonedData)
        {
            OnRelease();
            Track src = clonedData as Track;
            InitTrackExtraParam(src.action, src.eventTypeName);
            for (int i = 0; i < src.trackEvents.Count; i++)
            {
                BaseEvent trackEvent = src.trackEvents[i] as BaseEvent;
                BaseEvent ce = ActionClassPoolManager.Instance.GetActionClassPool(trackEvent.GetType()).GetActionObject(trackEvent) as BaseEvent;
                ce.track = this;
                trackEvents.Add(ce);
            }
       
            var enu = src.waitForConditions.GetEnumerator();
            while(enu.MoveNext())
            {
                waitForConditions.Add(enu.Current.Key, enu.Current.Value);
            }
            enabled = src.enabled;
            lod = src.lod;
            execOnActionCompleted = src.execOnActionCompleted;
            execOnForceStopped = src.execOnForceStopped;
            stopAfterLastEvent = src.stopAfterLastEvent;
        }

        public  void OnRelease()
        {
            eventType = null;
            eventTypeName = null;
            isDurationEvent = false;
            isCondition = false;
            for (int i = 0; i < trackEvents.Count; ++i)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                if (trackEvent != null)
                {
                    ActionClassPoolManager.Instance.GetActionClassPool(trackEvent.GetType()).ReleaseActionObject(trackEvent);
                }

            }
            trackEvents.Clear();
            started = false;
            enabled = false;

            supportEditMode = false;

            activeEvents.Clear();

            waitForConditions.Clear();
            curTime = 0.0f;

            trackIndex = -1;

            trackName = "";
            execOnActionCompleted = false;
            execOnForceStopped = false;

            enableSmooth = true; //开启表现平滑处理，可能会跳帧
            lastExecPos = 0; //上次执行的event位置

            lod = 0;
            stopAfterLastEvent = true; //only affect condition-depended tracks
        }

        public  uint GetMaxInitCount()
        {
            return 10;
        }

        public BaseEvent AddEvent(float _time, float _length)
        {
            if (eventType == null)
            {
                return null;
            }
            var newEvent = (BaseEvent)CreateInstance(eventType);

            newEvent.time = _time;

            if (isDurationEvent)
                (newEvent as DurationEvent).length = _length;

            float insertPosFloat;
            if (LocateInsertPos(_time, out insertPosFloat))
            {
                int insertPos = (int)(insertPosFloat + 1);
                if (insertPos >= trackEvents.Count)
                {
                    trackEvents.Add(newEvent);                    
                }
                else
                {
                    trackEvents.Insert(insertPos, newEvent);    
                }
            }

            newEvent.track = this;
            return newEvent;
        }

        public bool LocateEvent(float _curTime, out float _result)
        {
            _result = 0.0f;
            
            if (trackEvents == null)
            {
                return false;
            }

            int eventCount = trackEvents.Count;
            if (eventCount == 0) return false;

            if (Loop)
            {
                while (_curTime < 0) _curTime += Length;
                while (_curTime >= Length) _curTime -= Length;
            }
            else
            {
                if (_curTime < 0) _curTime = 0;
                else if (_curTime > Length) _curTime = Length;
            }

            int eventIndex = -1;

            int begin = 0;
            int end = trackEvents.Count - 1;

            while (begin != end)
            {
                int mid = (begin + end) / 2 + 1;
                if (_curTime < (trackEvents[mid] as BaseEvent).time)
                {
                    //search left branch
                    end = mid - 1;
                }
                else
                {
                    //search right branch
                    begin = mid;
                }
            }

            if (begin == 0 && _curTime < (trackEvents[0] as BaseEvent).time)
                eventIndex = -1;
            else
                eventIndex = begin;

            if (eventIndex < 0) //before the first event
            {
                if (Loop)
                {
                    float beginSpace = (trackEvents[0] as BaseEvent).time;
                    float endSpace = Length - (trackEvents[eventCount - 1] as BaseEvent).time;
                    _result = (eventCount - 1) + (_curTime + endSpace) / (beginSpace + endSpace);
                }
                else
                {
                    _result = -1 + _curTime / (trackEvents[0] as BaseEvent).time;
                }
            }
            else if (eventIndex == eventCount - 1) //the last event
            {
                if (Loop)
                {
                    float beginSpace = (trackEvents[0] as BaseEvent).time;
                    float endSpace = Length - (trackEvents[eventCount - 1] as BaseEvent).time;
                    _result = (eventCount - 1) + (_curTime - (trackEvents[eventCount - 1] as BaseEvent).time) / (beginSpace + endSpace);
                }
                else
                {
                    _result = (eventCount - 1) + (_curTime - (trackEvents[eventCount - 1] as BaseEvent).time) / (Length - (trackEvents[eventCount - 1] as BaseEvent).time);
                }
            }
            else
            {
                _result = eventIndex + (_curTime - (trackEvents[eventIndex] as BaseEvent).time) / ((trackEvents[eventIndex + 1] as BaseEvent).time - (trackEvents[eventIndex] as BaseEvent).time);
            }
            return true;
        }
        
        bool LocateInsertPos(float _curTime, out float _result)
        {
            _result = 0.0f;

            int eventCount = trackEvents.Count;
            if (eventCount == 0) 
                return true;

            if (_curTime < 0) 
                _curTime = 0;
            else if (_curTime > Length)
                return false;

            int eventIndex = -1;

            int begin = 0;
            int end = trackEvents.Count - 1;

            while (begin != end)
            {
                int mid = (begin + end) / 2 + 1;
                if (_curTime < trackEvents[mid].time)
                {
                    //search left branch
                    end = mid - 1;
                }
                else
                {
                    //search right branch
                    begin = mid;
                }
            }

            if (begin == 0 && _curTime < trackEvents[0].time)
                eventIndex = -1;
            else
                eventIndex = begin;

            if (eventIndex < 0) //before the first event
            {
                _result = -1 + _curTime / trackEvents[0].time;
            }
            else if (eventIndex == eventCount - 1) //the last event
            {
                var t = trackEvents[eventCount - 1].time;
                if (Mathf.Approximately(Length, t))
                {
                    _result = eventCount - 1;
                }
                else
                {
                    _result = (eventCount - 1) + (_curTime - t) / (Length - t);    
                }
            }
            else
            {
                _result = eventIndex + (_curTime - trackEvents[eventIndex].time) / (trackEvents[eventIndex + 1].time - trackEvents[eventIndex].time);
            }
            return true;
        }

        public void Process(float _curTime)
        {
            curTime = _curTime;

            float eventPosFloat = 0;
            if (!LocateEvent(_curTime, out eventPosFloat) || eventPosFloat < 0)
                return;

            int eventPos = (int)eventPosFloat;
            if (_curTime >= Length && !Loop)
                eventPos = trackEvents.Count - 1;

            if (eventPos < 0 || eventPos >= trackEvents.Count)
            {
                //if (AgeLogger.UserExceptionCount < AgeLogger.MAX_LOG_EXP_COUNT && this.action != null)
                {
                    string fmtMsg = string.Format("Exception: EventPos is out of Range:{0},track event count:{1}, action:{2},trackIndx:{3},curTime:{4},loop:{5}"
                        , eventPos, trackEvents.Count, action.actionName, trackIndex, curTime, Loop);
                    Log.LogF("AGE", fmtMsg);
                }
            }

            if (Loop)
            {
                int lastEventPos = (eventPos - 1 + trackEvents.Count) % trackEvents.Count;
                int nextEventPos = (eventPos + 1 + trackEvents.Count) % trackEvents.Count;

                if (lastEventPos < 0 || lastEventPos >= trackEvents.Count)
                {
                    //if (AgeLogger.UserExceptionCount < AgeLogger.MAX_LOG_EXP_COUNT && this.action != null)
                    {
                        string fmtMsg = string.Format("Exception: lastEventPos is out of Range:{0},track event count:{1}, action:{2},trackIndx:{3},curTime:{4},loop:{5}"
                            , lastEventPos, trackEvents.Count, action.actionName, trackIndex, curTime, Loop);
                        Log.LogF("AGE", fmtMsg);
                    }

                    if (trackEvents.Count > 0)
                    {
                        lastEventPos = Mathf.Clamp(lastEventPos, 0, trackEvents.Count - 1);
                    }
                    else
                    {
                        lastEventPos = 0;
                    }
                }

                if (nextEventPos < 0 || nextEventPos >= trackEvents.Count)
                {
                    //if (AgeLogger.UserExceptionCount < AgeLogger.MAX_LOG_EXP_COUNT && this.action != null)
                    {
                        string fmtMsg = string.Format("Exception: nextEventPos is out of Range:{0},track event count:{1}, action:{2},trackIndx:{3},curTime:{4},loop:{5}"
                            , nextEventPos, trackEvents.Count, action.actionName, trackIndex, curTime, Loop);
                        Log.LogF("AGE", fmtMsg);
                    }
                    if (trackEvents.Count > 0)
                    {
                        nextEventPos = Mathf.Clamp(nextEventPos, 0, trackEvents.Count - 1);
                    }
                    else
                    {
                        nextEventPos = 0;
                    }
                }

                if (isDurationEvent)
                {
					for (int i = lastEventPos, j = 0; j < trackEvents.Count && i < trackEvents.Count; i = (i + 1) % trackEvents.Count, j++)
                    {
                        DurationEvent durEvent = trackEvents[i] as DurationEvent;
                        if (durEvent == null)
                        {
                            continue;
                        }
                        if (CheckSkip(_curTime, durEvent.End) && activeEvents.Contains(durEvent))
                        {
                            durEvent.Leave(action, this);
                            activeEvents.Remove(durEvent);
                        }

                        if (CheckSkip(_curTime, durEvent.Start) && durEvent.CheckConditions(action))
                        {
                            if (activeEvents.Count == 0)
                            {
                                //not blending
                                durEvent.Enter(action, this);
                            }
                            else
                            {
                                //blending
                                DurationEvent preEvent = activeEvents[0] as DurationEvent;
                                if (preEvent.Start < durEvent.Start && preEvent.End < Length)
                                {
                                    //not looping
                                    float blendTime = preEvent.End - durEvent.Start;
                                    durEvent.EnterBlend(action, this, preEvent, blendTime);
                                }
                                else if (preEvent.Start < durEvent.Start && preEvent.End >= Length)
                                {
                                    //looping
                                    float blendTime = preEvent.End - durEvent.Start;
                                    durEvent.EnterBlend(action, this, preEvent, blendTime);
                                }
                                else
                                {
                                    //looped
                                    float blendTime = preEvent.End - Length - durEvent.Start;
                                    durEvent.EnterBlend(action, this, preEvent, blendTime);
                                }
                            }
                            activeEvents.Add(durEvent);
                        }
                    }
                }
                else
                {
					for (int i = lastEventPos, j = 0; j < trackEvents.Count && i < trackEvents.Count; i = (i + 1) % trackEvents.Count, j++)
                    {
                        TickEvent ticEvent = trackEvents[i] as TickEvent;
                        if (ticEvent == null)
                        {
                            continue;
                        }
                        if (CheckSkip(_curTime, ticEvent.time) && !ticEvent.IsNeedWait(action) && ticEvent.CheckConditions(action))
                        {
                            ticEvent.Process(action, this);
                        }
                    }

                    //process tick event blending (key frames)
                    if (eventPos != nextEventPos)
                    {
                        TickEvent ticEvent = eventPos >= 0 && eventPos < trackEvents.Count ? trackEvents[eventPos] as TickEvent : null;
                        TickEvent nexEvent = nextEventPos >= 0 && nextEventPos < trackEvents.Count ? trackEvents[nextEventPos] as TickEvent : null;
                        if (ticEvent != null && nexEvent != null)
                        {
                            if (nexEvent.time > ticEvent.time)
                            {
                                //not looped
                                float blendWeight = (_curTime - ticEvent.time) / (nexEvent.time - ticEvent.time);
                                nexEvent.ProcessBlend(action, this, ticEvent, blendWeight);
                            }
                            else if (nexEvent.time < ticEvent.time)
                            {
                                if (_curTime >= ticEvent.time)
                                {
                                    //pre loop
                                    float blendWeight = (_curTime - ticEvent.time) / (nexEvent.time + Length - ticEvent.time);
                                    nexEvent.ProcessBlend(action, this, ticEvent, blendWeight);
                                }
                                else
                                {
                                    //looped
                                    float blendWeight = (_curTime + Length - ticEvent.time) / (nexEvent.time + Length - ticEvent.time);
                                    nexEvent.ProcessBlend(action, this, ticEvent, blendWeight);
                                }
                            }
                        }
                    }
                    else
                    {
                        TickEvent ticEvent = eventPos >= 0 && eventPos < trackEvents.Count ? trackEvents[eventPos] as TickEvent : null;
                        if (ticEvent != null)
                        {
                            if (_curTime > ticEvent.time)
                            {
                                //not looped
                                float localTime = _curTime - ticEvent.time;
                                ticEvent.PostProcess(action, this, localTime);
                            }
                            else if (_curTime < ticEvent.time)
                            {
                                if (_curTime >= ticEvent.time)
                                {
                                    //pre loop
                                    float localTime = _curTime - ticEvent.time;
                                    ticEvent.PostProcess(action, this, localTime);
                                }
                                else
                                {
                                    //looped
                                    float localTime = _curTime + Length - ticEvent.time;
                                    ticEvent.PostProcess(action, this, localTime);
                                }
                            }
                        }

                    }
                }
            }
            else
            {
                int lastEventPos = enableSmooth ? eventPos - 1 : lastExecPos;
                if (lastEventPos < 0) lastEventPos = 0;
                int nextEventPos = eventPos + 1;
                if (nextEventPos >= trackEvents.Count) nextEventPos = eventPos;

                if (lastEventPos < 0 || lastEventPos >= trackEvents.Count)
                {
                    //if (AgeLogger.UserExceptionCount < AgeLogger.MAX_LOG_EXP_COUNT && this.action != null)
                    {
                        string fmtMsg = string.Format("Exception: lastEventPos is out of Range:{0},track event count:{1}, action:{2},trackIndx:{3},curTime:{4},loop:{5}"
                            , lastEventPos, trackEvents.Count, action.actionName, trackIndex, curTime, Loop);
                        Log.LogF("AGE", fmtMsg);
                    }

                    if (trackEvents.Count > 0)
                    {
                        lastEventPos = Mathf.Clamp(lastEventPos, 0, trackEvents.Count - 1);
                    }
                    else
                    {
                        lastEventPos = 0;
                    }
                }

                if (nextEventPos < 0 || nextEventPos >= trackEvents.Count)
                {
                    Log.LogF("AGE", "Exception: nextEventPos is out of Range:{0},track event count:{1}, action:{2},trackIndx:{3},curTime:{4},loop:{5}"
                        , nextEventPos, trackEvents.Count, action.actionName, trackIndex, curTime, Loop);

                    if (trackEvents.Count > 0)
                    {
                        nextEventPos = Mathf.Clamp(nextEventPos, 0, trackEvents.Count - 1);
                    }
                    else
                    {
                        nextEventPos = 0;
                    }
                }

                if (isDurationEvent)
                {
					if(lastExecPos >= 0 && eventPos != lastExecPos && lastExecPos < lastEventPos && lastExecPos < trackEvents.Count) //???????durationEvent,??leave?????durationEvent
					{
						DurationEvent durEvent = trackEvents[lastExecPos] as DurationEvent;
						if (CheckSkip(_curTime, durEvent.End) && activeEvents.Contains(durEvent))
						{
							if (activeEvents.Count > 1)
							{
								//do leave blending
								DurationEvent nextEvent = activeEvents[1] as DurationEvent;
								if (nextEvent != null)
								{
									float blendTime = durEvent.End - nextEvent.Start;
									durEvent.LeaveBlend(action, this, nextEvent, blendTime);
								}
							}
							else
							{
								//leave
								durEvent.Leave(action, this);
							}
							activeEvents.Remove(durEvent);
						}
					}

					for (int i = lastEventPos; i < trackEvents.Count; i++)
                    {
                        DurationEvent durEvent = trackEvents[i] as DurationEvent;
                        if (durEvent == null)
                        {
                            continue;
                        }
                        if (CheckSkip(_curTime, durEvent.Start) && durEvent.CheckConditions(action))
                        {
                            if (activeEvents.Count == 0)
                            {
                                //not blending
                                durEvent.Enter(action, this);
                            }
                            else
                            {
                                //blending
                                DurationEvent preEvent = activeEvents[0] as DurationEvent;
                                if (preEvent == null)
                                {
                                    continue;
                                }
                                float blendTime = preEvent.End - durEvent.Start;
                                durEvent.EnterBlend(action, this, preEvent, blendTime);
                            }
                            activeEvents.Add(durEvent);
                        }
                        if (CheckSkip(_curTime, durEvent.End) && activeEvents.Contains(durEvent))
                        {
                            if (activeEvents.Count > 1)
                            {
                                //do leave blending
                                DurationEvent nextEvent = activeEvents[1] as DurationEvent;
                                if (nextEvent == null)
                                {
                                    continue;
                                }
                                float blendTime = durEvent.End - nextEvent.Start;
                                durEvent.LeaveBlend(action, this, nextEvent, blendTime);
                            }
                            else
                            {
                                //leave
                                durEvent.Leave(action, this);
                            }
                            activeEvents.Remove(durEvent);
                        }
                    }
                }
                else
                {
                    for (int i = lastEventPos; i < trackEvents.Count; i++)
                    {
                        TickEvent ticEvent = trackEvents[i] as TickEvent;
                        if (ticEvent == null)
                        {
                            continue;
                        }
                        if (CheckSkip(_curTime, ticEvent.time) && !ticEvent.IsNeedWait(action) && ticEvent.CheckConditions(action))
                        {
                            ticEvent.Process(action, this);
                        }
                    }

                    //process tick event blending (key frames)
                    if (eventPos != nextEventPos)
                    {
                        TickEvent ticEvent = eventPos >= 0 && eventPos < trackEvents.Count ? trackEvents[eventPos] as TickEvent : null;
                        TickEvent nexEvent = nextEventPos >= 0 && nextEventPos < trackEvents.Count ? trackEvents[nextEventPos] as TickEvent : null;
                        if (ticEvent != null && nexEvent != null)
                        {
                            float blendWeight = (_curTime - ticEvent.time) / (nexEvent.time - ticEvent.time);
                            nexEvent.ProcessBlend(action, this, ticEvent, blendWeight);
                        }
                    }
                    else
                    {
                        TickEvent ticEvent = eventPos >= 0 && eventPos < trackEvents.Count ? trackEvents[eventPos] as TickEvent : null;
                        if (ticEvent != null)
                        {
                            float localTime = _curTime - ticEvent.time;
                            ticEvent.PostProcess(action, this, localTime);
                        }
                    }
                }

                lastExecPos = eventPos;
            }

            //process duration events
            if (activeEvents.Count == 1)
            {
                //not blending
                DurationEvent durEvent = activeEvents[0] as DurationEvent;
                if (durEvent != null)
                {
                    if (_curTime >= durEvent.Start)
                        durEvent.Process(action, this, _curTime - durEvent.Start);
                    else
                        durEvent.Process(action, this, _curTime + Length - durEvent.Start);
                }

            }
            else if (activeEvents.Count == 2)
            {
                //blending
                DurationEvent preEvent = activeEvents[0] as DurationEvent;
                DurationEvent durEvent = activeEvents[1] as DurationEvent;
                if (preEvent != null && durEvent != null)
                {
                    if (preEvent.Start < durEvent.Start && preEvent.End < Length)
                    {
                        //not looping
                        float localTime = _curTime - durEvent.Start;
                        float prevLocalTime = _curTime - preEvent.Start;
                        float blendWeight = (_curTime - durEvent.Start) / (preEvent.End - durEvent.Start);
                        durEvent.ProcessBlend(action, this, localTime, preEvent, prevLocalTime, blendWeight);
                    }
                    else if (preEvent.Start < durEvent.Start && preEvent.End >= Length)
                    {
                        //looping
                        if (_curTime >= durEvent.Start)
                        {
                            float localTime = _curTime - durEvent.Start;
                            float prevLocalTime = _curTime - preEvent.Start;
                            float blendWeight = (_curTime - durEvent.Start) / (preEvent.End - durEvent.Start);
                            durEvent.ProcessBlend(action, this, localTime, preEvent, prevLocalTime, blendWeight);
                        }
                        else
                        {
                            float localTime = _curTime + Length - durEvent.Start;
                            float prevLocalTime = _curTime + Length - preEvent.Start;
                            float blendWeight = (_curTime + Length - durEvent.Start) / (preEvent.End - durEvent.Start);
                            durEvent.ProcessBlend(action, this, localTime, preEvent, prevLocalTime, blendWeight);
                        }
                    }
                    else
                    {
                        //looped
                        float localTime = _curTime - durEvent.Start;
                        float prevLocalTime = _curTime + Length - preEvent.Start;
                        float blendWeight = (_curTime - durEvent.Start) / (preEvent.End - Length - durEvent.Start);
                        durEvent.ProcessBlend(action, this, localTime, preEvent, prevLocalTime, blendWeight);
                    }
                }

            }
        }

        protected bool CheckSkip(float _curTime, float _checkTime)
        {
            if (!Loop)
            {
                if (_checkTime < _curTime && _checkTime >= _curTime - (action == null ? 0 : action.deltaTime)) return true;
                else return false;
            }
            else
            {
                float preTime = _curTime - (action == null ? 0 : action.deltaTime);

                if (_checkTime < 0.0f) _checkTime += Length;
                else if (_checkTime >= Length) _checkTime -= Length;

                //note that curTime is always < Length
                if (preTime >= 0.0f)
                {
                    if (_checkTime < _curTime && _checkTime >= preTime) return true;
                    else return false;
                }
                else// if (preTime < 0.0f)
                {
                    if ((_checkTime < _curTime && _checkTime >= 0) || (_checkTime <= Length && _checkTime >= preTime + Length)) return true;
                    else return false;
                }
            }
        }

        public BaseEvent GetOffsetEvent(BaseEvent _curEvent, int _offset)
        {
            int curIndex = trackEvents.LastIndexOf(_curEvent);
            if (Loop)
            {
                int resultIndex = (curIndex + _offset) % trackEvents.Count;
                if (resultIndex < 0) resultIndex += trackEvents.Count;
                return trackEvents[resultIndex] as BaseEvent;
            }
            else
            {
                int resultIndex = curIndex + _offset;
                if (resultIndex < 0 || resultIndex >= trackEvents.Count) return null;
                else return trackEvents[resultIndex] as BaseEvent;
            }
        }

        public BaseEvent GetEvent(int index)
        {
            if (index >= 0 && index < trackEvents.Count)
                return trackEvents[index] as BaseEvent;
            return null;
        }

        public int GetIndexOfEvent(BaseEvent _curEvent)
        {
            int curIndex = trackEvents.LastIndexOf(_curEvent);
            return curIndex;
        }

        public int GetEventsCount()
        {
            return trackEvents.Count;
        }

        public void DoLoop()
        {
        }

        private void OnActionStart(Action _action)
        {
            for (int i = 0; i < trackEvents.Count; i++)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                if(trackEvent != null)
                {
                    trackEvent.OnActionStart(_action);
                }
            }
        }

        private void OnActionStop(Action _action)
        {
            for (int i = 0; i < trackEvents.Count; i++)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                if (trackEvent != null)
                {
                    trackEvent.OnActionStop(_action);
                }
            }
        }

        public void Start(Action _action)
        {
            if (!enabled)
                return;

            if (!isCondition)
                _action.SetCondition(this, true);

            curTime = 0.0f;
            lastExecPos = 0;

            started = true;

            OnActionStart(_action);
        }

        public void Stop(Action _action)
        {
        
            if (started)
            {
                for (int i = 0; i < activeEvents.Count; ++i)
                {
                    DurationEvent activeEvent = activeEvents[i] as DurationEvent;
                    if (activeEvent != null)
                        activeEvent.Leave(action, this);
                }

                activeEvents.Clear();

                if (!isCondition)
                    _action.SetCondition(this, false);

                started = false;
            }
           

            OnActionStop(_action);

            for (int i = 0; i < trackEvents.Count; ++i)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                if (trackEvent != null)
                {
                    ActionClassPoolManager.Instance.GetActionClassPool(trackEvent.GetType()).ReleaseActionObject(trackEvent);
                }
            }
            trackEvents.Clear();
        }

        public float Length
        {
            get { return action == null ? 0 : action.length; }
        }

        public bool Loop
        {
            get { return action == null ? false : action.loop; }
        }

        public bool IsDurationEvent
        {
            get { return isDurationEvent; }
        }

        public System.Type EventType
        {
            get { return eventType; }
        }

        public string EventTypeName
        {
            get { return eventTypeName; }
        }

        public bool SupportEditMode()
        {
            return supportEditMode;
        }

        System.Type eventType = null;
        string eventTypeName = null;
        bool isDurationEvent = false;
        bool isCondition = false;
        [ReadOnly]
        public List<TimelineItem> trackEvents = new List<TimelineItem>();
        public Action action => (this as TimelineTrack).Cutscene;
        [ReadOnly]
        public bool started = false;
        [LabelText("启用")]
        public bool enabled = false;

        bool supportEditMode = false;

        ArrayList activeEvents = new ArrayList(); //duration events only!

        public bool IsBlending()
        {
            return (activeEvents != null && activeEvents.Count > 1);
        }

        public bool CheckConditions(Action _action)
        {
            Dictionary<int, bool>.Enumerator conIter = waitForConditions.GetEnumerator();
            while (conIter.MoveNext())
            {
                int conditionId = conIter.Current.Key;
                if (conditionId >= 0 && conditionId < _action.GetConditionCount())
                {
                    if (_action.GetCondition(_action.tracks[conditionId] as Track) != waitForConditions[conditionId])
                        return false;
                }
            }
            return true;
        }

        public bool HasConditionCount(Action _action)
        {
            Dictionary<int, bool>.Enumerator conIter = waitForConditions.GetEnumerator();
            while (conIter.MoveNext())
            {
                int conditionId = conIter.Current.Key;
                if (conditionId >= 0 && conditionId < _action.GetConditionCount())
                {
                    if (_action.GetCondition(_action.tracks[conditionId] as Track) == waitForConditions[conditionId])
                        return true;
                }
            }
            return true;
        }
        //returns a time to ensure all events can be finished
        public float GetEventEndTime()
        {
            if (trackEvents.Count == 0) return 0;
            if (isDurationEvent)
                return (trackEvents[trackEvents.Count - 1] as DurationEvent).End + 0.0333f;
            else
                return (trackEvents[trackEvents.Count - 1] as TickEvent).time + 0.0333f;
        }

        public Dictionary<string, bool> GetAssociatedResources()
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>();
            for (int i = 0; i < trackEvents.Count; i++)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                Dictionary<string, bool> eventResources = trackEvent.GetAssociatedResources();
                if (eventResources != null)
                {
                    Dictionary<string, bool>.Enumerator resIter = eventResources.GetEnumerator();
                    while (resIter.MoveNext())
                    {
                        string resName = resIter.Current.Key;
                        if (result.ContainsKey(resName))
                            result[resName] |= eventResources[resName];
                        else
                            result.Add(resName, eventResources[resName]);
                    }
                }
            }
            return result;
        }

        public List<string> GetAssociatedAction()
        {
            List<string> result = new List<string>();
            for (int i = 0; i < trackEvents.Count; i++)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                List<string> eventActions = trackEvent.GetAssociatedAction();
                if (eventActions != null)
                {
                    for (int sid = 0; sid < eventActions.Count; sid++)
                    {
                        string acName = eventActions[sid];
                        if (!result.Contains(acName))
                            result.Add(acName);
                    }
                }
            }
            return result;
        }

        public List<string> GetAssociatedAudio()
        {
            List<string> result = new List<string>();
            for (int i = 0; i < trackEvents.Count; i++)
            {
                BaseEvent trackEvent = trackEvents[i] as BaseEvent;
                List<string> eventAudio = trackEvent.GetAssociatedAudio();
                if (eventAudio != null)
                {
                    for (int sid = 0; sid < eventAudio.Count; sid++)
                    {
                        string audioName = eventAudio[sid];
                        if (!result.Contains(audioName))
                            result.Add(audioName);
                    }
                }
            }
            return result;
        }

        public Dictionary<int, bool> waitForConditions = new Dictionary<int, bool>();
        [ReadOnly]
        public float curTime;
        [ReadOnly]
        public int trackIndex = -1;

        public Color color = Color.red;

        public string trackName = "";
        [LabelText("Action结束时执行")]
        public bool execOnActionCompleted = false;
        [LabelText("Action结束时执行")]
        public bool execOnForceStopped = false;

        protected bool enableSmooth = true; //开启表现平滑处理，可能会跳帧
        public int lastExecPos = 0; //上次执行的event位置

        public int lod = 0;
        [LabelText("事件执行完后停止")]
        public bool stopAfterLastEvent = true; //only affect condition-depended tracks
    }
}
