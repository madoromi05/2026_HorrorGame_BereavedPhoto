using UnityEngine.UI;

/// <summary>
/// マウス感度のスライダーを、設定（GameSettings）へつなぐ係
/// OptionMenuController が初期化のときに作り、Bind・Refresh を呼ぶ
/// 感度をカメラや移動へ伝えるのは GameSettings のイベント。ここは書き込むだけ
/// </summary>
public class SensitivitySlider
{
    private readonly Slider _slider;

    public SensitivitySlider(Slider slider) => _slider = slider;

    public void Bind()
    {
        if (_slider != null)
            _slider.maxValue = GameSettings.kMaxSensitivity;	// 上限は GameSettings の 1 か所で決める

        _slider?.onValueChanged.AddListener(OnChanged);
    }

    public void Refresh() => _slider?.SetValueWithoutNotify(GameSettings.Sensitivity);	// 保存値をスライダーへ映す

    private static void OnChanged(float v) => GameSettings.Sensitivity = v;
}
