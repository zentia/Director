using System;
using System.Reflection;
using UnityEditor;

namespace CinemaDirector
{
	[Serializable]
	public abstract class SidebarControl : DirectorBehaviourControl, IComparable
	{
		public bool isExpanded;

		public int expandedSize = 2;
		public event SidebarControlHandler SelectRequest;

		public int[] Ordinal
		{
			get;
			set;
		}

		internal string IsExpandedKey
		{
			get
			{
				return string.Format("Director.{0}.isExpanded", Behaviour.name);
			}
		}

		public void RequestSelect()
		{
			if (SelectRequest != null)
			{
				SelectRequest(this, new SidebarControlEventArgs(Behaviour, this));
			}
		}

		public void RequestSelect(SidebarControlEventArgs args)
		{
			if (SelectRequest != null)
			{
				SelectRequest(this, args);
			}
		}
		public int CompareTo(object obj)
		{
			if (obj == null)
			{
				return 1;
			}
			SidebarControl sidebarControl = obj as SidebarControl;
			if (sidebarControl != null)
			{
				int num = Math.Min(Ordinal.Length, sidebarControl.Ordinal.Length);
				for (int i = 0; i < num; i++)
				{
					int num2 = Ordinal[i] - sidebarControl.Ordinal[i];
					if (num2 != 0)
					{
						return num2;
					}
				}
				return Ordinal.Length - sidebarControl.Ordinal.Length;
			}
			throw new ArgumentException("Comparison object is not of type SidebarControl.");
		}

		internal void Select()
		{
			DirectorWindow.GetSelection().Add(Behaviour);
		}

		internal void SetExpandedFromEditorPrefs()
		{
			string isExpandedKey = IsExpandedKey;
			if (EditorPrefs.HasKey(isExpandedKey))
			{
				isExpanded = EditorPrefs.GetBool(isExpandedKey);
				return;
			}
			EditorPrefs.SetBool(isExpandedKey, isExpanded);
		}
	}
}
