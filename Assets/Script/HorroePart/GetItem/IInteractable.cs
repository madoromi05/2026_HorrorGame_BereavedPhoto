namespace HorrorGame.Interaction
{
    /// <summary>
    /// プレイヤーがインタラクトキーで操作できるオブジェクトの契約。
    /// ドア・スイッチ・アイテムなど、インタラクト可能なあらゆる
    /// オブジェクトに実装する。
    /// PlayerInteractorはこの型だけを知っていれば良く
    /// </summary>
    public interface IInteractable
    {
        // インタラクト可能な状態かどうか。
        bool CanInteract { get; }

        // インタラクトUIに表示するヒントテキスト。
        string HintText { get; }

        // インタラクトキーが押されたときに呼ばれる。
        void OnInteract();
    }
}