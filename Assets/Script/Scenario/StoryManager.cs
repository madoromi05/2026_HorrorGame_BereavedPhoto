using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [SerializeField] private StoryData[] _storyDatas;
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

    private void Start()
    {
        SetStoryElement(StoryIndex, TextIndex);
        _inputController.OnClickPerformed += AdvanceStory;
    }

    private void OnDisable()
    {
        _inputController.OnClickPerformed -= AdvanceStory;
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
                Debug.Log("全ストーリー終了");
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
