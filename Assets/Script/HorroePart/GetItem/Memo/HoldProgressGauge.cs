using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// インタラクトの長押しを数え、進み具合をゲージ（Image の fillAmount）に映す係
    /// RevealUIPresenterBase が持ち、押す・離す・毎フレームを渡す
    /// 長押しが終わったら Tick が true を返すだけで、閉じる・ページを送るなどは知らない
    /// </summary>
    public class HoldProgressGauge
    {
        private readonly Image _progressImage;

        private float _holdElapsed = 0f;
        private bool _isHolding = false;

        private const float kHoldDuration = 1f;

        public HoldProgressGauge(Image progressImage) => _progressImage = progressImage;

        public void Begin()
        {
            _isHolding = true;
            _holdElapsed = 0f;
            _progressImage.gameObject.SetActive(true);
        }

        public void Cancel()	// 経過を戻し、ゲージを空にして隠す
        {
            _isHolding = false;
            _holdElapsed = 0f;
            HideGauge();
        }

        public void HideGauge()
        {
            _progressImage.fillAmount = 0f;
            _progressImage.gameObject.SetActive(false);
        }

        public bool Tick()	// 長押しが規定秒数に届いたフレームだけ true
        {
            if (!_isHolding) return false;

            _holdElapsed += Time.deltaTime;
            _progressImage.fillAmount = _holdElapsed / kHoldDuration;

            if (_holdElapsed < kHoldDuration) return false;

            // ホールド完了。連続で再発火しないようホールド状態をリセットし、
            // 再度「離す→押す」まで次のアクションを起こさないようにする
            Cancel();
            return true;
        }
    }
}
