using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/Character", order = 1)]
public class CharacterData : ScriptableObject {
    public string characterName = "Character";
    [TextArea]
    public string characterDescription = "";
    public Sprite characterArt;

    public List<CardData> startingCards;

    public int startingHealth = 30;
    public int startingStrength = 0;
    public int startingAgility = 0;
    public int startingStamina = 5;
    public int startingIntelligence = 5;
}
