using UnityEngine;
using System.Collections.Generic;
using System.Collections;
namespace CinemaDirector
{
    public enum eActorKind
    {
        eActorKind_None,
        eActorKind_Player,
        eActorKind_MainPlayer,
        eActroKind_NPC,
        Camera,
        SceneObject,
    }

    [TrackGroupAttribute("ActorTrackGroup", TimelineTrackGenre.ActorTrack), ExecuteInEditMode]
    public class ActorTrackGroup : TrackGroup
    {
        [SerializeField] public Transform actor;

        [SerializeField] private uint id;
        public string m_ResName;
        public Vector3 pos;
        private Vector3 m_CachePosition;
        public Vector3 rotation;
        public eActorKind m_ActorKind;
        public float scale = 1;
        /// <summary>
        /// The Actor that this TrackGroup is focused on.
        /// </summary>
        public Transform Actor
        {
            get
            {
                if (actor == null)
                {
                    LoadActor();
                    if (actor == null)
                    {
                        return null;
                    }

                    if (m_ActorKind < eActorKind.Camera && m_ActorKind > eActorKind.eActorKind_None)
                    {
                        m_CachePosition = actor.position;
                        actor.gameObject.SetActive(true);
                        actor.transform.position = pos;
                        actor.transform.eulerAngles = rotation;
                        if (scale != 0)
                        {
                            actor.transform.localScale = new Vector3(scale, scale, scale);
                        }
                        ProjectorShadow.AddRenderer(actor);
                    }
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        return actor;
                    }
#endif
                }
                return actor;
            }
            set
            {
                actor = value;;
            }
        }

        public uint Id
        {
            get { return id; }
            set { id = value; }
        }
        private void SetActorEndStatus()
        {
            
        }
        private void OnDestroy()
        {
            if (m_ActorKind > eActorKind.eActroKind_NPC)
                return;
            if (actor == null)
                return;
            if ((m_ActorKind == eActorKind.eActorKind_MainPlayer || m_ActorKind == eActorKind.eActorKind_Player)&&Application.isPlaying)
            {
                SetActorEndStatus();
            }
            else
            {
            }
            actor = null;
        }


        private void LoadActor()
        {
            
        }
    }
}