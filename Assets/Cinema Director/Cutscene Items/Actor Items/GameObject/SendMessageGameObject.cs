using System.Collections.Generic;
using UnityEngine;

namespace CinemaDirector
{
    public enum eFunction
    {
        None,
        ChangePartnerFight
    }
    [CutsceneItemAttribute("Game Object", "Send Message", CutsceneItemGenre.ActorItem)]
    public class SendMessageGameObject : CinemaActorEvent
    {
        static void ChangePartnerFightStatus(string [] args)
        {

        }

        delegate void Callback(string[] args);

        private static Dictionary<eFunction, Callback> m_Callback = new Dictionary<eFunction, Callback>();

        public static void Register()
        {
            m_Callback[eFunction.ChangePartnerFight] = ChangePartnerFightStatus;
        }
        
        public eFunction m_Function;
        public string[] m_Args;
        public override void Trigger(GameObject actor)
        {

        }
    }
}