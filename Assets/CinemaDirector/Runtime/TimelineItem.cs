using UnityEngine;
using System;
using System.Xml;
using Assets.Plugins.Common;

namespace CinemaDirector
{
    [Serializable]
    public class TimelineItem : DirectorObject
    {
        private bool destroy;
        public Vector3 position
        {
            get
            {
                return new Vector3(Firetime, 0);
            }
        }
        
        public float time;
        
        public bool ContainsTime(float time)
        {
            return Mathf.Abs(time - Firetime) < 0.1;
        }

        public float Firetime
        {
            get
            {
                return time;
            }
            set
            {
                if (Mathf.Approximately(time, value))
                    return;
                Dirty = true;
                SetFiretime(value);
            }
        }

        private void SetFiretime(float value, bool force = false)
        {
            time = value;
            if (time < 0f)
            {
                time = 0f;
            }
            BindRawTrack(force);   
        }

        private void BindRawTrack(bool force)
        {
            if (destroy)
            {
                return;
            }
            var trackEvents = TimelineTrack.trackEvents;
            if (time > TimelineTrack.length)
            {
                trackEvents.Remove(this);
                trackEvents.Sort();
            }
            else if (!trackEvents.Contains(this))
            {
                trackEvents.Add(this);
                trackEvents.Sort();
            }
            else if (force)
            {
                trackEvents.Sort();
            }
        }

        /// <summary>
        /// Called when a cutscene begins or enters preview mode.
        /// </summary>
        public virtual void Initialize()
        {
            
        }

        /// <summary>
        /// Called when a cutscene ends or exits preview mode.
        /// </summary>
        public virtual void Stop() { }

        /// <summary>
        /// Called when a new timeline item is created from the Director panel.
        /// Override to set defaults to your timeline items.
        /// </summary>
        public virtual void SetDefaults() { }

        /// <summary>
        /// Called when a new timeline item is created from the Director panel with a paired item.
        /// Override to set defaults to your timeline items.
        /// </summary>
        /// <param name="PairedItem">The paired item of this timeline item.</param>
        public virtual void SetDefaults(UnityEngine.Object PairedItem) { }

        /// <summary>
        /// The cutscene that this timeline item is associated with. Can return null.
        /// </summary>
        public Cutscene Cutscene
        {
            get { return (TimelineTrack == null) ? null : TimelineTrack.Cutscene; }
        }

        /// <summary>
        /// The track that this timeline item is associated with. Can return null.
        /// </summary>
        public TimelineTrack TimelineTrack
        {
            get
            {
                return Parent as TimelineTrack;
            }
        }

        public override void Import(XmlElement xmlElement)
        {
            base.Import(xmlElement);
            var strTime = xmlElement.GetAttribute("time");
            var t = 0.0f; 
            if (float.TryParse(strTime, out t))
            {
                SetFiretime(t, true);    
            }
            else
            {
                SetFiretime(0, true);
            }
            foreach (XmlElement node in xmlElement.ChildNodes)
            {
                if (node == null)
                {
                    continue;
                }
                var n = node.GetAttribute("name");
                var field = GetType().GetField(n) ?? GetType().GetField(char.ToLower(n[0]) + n.Substring(1));
                if (field == null)
                {
                    Log.LogE("AGE","{0} no found {1}", GetType().ToString(), n);
                    continue;
                }
                switch (node.Name)
                {
                    case "TemplateObject":
                    {
                        var value = node.GetAttribute("id");
                        field.SetValue(this, int.Parse(value));
                    }
                        break;
                    case "string":
                    {
                        var value = node.GetAttribute("value");
                        field.SetValue(this, value);
                    }
                        break;
                    case "bool":
                    {
                        var value = node.GetAttribute("value");
                        field.SetValue(this, value == "true");
                    }
                        break;
                    case "EulerAngle":
                    {
                        var x = node.GetAttribute("x");
                        var y = node.GetAttribute("y");
                        var z = node.GetAttribute("z");
                        field.SetValue(this, Quaternion.Euler(new Vector3(float.Parse(x), float.Parse(y), float.Parse(z))));
                    }
                        break;
                    case "Vector3":
                    {
                        var x = node.GetAttribute("x");
                        var y = node.GetAttribute("y");
                        var z = node.GetAttribute("z");
                        field.SetValue(this, new Vector3(float.Parse(x),float.Parse(y),float.Parse(z)));
                    }
                        break;
                    case "Enum":
                    {
                        var value = node.GetAttribute("value");
                        int val;
                        if (int.TryParse(value, out val))
                        {
                            field.SetValue(this, Enum.ToObject(field.FieldType, int.Parse(value)));
                        }
                        else
                        {
                            field.SetValue(this, Enum.Parse(field.FieldType, value));
                        }
                    }
                        break;
                }
            }
        }

