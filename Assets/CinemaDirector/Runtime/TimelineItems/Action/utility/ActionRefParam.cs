namespace AGE
{
	//定义Action引用参数名关键字列表，程序使用Action间传参
    //Note: 遵循命名规则，避免Action编辑器的引用参数冲突（不可重名！）

	public class ActionRefParam 
	{
        public const string S_IsComboAttack = "S_IsComboAttack"; // type: int

        public const string S_BattleCommandContext = "S_BattleCommandContext";//type:BattleCommandContext

        public const string S_BattleProxy = "S_BattleProxy";//type:IBattleProxy

        public const string S_SceneRes = "S_SceneRes";

        public const string S_ParentActionInstanceID = "S_ParentActionInstanceID";

        public const string Effect_SceneEffectData = "Effect_SceneEffectData"; //场景跑动特效
    }
}
