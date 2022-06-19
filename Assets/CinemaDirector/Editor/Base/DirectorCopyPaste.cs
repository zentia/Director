using UnityEngine;

namespace CinemaDirector
{
	public static class DirectorCopyPaste
	{
		private static DirectorObject clipboard;
		public static DirectorObject deepCopy;
		public static float time;

		public static void Copy(DirectorObject obj)
		{
			clipboard = obj;
			deepCopy = Object.Instantiate(obj);
			deepCopy.name = DirectorControlHelper.GetNameForDuplicate(obj.name);
			deepCopy.hideFlags = HideFlags.HideAndDontSave;
		}

		public static DirectorObject Paste(DirectorObject parent)
		{
			return DirectorObject.Create(deepCopy, parent, deepCopy.name);
		}

		public static Object Duplicate(Object source)
        {
			Object gameObject = null;
			if (source != null)
            {
				gameObject = Object.Instantiate(source);
				gameObject.name = source.name;
            }
			return gameObject;
        }

		public static DirectorObject Peek()
		{
			return clipboard;
		}
	}
}