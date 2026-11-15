using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// Image のマテリアルの _Radius を 0 から広げ、円形に中身を見せていくアニメーションの係
    /// RevealUIPresenterBase が持ち、開くたびに Restart、破棄のときに ReleaseMaterial を呼ぶ
    /// コルーチンは持ち主の MonoBehaviour の上で動かす。パネルの表示や入力の切り替えは知らない
    /// </summary>
    public class CircleRevealAnimator
    {
        private readonly MonoBehaviour _host;
        private readonly Image _revealImage;

        private Coroutine _revealCoroutine;
        private Material _revealMaterialInstance;		// アセットを書き換えないための複製。初めて開くときに作る

        private const float kRevealDuration = 0.6f;
        private const float kRevealMaxRadius = 1.5f;	// UV の対角（0.707）を超えて、画面の端まで確実に見せる
        private readonly int kShaderRadius = Shader.PropertyToID("_Radius");

        public CircleRevealAnimator(MonoBehaviour host, Image revealImage)
        {
            _host = host;
            _revealImage = revealImage;
        }

        public void Restart()	// 再生中に開き直したら、止めて 0 からやり直す
        {
            if (_revealCoroutine != null)
                _host.StopCoroutine(_revealCoroutine);
            _revealCoroutine = _host.StartCoroutine(PlayRevealAnimation());
        }

        public void ReleaseMaterial()	// 持ち主の OnDestroy から呼ぶ
        {
            if (_revealMaterialInstance != null) Object.Destroy(_revealMaterialInstance);
        }

        /// <summary>
        /// _Radiusを0から1.5にアニメーションして円形リビールを再生する
        /// 1.5まで広げるのはUV対角距離（最大0.5→0.707）を超えて画面端まで確実に表示するため
        /// </summary>
        private IEnumerator PlayRevealAnimation()
        {
            if (_revealMaterialInstance == null)
            {
                _revealMaterialInstance = new Material(_revealImage.material);
                _revealImage.material = _revealMaterialInstance;
            }
            _revealMaterialInstance.SetFloat(kShaderRadius, 0f);

            yield return Tween.Run(kRevealDuration, t =>
                _revealMaterialInstance.SetFloat(kShaderRadius, Mathf.Lerp(0f, kRevealMaxRadius, Tween.EaseOutCubic(t))));
            _revealCoroutine = null;
        }
    }
}
