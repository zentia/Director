using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using AGE;
using EditorExtension;
using Gear;
using UnityEngine;

namespace CinemaDirector
{
    public partial class Cutscene
    {
        public string path => actionName;

        private DateTime lastModifyTime;
        
        public void ExportXML(string assetsPath)
        {
            string relPath = assetsPath + actionName;
            if (string.IsNullOrEmpty(actionName) || !File.Exists(relPath))
            {
#if UNITY_EDITOR
                relPath = UnityEditor.EditorUtility.SaveFilePanel("保存AGE", assetsPath, "age.xml", "xml");
#endif
                if (string.IsNullOrEmpty(relPath))
                {
                    return;
                }
                name = relPath.GetRelativePath(assetsPath);
            }
            
            var xmlDoc = new XmlDocument();
            var project = xmlDoc.CreateElement("Project");
            Export(project);
            xmlDoc.AppendChild(project);
            xmlDoc.Save(relPath);
            DetectExternalChanged(assetsPath);
        }

        public void Load(string xmlPath, bool relative, string assetsPath)
        {
            state = CutsceneState.Inactive;
            name = xmlPath;
            if (string.IsNullOrEmpty(xmlPath))
            {
                return;
            }
            var relPath = relative ? assetsPath + xmlPath : xmlPath;
            if (!File.Exists(relPath) || Path.GetExtension(xmlPath) != ".xml")
            {
                Debug.LogErrorFormat("{0} load failed.", relPath);
                return;
            }
            var doc = new XmlDocument();
            doc.Load(relPath);
            var projectNode = doc.SelectSingleNode("Project") as XmlElement;
            if (projectNode == null)
            {
                Debug.LogErrorFormat("{0} bad file.", relPath);
                return;
            }
            Import(projectNode);
            DetectExternalChanged(assetsPath);
        }

        public TrackGroup GetOrAddTrackGroup(string trackGroupName)
        {
            var trackGroup = Children.Find(i => i.name == trackGroupName) as TrackGroup;
            if (trackGroup == null)
            {
                trackGroup = CreateChild() as TrackGroup;
                trackGroup.name = trackGroupName;
            }

            return trackGroup;
        }

        public override void Import(XmlElement xmlElement)
        {
            base.Import(xmlElement);
            var templateObjectList = xmlElement.SelectSingleNode("TemplateObjectList");
            var action = xmlElement.SelectSingleNode("Action");
            enabled = false;
            length = float.Parse(action.Attributes["length"].Value);
            isLooping = bool.Parse(action.Attributes["loop"].Value);
            foreach (XmlElement templateObject in templateObjectList)
            {
                var objectName = templateObject.GetAttribute("objectName");
                var template = new TemplateObject
                {
                    templateObject =
                    {
                        name = objectName,
                        id = int.Parse(templateObject.GetAttribute("id")),
                        isTemp = bool.Parse(templateObject.GetAttribute("isTemp")),    
                    }
                };
                AddTemplateObject(template.templateObject.name);
                m_templateObjectList.Add(template);
            }
            RefreshTemplateObject();
            foreach (XmlElement trackElement in action.ChildNodes)
            {
                var trackName = trackElement.GetAttribute("trackName");
                var trackGroupName = "Action";
                if (trackElement.HasAttribute("group"))
                {
                    trackGroupName = trackElement.GetAttribute("group");
                }
                var trackGroup = GetOrAddTrackGroup(trackGroupName);
                var eventType = "AGE." + trackElement.GetAttribute("eventType");
                var type = Type.GetType(eventType);
                if (type == null)
                {
                    Debug.LogErrorFormat("{0} not found!", eventType);
                    continue;
                }
                var track = Create(typeof(GenericTrack), trackGroup, trackName) as TimelineTrack;
                if (track == null)
                {
                    continue;
                }
                track.InitTrackExtraParam(this, eventType);
                track.ItemType = type;
                track.Import(trackElement);
            }
        }

        private void SetTrackGroupAttribute(XmlDocument ownerDocument, string name, int id, XmlElement trackGroupList, List<string> list)
        {
            list.Add(name);
            var trackGroup = ownerDocument.CreateElement("TemplateObject");
            trackGroup.SetAttribute("objectName", name);
            trackGroup.SetAttribute("id", id.ToString());
            trackGroup.SetAttribute("isTemp", false.ToString());
            trackGroupList.AppendChild(trackGroup);
        }

        public override void Export(XmlElement xmlElement)
        {
            base.Export(xmlElement);
            var ownerDocument = xmlElement.OwnerDocument;
            var trackGroupList = ownerDocument.CreateElement("TemplateObjectList");
            var action = ownerDocument.CreateElement("Action");
            var list = new List<string>();
            var id = 0;
            foreach (var actor in m_templateObjectList)
            {
                if (string.IsNullOrEmpty(actor.templateObject.name) || list.Contains(actor.templateObject.name))
                    continue;
                SetTrackGroupAttribute(ownerDocument, actor.templateObject.name, id++, trackGroupList, list);
            }
            foreach (var child in Children)
            {
                child.Export(action);
            }
            xmlElement.AppendChild(trackGroupList);
            action.SetAttribute("length", Duration.ToString());
            action.SetAttribute("loop", isLooping.ToString());
            xmlElement.AppendChild(action);
        }
        
        public bool DetectExternalChanged(string assetsPath)
        {
            var newModifyTime = EditorHelper.GetLastModifiedTime(assetsPath + path);
            var result = newModifyTime > lastModifyTime;
            lastModifyTime = newModifyTime;
            return result;
        }
    }
}