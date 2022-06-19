
using System.Collections;
using System.Collections.Generic;

namespace AGE
{

    // this class hold the info of the data object using reference parameter
    public class RefData
    {
        public System.Reflection.FieldInfo fieldInfo;
        public object dataObject;

        public RefData(System.Reflection.FieldInfo field, object obj)
        {
            fieldInfo = field;
            dataObject = obj;
        }
    }

    public class RefParamObject
    {
        public object value;
        public bool dirty;

        public RefParamObject(object v)
        {
            value = v;
            dirty = false;
        }
    }

    public class RefParamOperator
    {
        public DictionaryView<string, RefParamObject> refParamList;
        public DictionaryView<string, ListView<RefData>> refDataList;

        public RefParamOperator()
        {
            refParamList = new DictionaryView<string, RefParamObject>();
            refDataList = new DictionaryView<string, ListView<RefData>>();
        }

        public RefParamObject AddRefParamReset(string name, object v)
        {
            if (refParamList.ContainsKey(name))
            {
                refParamList.Remove(name);
            }

            return AddRefParam(name, v);
        }

        public RefParamObject AddRefParam(string name, object v)
        {
            if (!refParamList.ContainsKey(name))
            {
                RefParamObject obj = new RefParamObject(v);
                refParamList.Add(name, obj);
                return obj;
            }
            return refParamList[name];
        }

        public RefData AddRefData(string name, System.Reflection.FieldInfo field, object data)
        {
            ListView<RefData> lst;
            if (refDataList.ContainsKey(name))
                lst = refDataList[name];
            else
            {
                lst = new ListView<RefData>();
                refDataList.Add(name, lst);
            }
            RefData obj = new RefData(field, data);
            lst.Add(obj);
            return obj;
        }

        public void SetRefParamAndData(string name, object newValue)
        {
            System.Type dstType = newValue.GetType();
            bool isValidType = false;
            if (refParamList.ContainsKey(name))
            {
                object parmVal = refParamList[name].value;
                if (parmVal != null && parmVal.GetType() == dstType)
                {
                    isValidType = true;
                    refParamList[name].value = newValue;
                }
            }
            if (isValidType && refDataList.ContainsKey(name))
            {
                ListView<RefData> lst = refDataList[name];
                for (int i = 0; i < lst.Count; i++)
                {
                    RefData rpd = lst[i];
                    if (rpd != null && rpd.fieldInfo != null)
                        rpd.fieldInfo.SetValue(rpd.dataObject, newValue);
                }
            }
        }

        public object GetRefParamValue(string name)
        {
            if (refParamList.ContainsKey(name))
            {
                RefParamObject obj = refParamList[name];
                return obj.value;
            }
            return null;
        }

        public T GetRefParamValue<T>(string name)
        {
            if (refParamList.ContainsKey(name))
            {
                RefParamObject obj = refParamList[name];
                return (T)obj.value;
            }
            return default(T);
        }

        public void CopyRefParamFrom(Action fromAction)
        {
            var em = fromAction.refParams.refParamList.GetEnumerator();
            while (em.MoveNext())
            {
                if (em.Current.Value != null)
                {
                    AddRefParam(em.Current.Key, em.Current.Value.value);
                }
            }
        }
    }

    public class ActionCommonData
    {
        public ListView<TemplateObject> templateObjects = new ListView<TemplateObject>();
        public List<string> predefRefParamNames = new List<string>();
    }

}
