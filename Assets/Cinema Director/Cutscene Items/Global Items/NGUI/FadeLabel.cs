using UnityEngine;
using UnityEngine.UI;

namespace CinemaDirector
{
    [CutsceneItem("UI", "Label", CutsceneItemGenre.GlobalItem)]
    public class FadeLabel : CinemaGlobalAction
    {
        public Text target;
        public Color tint = Color.gray;
        public string m_TargetPath;
        private Transform m_Actor;
		public Vector2 m_Offset;
#if UNITY_EDITOR
        public void SelectTarget()
        {
            ActorTargetSelector.Show(OnSelectTarget);
        }

        public void OnSelectTarget(string path)
        {
            m_TargetPath = path;
        }
#endif

        public override void Trigger()
        {
            if (target != null)
            {
				target.gameObject.SetActive(true);
				if (!string.IsNullOrEmpty (m_TargetPath)) {
					var go = GetActor (m_TargetPath);
					if (go)
						m_Actor = go.transform.Find ("HUD");
				} else {
					m_Actor = null;
				}
				if (Application.isPlaying) {
					
				} 
            }
        }

        public override void End()
        {
            if (target != null)
            {
				target.gameObject.SetActive(false);
            }
        }

		public override void Stop()
		{
			End ();
		}
        
        public override void UpdateTime(float time, float deltaTime)
        {
            if (m_Actor && target)
            {
				var position = Cutscene.camera.WorldToScreenPoint(m_Actor.position);
				target.rectTransform.position = new Vector3(m_Offset.x+ position.x, m_Offset.y + position.y);
            }
        }
    }
}