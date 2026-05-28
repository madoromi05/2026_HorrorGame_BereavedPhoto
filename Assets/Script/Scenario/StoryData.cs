using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sinario", menuName = "StoryData")]
public class StoryData : ScriptableObject
{
    public List<Story> Stories = new List<Story>();
}

[System.Serializable]
public class Story
{
    public Sprite Background;
    public Sprite CharacterImages;
    public string CharacterName;
    [TextArea]
    public string StoryText;
}
