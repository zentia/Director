using Assets.Scripts.Framework.Lua;
using Assets.Scripts.GameLogic;

namespace TimelineRuntime
{
    public enum PlayerType
    {
        Host,
        Observe
    }

    [TimelineItem("timeline", "Battle level show", TimelineItemGenre.GlobalItem)]
    public class BattleFieldShowEvent : TimelineGlobalEvent
    {
        public bool show;
        public PlayerType playerType;

        public override void Trigger()
        {
            var instance = Project8Logic.GetInstance();
            var player = playerType switch
            {
                PlayerType.Host => PlayerUtil.GetHostInstance(),
                PlayerType.Observe => PlayerUtil.GetObInstance(),
                _ => null
            };
            if (instance != null && player != null && instance.ArtField != null &&
                instance.ArtField.curArtField != null)
            {
                if (show)
                {
                    instance.ArtField.curArtField.ShowAndSetFieldTransform(player.SceneManagement.BattleField.transform.localPosition, false, false);
                    LuaService.GetInstance().Interaction.SendLuaEvent("PersonaliaztionPlayAgeChangeScene", null);
                }
                else
                {
                    instance.ArtField.curArtField.Hide();
                }
            }
        }
    }
}
