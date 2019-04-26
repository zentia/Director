using UnityEngine;
using UnityEngine.UI;

namespace CinemaDirector
{
    /// <summary>
    /// An action that fades in a texture over the first 25% of length, shows for 50% of time length
    /// and fades away over the final 25%.
    /// </summary>
    [CutsceneItem("GUITexture", "Fade Texture", CutsceneItemGenre.GlobalItem)]
    public class FadeTexture : CinemaGlobalAction
    {
        // The GUITexture to show
        public Image target;

        // Optional Tint
        public Color tint = new Color(1,1,1,1);

        /// <summary>
        /// Disable the Texture and make it clear.
        /// </summary>
        void Awake()
        {
            if (target != null)
            {
                //target.enabled = false;
                target.color = Color.clear;
            }
        }

        /// <summary>
        /// Trigger this event, enable the texture and make it clear.
        /// </summary>
        public override void Trigger()
        {
            if (target != null)
            {
                target.gameObject.SetActive(true);
                target.color = Color.clear;
            }
        }

        /// <summary>
        /// Reverse the start of this action by disabling the texture.
        /// </summary>
        public override void ReverseTrigger()
        {
            End();
        }

        /// <summary>
        /// Update the fading/showing of this texture.
        /// </summary>
        /// <param name="time">The time of this action.</param>
        /// <param name="deltaTime">The deltaTime since last update.</param>
        public override void UpdateTime(float time, float deltaTime)
        {
            if (target != null)
            {
                float transition = time / Duration;
                if (transition <= 0.25f)
                {
                    FadeToColor(Color.clear, tint, (transition / 0.25f));
                }
                else if (transition >= 0.75f)
                {
                    FadeToColor(tint, Color.clear, (transition - 0.75f) / .25f);
                }
            }
        }

        /// <summary>
        /// Update this action to an arbitrary time.
        /// </summary>
        /// <param name="time">The new time.</param>
        /// <param name="deltaTime">The deltaTime since last update.</param>
        public override void SetTime(float time, float deltaTime)
        {
            if (target != null)
            {
                target.enabled = true;
                if (time >= 0 && time <= Duration)
                {
                    UpdateTime(time, deltaTime);
                }
                else if (target.enabled)
                {
                    //target.enabled = false;
                }
            }
        }

        /// <summary>
        /// End this action and disable the texture.
        /// </summary>
        public override void End()
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Trigger the action from the end in reverse.
        /// </summary>
        public override void ReverseEnd()
        {
            Trigger();
        }

        /// <summary>
        /// Stop this action and disable its texture.
        /// </summary>
        public override void Stop()
        {
            if (target != null)
            {
                //target.enabled = false;
            }
        }

        /// <summary>
        /// Fade between two colours over a transition value.
        /// </summary>
        /// <param name="from">The start color.</param>
        /// <param name="to">The end color.</param>
        /// <param name="transition">The transition amount.</param>
        private void FadeToColor(Color from, Color to, float transition)
        {
            target.color = Color.Lerp(from, to, transition);
        }
    }
}