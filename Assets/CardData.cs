using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/CardAsset", order = 1)]
public class CardData : ScriptableObject
{
    
    public Card.CardType cardType;
    
    public int slowDelay = 0;
    public int energyCost;
    public string cardName = "Card";
    [Multiline]
    public string cardText = "";
    public Sprite art;

    public List<Card.CardAttribute> attributes;

    public List<Card.CardKeyword> keywords;

    public List<Card.CardPower> powers;
    
    public Card.DiscardType discardType = Card.DiscardType.Discard;
}
