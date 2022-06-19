using System;
using UnityEngine;
using Assets.Scripts.Framework.AssetService;

namespace AGE
{
	[Serializable]
	public class TemplateObject 
	{
		public string            name;
		[HideInInspector]
		public int               id;
        [HideInInspector]
		public bool              isTemp;
        [HideInInspector]
		public string fileName;
        [HideInInspector]
		public AssetType assetType;
		public BaseAsset asset { get; set; }
	}
}
