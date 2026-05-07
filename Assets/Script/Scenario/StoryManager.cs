using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : MonoBehaviour
{
    [SerializeField] private StoryData[] storyDatas;
    [SerializeField] private Image background;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private InputScenarioController inputController;

    //ストーリーのエレメント配列番号が必要なのでプロパティを
    public int StoryIndex { get; private set; }
    public int TextIndex { get; private set; }

    private const float kTypingInterval = 0.05f;
    private Coroutine _typingCoroutine;
    private bool _isTyping = false;

    //Startで呼び出そう
    private void Start()
    {
        SetStoryElement(StoryIndex, TextIndex);
        inputController.OnClickPerformed += AdvanceStory;
    }

    private void OnDisable()
    {
        inputController.OnClickPerformed -= AdvanceStory;
    }

    // クリック入力を受け取り、タイピング中か否かで処理を決める
    private void OnClickReceived()
    {
        if (_isTyping)
            SkipTyping();
        else
            AdvanceStory();
    }

    // ストーリーを次のテキストへ進める。
    // storyDatas の範囲外アクセスを防ぐため、両インデックスを事前検証する。
    public void AdvanceStory()
    {
        TextIndex++;

        if (TextIndex < storyDatas[StoryIndex].stories.Count)
        {
            SetStoryElement(StoryIndex, TextIndex);
        }
        else
        {
            TextIndex = 0;
            StoryIndex++;

            if (StoryIndex < storyDatas.Length)
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
        //同じ言葉をまとめておくためのvar
        var storyElement = storyDatas[storyIndex].stories[textIndex];
        //どのストーリーデータの、どのバックグランドか
        background.sprite = storyElement.Background;
        //どのストーリーデータの、どのキャラクタか
        characterImage.sprite = storyElement.CharacterImages;
        //どのストーリーデータの、どのテキストか
        storyText.text = storyElement.StoryText;
        //どのストーリーデータの、どのキャラ名か
        characterName.text = storyElement.CharacterName;

        storyText.text = "";
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeSentence(storyElement.StoryText));
    }

    // 1文字ずつ表示する
    private IEnumerator TypeSentence(string sentence)
    {
        _isTyping = true;
        foreach (var letter in sentence)
        {
            storyText.text += letter;
            yield return new WaitForSeconds(kTypingInterval);
        }
        _isTyping = false;
        _typingCoroutine = null;
    }

    // タイピング中にクリックされたとき、残りを即時全表示する
    private void SkipTyping()
    {
        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
        storyText.text = storyDatas[StoryIndex].stories[TextIndex].StoryText;
        _isTyping = false;
    }
}