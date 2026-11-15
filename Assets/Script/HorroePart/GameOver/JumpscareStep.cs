using System.Collections;
using UnityEngine;

/// <summary>
/// ゲームオーバーの段：プレイヤーを敵へ向け、敵をカメラの目の前へ飛び込ませる
/// 敵がいなければ何もしない。カメラが無ければ向きを変えるだけ
/// </summary>
public class JumpscareStep : IGameOverStep
{
    private const float kMinEndDistance = 0.3f;	// カメラから最低でもこの距離（m）前に置く。近すぎるとカメラにめり込む

    private readonly Transform _camera;
    private readonly float _duration;
    private readonly float _startDistance;
    private readonly float _endDistance;
    private readonly float _heightOffset;

    public JumpscareStep(Transform camera, float duration, float startDistance, float endDistance, float heightOffset)
    {
        _camera        = camera;
        _duration      = duration;
        _startDistance = startDistance;
        _endDistance   = endDistance;
        _heightOffset  = heightOffset;
    }

    public IEnumerator Play(GameOverContext context)
    {
        var enemy = context.Enemy;
        if (enemy == null) yield break;

        if (FlatLook.TryGetRotation(enemy.position - context.Player.position, out var toEnemy))
            context.Player.eulerAngles = new Vector3(0f, toEnemy.eulerAngles.y, 0f);

        if (_camera == null) yield break;

        _camera.localEulerAngles = Vector3.zero;
        Vector3 startPos  = PointInFront(_startDistance);
        Vector3 targetPos = PointInFront(Mathf.Max(_endDistance, kMinEndDistance));
        enemy.position = startPos;

        yield return Tween.Run(_duration, t =>
        {
            enemy.position = Vector3.Lerp(startPos, targetPos, Tween.EaseInQuad(t));	// 後半で加速する

            if (FlatLook.TryGetRotation(_camera.position - enemy.position, out var lookCam))
                enemy.rotation = lookCam;
        });
    }

    private Vector3 PointInFront(float distance)	// カメラの水平な正面へ distance 進み、高さを目線＋オフセットにした点
    {
        Vector3 forward = _camera.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 point = _camera.position + forward * distance;
        point.y = _camera.position.y + _heightOffset;
        return point;
    }
}
