using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

internal class DirectorControlHelper
{
	internal static Type[] GetAllSubTypes(Type ParentType)
	{
		List<Type> list = new List<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type[] types = assemblies[i].GetTypes();
			for (int j = 0; j < types.Length; j++)
			{
				Type type = types[j];
				if (type.IsSubclassOf(ParentType))
				{
					list.Add(type);
				}
			}
		}
		return list.ToArray();
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

	public static string GetNameForDuplicate(Behaviour behaviour, string name)
	{
		string value = Regex.Match(name, "(\\d+)$").Value;
		int num = 1;
		string result;
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
