/********************************************************************
	created:	2017/03/24
	created:	24:3:2017   10:39
	filename: 	H:\ssk\client_proj\UnityProj\Assets\Scripts\Framework\AssetService\Asset\AssetProcessorAge.cs
	file path:	H:\ssk\client_proj\UnityProj\Assets\Scripts\Framework\AssetService\Asset
	file base:	AssetProcessorAge
	file ext:	cs
	author:		benzhou
	
	purpose:	Age资源处理器
*********************************************************************/

using AGE;
using System.IO;
using System.Xml;
using CinemaDirector;
using UnityEngine;

namespace Assets.Scripts.Framework.AssetService.Asset
{
    public static class AssetProcessorAge
    {
        public static bool InstantiateWithXml(byte[] resource, string resourceName, out ActionCommonData commonData, out Cutscene act)
        {
            commonData = null;
            act = null;

            if (resource == null)
            {
                return false;
            }

            var ms = new MemoryStream(resource);
            var doc = new XmlDocument();
            doc.Load(ms);

            var projectNode = doc.SelectSingleNode("Project") as XmlElement;
            if (projectNode == null)
            {
                return false;
            }
            
            act = ScriptableObject.CreateInstance<Cutscene>();
#if UNITY_EDITOR
            //DirectorControlSettings.Instance.assetsPath;
            var path = Path.Combine("RawAsset", resourceName);
            act.name = resourceName;
#endif
            act.Import(projectNode);
            return true;
        }

        public static bool InstantiateWithXml(BaseAsset ba, out ActionCommonData commonData, out Cutscene act)
        {
            return InstantiateWithXml(ba.Resource.Bytes, ba.BaseData.Name, out commonData, out act);
        }
    }
}