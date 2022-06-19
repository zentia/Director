using System.Collections;
using UnityEngine.Events;

namespace CinemaDirector
{
    public class DirectorObjectEvent : UnityEvent<DirectorObject>
    {

    }
    public delegate IEnumerator CoroutinesEvent();

    public class DirectorCoroutineEvent : UnityEvent<CoroutinesEvent>
    {

    }

    public class DirectorEvent
    {
        public static DirectorObjectEvent DestroyObject = new DirectorObjectEvent();
        public static DirectorCoroutineEvent ExecuteCoroutines = new DirectorCoroutineEvent();
        public static DirectorCoroutineEvent StopCoroutines = new DirectorCoroutineEvent(); 
    }
}
