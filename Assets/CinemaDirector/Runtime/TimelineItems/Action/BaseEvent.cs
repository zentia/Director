using System;
using System.Collections.Generic;
using System.Reflection;
using CinemaDirector;
using XLua;

namespace AGE
{
    [Serializable]
	public abstract class BaseEvent : TimelineItem, PooledActionClass, IComparable
    {
        public virtual LuaTable LuaBaseEvent
        {
            get;
            protected set;
        }

        public virtual bool GetSmoothEnable() { return true; }
		public virtual bool SupportEditMode() { return false; }
        public abstract bool IsDuration();
        public abstract bool IsCondition();
        
		public virtual Dictionary<string, bool> GetAssociatedResources() 
		{
			Dictionary<string, bool> result = new Dictionary<string, bool>();

            System.Type myType = this.GetType();
            System.Type stringType = typeof(string);
            System.Type stringArrayType = typeof(string[]);
            while (myType == typeof(BaseEvent) || myType.IsSubclassOf(typeof(BaseEvent)))
            {
                FieldInfo[] fieldInfos = myType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    System.Reflection.FieldInfo fieldInfo = fieldInfos[i];
                    if (System.Attribute.IsDefined(fieldInfo, typeof(AssetReference)))
                    {
                        if (fieldInfo.FieldType == stringType)
                        {
                            string strKey = fieldInfo.GetValue(this) as string;
                            if ((strKey != null) && (strKey.Length >0) && (!result.ContainsKey(strKey)))
                            {
                                result.Add(strKey, true);
                            }
                        }
                        else if (fieldInfo.FieldType == stringArrayType)
                        {
                            string[] array = fieldInfo.GetValue(this) as string[];
                            for (int sId = 0; sId < array.Length; sId++)
                            {
                                string strKey = array[sId];
                                if ((strKey != null) && (strKey.Length > 0) && (!result.ContainsKey(strKey)))
                                {
                                    result.Add(strKey, true);
                                }
                            }
                        }
                    }
                }
                myType = myType.BaseType;
            }


			return result;
		}

		public virtual List<string> GetAssociatedAction()
		{
			List<string> ret = new List<string>();

			System.Type type = this.GetType();
			while (type == typeof(BaseEvent) || type.IsSubclassOf(typeof(BaseEvent)))
			{
				FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    System.Reflection.FieldInfo fieldInfo = fieldInfos[i];
					if (fieldInfo.FieldType == typeof(string) && System.Attribute.IsDefined(fieldInfo, typeof(ActionReference)))
					{
						string path = fieldInfo.GetValue(this) as string;
						if(path.Length > 0 && !ret.Contains(path))
							ret.Add(path);
					}
				}
				type = type.BaseType;
			}
			
			return ret;
		}

        public List<string> GetAssociatedAudio()
        {
            List<string> ret = new List<string>();

            System.Type type = this.GetType();
            System.Type stringType = typeof(string);
            System.Type stringArrayType = typeof(string[]);
            while (type == typeof(BaseEvent) || type.IsSubclassOf(typeof(BaseEvent)))
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                for (int i = 0; i < fieldInfos.Length; i++)
                {
                    System.Reflection.FieldInfo fieldInfo = fieldInfos[i];
                    if (System.Attribute.IsDefined(fieldInfo, typeof(AudioReference)))
                    {
                        if (fieldInfo.FieldType == stringType)
                        {
                            string path = fieldInfo.GetValue(this) as string;
                            if (path.Length > 0 && !ret.Contains(path))
                                ret.Add(path);
                        }
                        else if (fieldInfo.FieldType == stringArrayType)
                        {
                            string[] array = fieldInfo.GetValue(this) as string[];
                            for (int sId = 0; sId < array.Length; sId++)
                            {
                                string path = array[sId];
                                if (path.Length > 0 && !ret.Contains(path))
                                {
                                    ret.Add(path);
                                }
                            }
                        }
                    }
                }
                type = type.BaseType;
            }

            return ret;
        }

        protected abstract void CopyData(BaseEvent src);
        protected abstract void ClearData();
        protected virtual uint GetPoolInitCount()
        {
            return 2;
        }

        public void OnUse(PooledActionClass clonedData)
        {
            BaseEvent src = clonedData as BaseEvent;
            time = src.time;
            waitForConditions = src.waitForConditions;//todo不晓得干啥子的
            CopyData(src);
        }

        public  void OnRelease()
        {
            time = 0;
            waitForConditions.Clear();
            ClearData();
        }

        public uint GetMaxInitCount()
        {
            return GetPoolInitCount();
        }
        public virtual void OnActionStart(Action _action)
        {

        }

        public virtual void OnActionStop(Action _action)
        {

        }

        public bool CheckConditions(Action _action)
		{

            if (track != null)
            {
                waitForConditions = track.waitForConditions;
            }
            Dictionary<int, bool>.Enumerator conIter = waitForConditions.GetEnumerator();
            while(conIter.MoveNext())
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

		public Dictionary<int, bool> waitForConditions = new Dictionary<int, bool>();
        [NonSerialized]
		public Track track = null;

        public int CompareTo(object other)
        {
            var otherEvent = (BaseEvent) other;
            if (time > otherEvent.time)
            {
                return 1;
            }
            else if (time < otherEvent.time)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public override string ToString()
        {
            return time.ToString();
        }
    }
}
