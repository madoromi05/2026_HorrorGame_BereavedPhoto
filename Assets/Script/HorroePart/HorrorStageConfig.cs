using UnityEngine;

/// <summary>
/// ホラーシーン1つ分の設定をまとめた ScriptableObject。
/// HorrorSceneConfigurator の Inspector に直接アサインすることで、
/// GameProgressManager のランタイム状態に依存せずシーンを構成できる。
/// </summary>
[CreateAssetMenu(fileName = "HorrorStageConfig", menuName = "Horror/HorrorStageConfig")]
public class HorrorStageConfig : ScriptableObject
{
    [Tooltip("このシーンで使用するダンジョン構成（敵プレハブを含む）")]
    [SerializeField] private RoomDataBase _roomDataBase;

    [Tooltip("デバッグ用ラベル（ログに表示）")]
    [SerializeField] private string _label;

    public RoomDataBase RoomDataBase => _roomDataBase;
    public string Label => _label;
}
