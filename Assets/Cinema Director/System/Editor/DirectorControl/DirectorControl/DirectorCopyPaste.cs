using UnityEngine;

public class DirectorCopyPaste
{
	private static Behaviour clipboard;
	private static GameObject deepCopy;
    public static float time;

	public static void Copy(Behaviour obj)
	{
		clipboard = obj;
		GameObject gameObject = clipboard.gameObject;
		deepCopy = Object.Instantiate(gameObject);
		deepCopy.name = DirectorControlHelper.GetNameForDuplicate(obj, gameObject.name);
		deepCopy.hideFlags = (HideFlags)(13);
	}

	public static GameObject Paste(Transform parent)
	{
		GameObject gameObject = null;
		if (clipboard != null)
		{
			gameObject = Object.Instantiate(deepCopy);
			gameObject.name = deepCopy.name;
			gameObject.transform.parent=(parent);
		}
		return gameObject;
	}

	public static Behaviour Peek()
	{
		return clipboard;
	}
}
