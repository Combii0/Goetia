using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Goetia/Card")]

public class CardData : ScriptableObject
{
    public string cardID;

    public string cardName;

    [TextArea]
    public string description;

    public string category;

    public Sprite frontSprite;
}