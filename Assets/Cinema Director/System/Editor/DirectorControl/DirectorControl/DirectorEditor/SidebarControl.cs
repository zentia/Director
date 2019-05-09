using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DirectorEditor
{
	public abstract class SidebarControl : DirectorBehaviourControl, IComparable
	{
		public bool isExpanded;

		public int expandedSize = 2;
		public event SidebarControlHandler SelectRequest;
		public event SidebarControlHandler DuplicateRequest;

		public int[] Ordinal
		{
			get;
			set;
		}

		internal string IsExpandedKey
		{
			get
			{
				PropertyInfo arg_2A_0 = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
				SerializedObject serializedObject = new SerializedObject(base.Behaviour);
				arg_2A_0.SetValue(serializedObject, 1, null);
				SerializedProperty serializedProperty = serializedObject.FindProperty("m_LocalIdentfierInFile");
				return string.Format("Director.{0}.{1}", serializedProperty.intValue, "isExpanded");
			}
		}

		public void RequestSelect()
		{
			if (SelectRequest != null)
			{
				SelectRequest(this, new SidebarControlEventArgs(base.Behaviour, this));
			}
		}

		public void RequestSelect(SidebarControlEventArgs args)
		{
			if (this.SelectRequest != null)
			{
				this.SelectRequest(this, args);
			}
		}

		public void RequestDuplicate()
		{
			if (this.DuplicateRequest != null)
			{
				this.DuplicateRequest(this, new SidebarControlEventArgs(base.Behaviour, this));
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
				int num = Math.Min(this.Ordinal.Length, sidebarControl.Ordinal.Length);
				for (int i = 0; i < num; i++)
				{
					int num2 = this.Ordinal[i] - sidebarControl.Ordinal[i];
					if (num2 != 0)
					{
						return num2;
					}
				}
				return this.Ordinal.Length - sidebarControl.Ordinal.Length;
			}
			throw new ArgumentException("Comparison object is not of type SidebarControl.");
		}

		internal void Select()
		{
			GameObject[] gameObjects = Selection.gameObjects;
			ArrayUtility.Add<GameObject>(ref gameObjects, Behaviour.gameObject);
			Selection.objects=(gameObjects);
		}

		internal void SetExpandedFromEditorPrefs()
		{
			string isExpandedKey = this.IsExpandedKey;
			if (EditorPrefs.HasKey(isExpandedKey))
			{
				this.isExpanded = EditorPrefs.GetBool(isExpandedKey);
				return;
			}
			EditorPrefs.SetBool(isExpandedKey, isExpanded);
		}
	}
}
