/********************************************************************
	created:	2016/04/06
	created:	6:4:2016   9:57
	filename: 	H:\SSK\Branch_BattleDll_201603181422\UnityProj\Assets\Scripts\Logic\AGE\Action\Attributes\AttributesReference.cs
	file path:	H:\SSK\Branch_BattleDll_201603181422\UnityProj\Assets\Scripts\Logic\AGE\Action\Attributes
	file base:	AttributesReference
	file ext:	cs
	author:		jeffxie
	
	purpose:	AGE使用的拓展属性
*********************************************************************/
using UnityEngine;
using System.Collections;
using System;
using System.Reflection;

namespace AGE
{
    public sealed class ActionReference : Attribute
    {
    }

    public sealed class AssetReference : Attribute
    {
    }

    public sealed class AudioReference : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class EventCategory : Attribute
    {
        public string category;
        public EventCategory(string _category)
        {
            category = _category;
        }
    }

    public sealed class SubObject : Attribute
    {
        public static GameObject FindSubObject(Transform _targetObjectTrans, string _subObjectNamePath)
        {
            if (_subObjectNamePath.IndexOf('/') >= 0)
            {
                //treat as path
                Transform resultTransform = _targetObjectTrans.Find(_subObjectNamePath);
                if (resultTransform)
                    return resultTransform.gameObject;
                else
                    return null;
            }
            else
            {
                //treat as object name, search recursively
                Transform resultTransform = _targetObjectTrans.Find(_subObjectNamePath);
                if (resultTransform == null)
                {
                    for (int i = 0; i < _targetObjectTrans.childCount; i++)
                    {
                        if (_targetObjectTrans.GetChild(i).gameObject.activeSelf)
                        {
                            GameObject result = FindSubObject(_targetObjectTrans.GetChild(i), _subObjectNamePath);
                            if (result != null)
                                return result;
                        }
                    }
                    return null;
                }
                else
                {
                    return resultTransform.gameObject;
                }
            }
        }

        public static GameObject FindSubObject(GameObject _targetObject, string _subObjectNamePath)
        {
            Transform targetTransform = _targetObject.transform;
            if (_subObjectNamePath.IndexOf('/') >= 0)
            {
                //treat as path
                Transform resultTransform = targetTransform.Find(_subObjectNamePath);
                if (resultTransform)
                    return resultTransform.gameObject;
                else
                    return null;
            }
            else
            {
                //treat as object name, search recursively
                Transform resultTransform = targetTransform.Find(_subObjectNamePath);
                if (resultTransform == null)
                {
                    for (int i = 0; i < targetTransform.childCount; i++)
                    {
                        if (targetTransform.GetChild(i).gameObject.activeSelf)
                        {
                            GameObject result = FindSubObject(targetTransform.GetChild(i).gameObject, _subObjectNamePath);
                            if (result != null)
                                return result;
                        }
                    }
                    return null;
                }
                else
                {
                    return resultTransform.gameObject;
                }
            }
        }
    }

    /// <summary>
    /// 处理Event的反射并存储
    /// 提前处理，避免每次处理带来的消耗
    /// </summary>
    public class BaseEventReflection
    {
        private static DictionaryView<Type, BaseEvent> EventTypeDic = new DictionaryView<Type, BaseEvent>();
        public static DictionaryView<Type, ListView<FieldInfo>> PropertyDic = new DictionaryView<Type, ListView<FieldInfo>>();

        /// <summary>
        /// 处理Event的反射并存储
        /// </summary>
        public static void InitEventTypeDic()
        {
            EventTypeDic.Clear();

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0, imax = types.Length; i < imax; i++)
            {
                if (types[i].FullName.Contains("AGE"))
                {
                    if (IsSubClassOf(types[i], typeof(BaseEvent)) && !EventTypeDic.ContainsKey(types[i]))
                    {
                        EventTypeDic.Add(types[i], types[i].IsAbstract ? null : (BaseEvent)Activator.CreateInstance(types[i]));
                        InitPropertyTypeDic(types[i]);
                    }
                }
            }

            InitActionPool();
        }

        private static void InitActionPool()
        {
            ActionClassPoolManager poolManager = ActionClassPoolManager.Instance;
            if(poolManager != null)
            {
                var enu = EventTypeDic.GetEnumerator();
                while(enu.MoveNext())
                {
                    if(!enu.Current.Key.IsAbstract)
                    {
                        poolManager.AddActionClassPool(enu.Current.Key, new ActionClassPool(enu.Current.Key));
                    }
                }

                Type trackType = typeof(Track);
                poolManager.AddActionClassPool(trackType, new ActionClassPool(trackType));
            }
// 
//             Type actionType = typeof(Action);
//             poolManager.AddActionClassPool(actionType, new ActionClassPool(actionType));
        }

        private static bool IsSubClassOf(Type type, Type baseType)
        {
            var b = type.BaseType;
            while (b != null)
            {
                if (b.Equals(baseType))
                {
                    return true;
                }
                b = b.BaseType;
            }
            return false;
        }

        private static void InitPropertyTypeDic(Type eventType)
        {
            if (!PropertyDic.ContainsKey(eventType))
            {
                PropertyDic.Add(eventType, new ListView<FieldInfo>());
            }

            PropertyDic[eventType].Clear();
            FieldInfo[] infos = eventType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (infos == null) return;
            int qmax = infos.Length;
            for (int q = 0; q < qmax; q++)
            {
                PropertyDic[eventType].Add(infos[q]);
            }
        }

        public static BaseEvent GetEventInstance(Type eventType)
        {
            if(EventTypeDic.ContainsKey(eventType))
            {
                return EventTypeDic[eventType];
            }

            BaseEvent be = (BaseEvent)Activator.CreateInstance(eventType);
            EventTypeDic.Add(eventType, be);
            return be;
        }
    }
}
