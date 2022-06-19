using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CinemaDirector
{
	internal class DirectorControlHelper
	{
		private static IEnumerable<Type> allTypes;
		
		internal static IEnumerable<Type> GetAllSubTypes(Type ParentType)
		{
			List<Type> list = new List<Type>();
			if (allTypes == null)
			{
				allTypes = ParentType.Assembly.GetTypes().Where(i=>i.Namespace == "CinemaDirector");
			}

			return allTypes.Where(i => i.IsSubclassOf(ParentType));
		}

		internal static int GetSubTypeDepth(Type type, Type parent)
		{
			if (type == null)
			{
				return 1000;
			}
			if (parent == type)
			{
				return 0;
			}
			return GetSubTypeDepth(type.BaseType, parent) + 1;
		}

		public static string GetUserFriendlyName(Component component, MemberInfo memberInfo)
		{
			return GetUserFriendlyName(component.GetType().Name, memberInfo.Name);
		}

		public static string GetUserFriendlyName(string componentName, string memberName)
		{
			string result = memberName;
			if (componentName == "Transform")
			{
				if (memberName == "localPosition")
				{
					result = "Position";
				}
				else if (memberName == "localEulerAngles")
				{
					result = "Rotation";
				}
				else if (memberName == "localScale")
				{
					result = "Scale";
				}
			}
			return result;
		}

		public static string GetNameForDuplicate(string name)
		{
			string value = Regex.Match(name, "(\\d+)$").Value;
			string result;
			int num;
			if (int.TryParse(value, out num))
			{
				num++;
				result = name.Substring(0, name.Length - value.Length) + num.ToString();
			}
			else
			{
				num = 1;
				result = name.Substring(0, name.Length - value.Length) + " " + num.ToString();
			}
			return result;
		}
	}
}