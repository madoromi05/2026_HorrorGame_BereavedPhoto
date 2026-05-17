namespace HorrorGame.Interaction
{
    /// <summary>
    /// プレイヤーがインタラクトキーで操作できるオブジェクトの契約。
    /// ドア・スイッチ・アイテムなど、インタラクト可能なあらゆる
    /// オブジェクトに実装する。
    /// PlayerInteractorはこの型だけを知っていれば良く、
    /// 具体的なオブジェクトの種類を知る必要はない。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>インタラクト可能な状態かどうか。</summary>
        bool CanInteract { get; }

        string HintText { get; }

        /// <summary>インタラクトキーが押されたときに呼ばれる。</summary>
        void OnInteract();
    }
}