using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace CinemaDirector
{
	[Serializable]
	public class TemplateObject : ICloneable
	{
		[HideLabel]
		public AGE.TemplateObject templateObject = new AGE.TemplateObject();
        [OnValueChanged("OnGameObjectChanged")]
		public GameObject gameObject;

		private void OnGameObjectChanged()
        {
			if (gameObject != null)
            {
				templateObject.fileName = CommonTools.GetGameObjectPath(gameObject);
            }
            else
            {
				templateObject.fileName = null;
            }
        }
		
		public object Clone()
		{
			var obj = new TemplateObject();
			obj.templateObject.id = templateObject.id;
			obj.templateObject.isTemp = templateObject.isTemp;
			obj.templateObject.name = templateObject.name;
			obj.gameObject = gameObject;
			return obj;
		}
	}
}