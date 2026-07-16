using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button の SpriteSwapを使ってマウスオーバー時のスプライトを設定するためのコンポーネント。
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class ButtonMouseOver : MonoBehaviour
{
    [SerializeField] private Sprite _highlightedSprite;

    private void Awake()
    {
        var button = GetComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;

        var spriteState = new SpriteState
        {
            highlightedSprite = _highlightedSprite,
        };
        button.spriteState = spriteState;
    }
}