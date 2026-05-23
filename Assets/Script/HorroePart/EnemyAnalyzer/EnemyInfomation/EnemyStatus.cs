using UnityEngine;

/// <summary>
/// IAnalyzable の実装例。敵 GameObject にアタッチする。
/// ScriptableObject でデータ管理する場合はそちらから値を引くように変更する。
/// </summary>
public class EnemyStatus : MonoBehaviour, IAnalyzable
{
    [SerializeField] private int age = 25;
    [SerializeField] private string gender = "MALE";
    [SerializeField] private float height = 175.0f;
    [SerializeField] private float bodyWeight = 70.0f;
    [SerializeField] private string condition = "HEALTHY";

    public int Age => age;
    public string Gender => gender;
    public float Height => height;
    public float BodyWeight => bodyWeight;
    public string Condition => condition;
}