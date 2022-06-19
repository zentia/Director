using UnityEngine;
using System.Collections;
using System;

namespace AGE
{
    public class ActionClassPoolManager
    {
        public static ActionClassPoolManager Instance
        {
            get
            {
                return _instance;
            }
        }
        static ActionClassPoolManager _instance = new ActionClassPoolManager();


        private DictionaryView<Type, ActionClassPool> pools = new DictionaryView<Type, ActionClassPool>();

        //public ActionClassPool GetActionClassPool(Type key)
        //{
        //    var enu = pools.GetEnumerator();
         //   while (enu.MoveNext())
         //   {
         //       if (enu.Current.Key == key)
        //        {
        //            return enu.Current.Value;
        //        }
        //    }

       //     return null;
       // }

        public void AddActionClassPool(Type type, ActionClassPool pool)
        {
            if(type != null && !type.IsAbstract && !pools.ContainsKey(type))
            {
                pools.Add(type,pool);
            }
        }

        public ActionClassPool GetActionClassPool(Type type)
        {
            
            if (pools.ContainsKey(type))
            {
                return pools[type];
            }

            ActionClassPool acp = new ActionClassPool(type);
            AddActionClassPool(type, acp);
            return acp;
        }
    }
}