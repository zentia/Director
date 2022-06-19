
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AGE
{
    public  interface  PooledActionClass
    {
        void OnUse(PooledActionClass clonedData);

        void OnRelease();

        uint GetMaxInitCount();
    }

    public class ActionClassPool
    {
        private ListView<PooledActionClass> _poolList = new ListView<PooledActionClass>();
        private Type _type;

        public ActionClassPool(Type type)
        {
            _type = type;
            OnInit();
        }

        public void OnInit()
        {
            PooledActionClass pooledObj = Activator.CreateInstance(_type) as PooledActionClass;
            _poolList.Add(pooledObj);
            for (int i = 1; i < pooledObj.GetMaxInitCount(); i++)
            {
                _poolList.Add(Activator.CreateInstance(_type) as PooledActionClass);
            }
        }

        public PooledActionClass GetActionObject(PooledActionClass clonedData)
        {
            PooledActionClass result = null;
            if (_poolList.Count > 0)
            {
                result = _poolList[0];
                _poolList.RemoveAt(0);
            }
            else
            {
                result = Activator.CreateInstance(_type) as PooledActionClass;
            }

            result.OnUse(clonedData != null ? clonedData : result);
            return result;
        }

        public void ReleaseActionObject(PooledActionClass obj)
        {
            if (obj != null)
            {
                obj.OnRelease();
                _poolList.Add(obj);
            }
        }
    }
}