using System.Collections;
using UnityEngine.Events;

namespace CinemaDirector
{
    public class DirectorObjectEvent : UnityEvent<DirectorObject>
    {

    }
    public delegate IEnumerator CoroutineEvent();

    public class DirectorCoroutineEvent : UnityEvent<CoroutineEvent>
    {

    }

    public class DirectorEvent
    {
        public static DirectorObjectEvent DestroyObject = new DirectorObjectEvent();
        public static DirectorCoroutineEvent StartCoroutine = new DirectorCoroutineEvent();
        public static DirectorCoroutineEvent StopCoroutine = new DirectorCoroutineEvent(); 
    }
}
