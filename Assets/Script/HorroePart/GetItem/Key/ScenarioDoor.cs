// ScenarioDoor の機能は ExitDoor に統合されました。
// 既存のプレハブとの互換性のためにクラスは残してありますが、
// 新規作成するプレハブでは ExitDoor を直接使用してください。
namespace HorrorGame.Interaction
{
    public class ScenarioDoor : ExitDoor { }
}
