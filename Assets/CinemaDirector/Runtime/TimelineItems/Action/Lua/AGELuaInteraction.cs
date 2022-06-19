namespace AGE
{
    public class AGELuaInteraction
    {
        public static int GetEventCountByBaseEvent(BaseEvent eve)
        {
            if(eve == null)
            {
                return 0;
            }

            return eve.track.trackEvents.Count;
        }

        public static float GetDurationEventLength(DurationEvent eve)
        {
            if (eve == null)
            {
                return 0;
            }

            return eve.length;
        }

        public static void AddLuaBaseEventType(string eventType, bool isDuration, bool isCondition, bool isSmoothEnable, int methodFlag)
        {
            LuaBaseEventMgr.AddLuaBaseEventType(eventType, isDuration, isCondition, isSmoothEnable, methodFlag);
        }

        public static void RemoveAllLuaEventType()
        {
            LuaBaseEventMgr.RemoveAllLuaEventType();
        }
    }
}
