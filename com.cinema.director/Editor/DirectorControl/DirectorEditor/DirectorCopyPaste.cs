using System;
using UnityEngine;

public class DirectorCopyPaste
{
    private static Behaviour clipboard;
    private static GameObject deepCopy;

    public static void Copy(Behaviour obj)
    {
        clipboard = obj;
        GameObject gameObject = clipboard.gameObject;
        deepCopy = UnityEngine.Object.Instantiate<GameObject>(gameObject);
        deepCopy.name = DirectorControlHelper.GetNameForDuplicate(obj, gameObject.name);
        deepCopy.hideFlags = HideFlags.HideAndDontSave;
    }

    public static GameObject Paste(Transform parent)
    {
        GameObject obj2 = null;
        if (clipboard != null)
        {
            obj2 = UnityEngine.Object.Instantiate<GameObject>(deepCopy);
            obj2.name = deepCopy.name;
            obj2.transform.parent = parent;
        }
        return obj2;
    }

    public static Behaviour Peek() => 
        clipboard;
}

