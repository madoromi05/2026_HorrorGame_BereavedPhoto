using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    /// <summary>全ストーリーが終了したときに発火する。</summary>
    public event Action OnAllStoriesComplete;

    // StorySceneController.Awake() で SetStoryDatas() により必ず設定される。
    // Inspector からの直接設定は不要（[SerializeField] を外している）。
    private StoryData[] _storyDatas;

    [SerializeField] private Image _background;
    [SerializeField] private Image _characterImage;
    [SerializeField] private TextMeshProUGUI _storyText;
    [SerializeField] private TextMeshProUGUI _characterName;
    [SerializeField] private InputScenarioController _inputController;

    // ストーリーのエレメント配列番号が必要なのでプロパティに
    public int StoryIndex { get; private set; }
    public int TextIndex { get; private set; }

    private const float kTypingInterval = 0.05f;
    private Coroutine _typingCoroutine;
    private bool _isTyping = false;

    private void Awake()
    {
        DebugCustom.ValidateFields(this,
            (nameof(_background), _background),
            (nameof(_characterImage), _characterImage),
            (nameof(_storyText), _storyText),
            (nameof(_characterName), _characterName),
            (nameof(_inputController), _inputController));
    }

    private void Start()
    {
        // データは StorySceneController.Awake() で SetStoryDatas() により設定済み。
        // ここでは入力イベントの購読のみ行う。
        _inputController.OnClickPerformed += OnClickReceived;
    }

    /// <summary>ストーリーデータを外部から差し替えて先頭から再生する。</summary>
    public void SetStoryDatas(StoryData[] datas)
    {
        _storyDatas = datas;
        StoryIndex  = 0;
        TextIndex   = 0;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_storyDatas != null && _storyDatas.Length > 0)
            SetStoryElement(0, 0);
    }

    private void OnDisable()
    {
        _inputController.OnClickPerformed -= OnClickReceived;
    }

    private void OnClickReceived()
    {
        if (_isTyping)
            SkipTyping();
        else
            AdvanceStory();
    }

    public void AdvanceStory()
    {
        if (_storyDatas == null || _storyDatas.Length == 0) return;
        TextIndex++;

        if (TextIndex < _storyDatas[StoryIndex].Stories.Count)
        {
            SetStoryElement(StoryIndex, TextIndex);
        }
        else
        {
            TextIndex = 0;
            StoryIndex++;

            if (StoryIndex < _storyDatas.Length)
            {
                SetStoryElement(StoryIndex, TextIndex);
            }
            else
            {
                DebugCustom.Log("全ストーリー終了");
                OnAllStoriesComplete?.Invoke();
            }
        }
    }

    private void SetStoryElement(int storyIndex, int textIndex)
    {
        var storyElement = _storyDatas[storyIndex].Stories[textIndex];
        _background.sprite = storyElement.Background;
        _characterImage.sprite = storyElement.CharacterImages;
        _storyText.text = storyElement.StoryText;
        _characterName.text = storyElement.CharacterName;

        _storyText.text = "";
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeSentence(storyElement.StoryText));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        _isTyping = true;
        foreach (var letter in sentence)
        {
            _storyText.text += letter;
            yield return new WaitForSeconds(kTypingInterval);
        }
        _isTyping = false;
        _typingCoroutine = null;
    }

    private void SkipTyping()
    {
        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
        _storyText.text = _storyDatas[StoryIndex].Stories[TextIndex].StoryText;
        _isTyping = false;
    }
}
