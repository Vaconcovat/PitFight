using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Persistence : MonoBehaviour
{
    public static Persistence instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            Destroy(this);
        }
    }

    public DeckListString playerDeck, opponentDeck;
    public CharacterData playerCharacter, opponentCharacter;
    public int startingHealthOverride;
    public bool skipMulligan, skipPreDiscover, randomGeneratedDeck;

    public List<CardData> cards;

    [System.Serializable]
    public class DeckListString {
        public string deckName = "Cool Deck";
        public List<int> cards;
    }

    public DeckData GetDeckDataFromDeckListString(DeckListString deckListString) {
        DeckData result = new DeckData();

        result.deckName = deckListString.deckName;
        result.cards = new List<CardData>();
        foreach (int i in deckListString.cards) {
            result.cards.Add(GetCardDataFromId(i));
        }

        return result;
    }

    public DeckListString GetDeckListStringFromDeckData(DeckData data) {
        DeckListString result = new DeckListString();
        result.deckName = data.deckName;
        result.cards = new List<int>();
        foreach (CardData cardData in data.cards) {
            result.cards.Add(cards.FindIndex(x => x == cardData));
        }

        return result;
    }

    public string GetDeckCodeFromDeckListString(DeckListString deckListString) {
        string json = JsonUtility.ToJson(deckListString);

        byte[] bytesToencode = Encoding.UTF8.GetBytes(json);
        string encoded = System.Convert.ToBase64String(bytesToencode);

        return encoded;
        
    }

    public DeckListString GetDeckListStringFromDeckCode(string code) {
        byte[] decodedBytes = System.Convert.FromBase64String(code);
        string decoded = Encoding.UTF8.GetString(decodedBytes);

        DeckListString deckListString = JsonUtility.FromJson<DeckListString>(decoded);
        return deckListString;
    }

    public CardData GetCardDataFromId(int id) {
        return cards[id];
    }

    public int GetIDFromCardData(CardData data) {
        return cards.FindIndex(x => x.cardName == data.cardName);
    }

#if UNITY_EDITOR
    [Button]
    public void RegisterAllCards() {
        string[] GUIDS = AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Cards" });

        List<CardData> _cards = new List<CardData>();

        foreach (string guid in GUIDS) {
            _cards.Add((CardData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(CardData)));
        }

        foreach (CardData data in _cards) {
            if (!cards.Contains(data)) {
                cards.Add(data);
                Debug.Log("registered new card: " + data.cardName);
            }
        }
    }

    public DeckData loadPlayerDeck, loadOpponentDeck;

    [Button]
    public void LoadDecks() {
        playerDeck = GetDeckListStringFromDeckData(loadPlayerDeck);
        opponentDeck = GetDeckListStringFromDeckData(loadOpponentDeck);
    }

#endif
}
