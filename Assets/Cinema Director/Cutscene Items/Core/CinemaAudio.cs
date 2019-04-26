using UnityEngine;

namespace CinemaDirector
{
    [CutsceneItemAttribute("Audio", "Play Audio", typeof(AudioClip), CutsceneItemGenre.AudioClipItem)]
    public class CinemaAudio : TimelineActionFixed
    {
        private bool wasPlaying = false;
        public string m_Path;
        
        public void Trigger()
        {

        }

        public void End()
        {
            Stop();
        }

        public void UpdateTime(float time, float deltaTime)
        {
            //FMOD_StudioEventEmitter audio = gameObject.GetComponent<FMOD_StudioEventEmitter>();
            //if (audio != null)
            //{
            //    audio.mute = false;
            //    time = Mathf.Clamp(time, 0, audio.clip.length) + InTime;

            //    if ((audio.clip.length - time) > 0.0001)
            //    {
            //        if (Cutscene.State == Cutscene.CutsceneState.Scrubbing)
            //        {
            //            audio.time = time;
            //        }
            //        if (!audio.isPlaying)
            //        {
            //            audio.time = time;
            //            audio.Play();
            //        }
            //    }
            //}
        }

        public void Resume()
        {

        }

        public void Pause()
        {
            AudioSource audio = gameObject.GetComponent<AudioSource>();
            if (audio != null)
            {
                wasPlaying = false;
                if (audio.isPlaying)
                {
                    wasPlaying = true;
                }
                
                audio.Pause();
            }
        }

        public override void Stop()
        {
            
        }

        public void SetTime(float audioTime)
        {
            AudioSource audio = gameObject.GetComponent<AudioSource>();
            if (audio != null && audio.clip != null)
            {
                audioTime = Mathf.Clamp(audioTime, 0, audio.clip.length);
                if ((audio.clip.length - audioTime) > 0.0001)
                {
                    audio.time = audioTime;
                }
            }
        }

        public override void SetDefaults(Object PairedItem)
        {

        }
    }
}