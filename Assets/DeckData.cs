using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DeckData", menuName = "Cards/Deck", order = 1)]
public class DeckData : ScriptableObject {
    public string deckName = "New Deck";

    public List<CardData> cards;
}