        public override void Export(XmlElement xmlElement)
        {
            base.Export(xmlElement);
            var document = xmlElement.OwnerDocument;
            if (document == null) 
                return;
            var evt = document.CreateElement("Event");
            evt.SetAttribute("eventName", GetType().ToString());
            evt.SetAttribute("time", $"{Firetime}");
            xmlElement.AppendChild(evt);
            var fields = GetType().GetFields();
            foreach (var fieldInfo in fields)
            {
                XmlElement itemElement;
                if (fieldInfo.FieldType.IsArray)
                {
                    itemElement = document.CreateElement("Array");
                    itemElement.SetAttribute("type", fieldInfo.FieldType.Name);
                }
                else if (fieldInfo.FieldType.IsEnum)
                {
                    itemElement = document.CreateElement("Enum");
                    itemElement.SetAttribute("value", fieldInfo.GetValue(this).ToString());
                }
                else if (fieldInfo.FieldType == typeof(int))
                {
                    var attributes = fieldInfo.GetCustomAttributes(typeof(TemplateAttribute), true);
                    if (attributes.Length > 0)
                    {
                        itemElement = document.CreateElement("TemplateObject");
                        itemElement.SetAttribute("id", fieldInfo.GetValue(this).ToString());
                        itemElement.SetAttribute("isTemp", "false");
                    }
                    else
                    {
                        itemElement = document.CreateElement("int");
                        itemElement.SetAttribute("value", fieldInfo.GetValue(this).ToString());
                    }
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    itemElement = document.CreateElement("string");
                    itemElement.SetAttribute("value", (string)fieldInfo.GetValue(this));
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    itemElement = document.CreateElement("float");
                    itemElement.SetAttribute("value", fieldInfo.GetValue(this).ToString());
                }
                else if (fieldInfo.FieldType == typeof(bool))
                {
                    itemElement = document.CreateElement("bool");
                    itemElement.SetAttribute("value", (bool)fieldInfo.GetValue(this) ? "true" : "false");
                }
                else if (fieldInfo.FieldType == typeof(Vector3))
                {
                    itemElement = document.CreateElement("Vector3");
                    var value = (Vector3)fieldInfo.GetValue(this);
                    itemElement.SetAttribute("x", $"{value.x}");
                    itemElement.SetAttribute("y", $"{value.y}");
                    itemElement.SetAttribute("z", $"{value.z}");
                }
                else if (fieldInfo.FieldType == typeof(Quaternion))
                {
                    itemElement = document.CreateElement("EulerAngle");
                    var value = (Quaternion)fieldInfo.GetValue(this);
                    itemElement.SetAttribute("x", $"{value.eulerAngles.x}");
                    itemElement.SetAttribute("y", $"{value.eulerAngles.y}");
                    itemElement.SetAttribute("z", $"{value.eulerAngles.z}");
                }
                else
                {
                    continue;
                }
                itemElement.SetAttribute("name", fieldInfo.Name);
                evt.AppendChild(itemElement);
            }
        }

        public virtual void Trigger()
        {
            
        }

        public void Reverse()
        {
            
        }
    }
}