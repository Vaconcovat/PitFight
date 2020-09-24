using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;


public class UI_Deckbuilder : MonoBehaviour
{
    public CardDisplay cursorDisplay;

    Transform cursor;

    public Transform cardViewer, deckViewer;

    public GameObject cardViewerPrefab, deckViewerPrefab;

    public DeckData allCardsDeck;

    public Persistence.DeckListString deckList;

    public TMP_InputField importField, exportField, deckNameField;

    public TextMeshProUGUI deckVerifyText;

    public Toggle orderName, orderCost, orderType, orderCategory;

    // Start is called before the first frame update
    void Start()
    {
        deckViewerPrefab.transform.parent = null;
        cardViewerPrefab.transform.parent = null;
        cursor = cursorDisplay.transform.parent;
        PopulateAllCards();
    }

    // Update is called once per frame
    void Update()
    {
        cursor.transform.position = Input.mousePosition;
    }

    public void ShowCursor(CardDisplay reference)
    {
        cursorDisplay.gameObject.SetActive(true);
        cursorDisplay.card = reference.card;
        cursorDisplay.UpdateDisplay();
    }

    public void HideCursor()
    {
        cursorDisplay.gameObject.SetActive(false);
    }

    public void AddCard(CardDisplay card) {
        //adding card
        deckList.cards.Add(Persistence.instance.GetIDFromCardData(card.card.GetData()));
        PopulateDeckList();
    }

    public void RemoveCard(CardDisplay card) {
        //removing card
        deckList.cards.Remove(Persistence.instance.GetIDFromCardData(card.card.GetData()));
        PopulateDeckList();
        HideCursor();
    }

    public void PopulateAllCards() {

        foreach (Transform t in cardViewer) {
            Destroy(t.gameObject);
        }

        List<CardData> cardOrder = new List<CardData>();
        if (orderName.isOn)
        {
            cardOrder = new List<CardData>(allCardsDeck.cards.OrderBy(x => x.name).ThenBy(x => x.cardType));
        }
        else if (orderType.isOn)
        {
            cardOrder = new List<CardData>(allCardsDeck.cards.OrderBy(x => x.cardType));

        }
        else if (orderCost.isOn)
        {
            cardOrder = new List<CardData>(allCardsDeck.cards.OrderBy(x => x.energyCost).ThenBy(x => x.cardType));

        }
        else if (orderCategory.isOn) {
            cardOrder = new List<CardData>(allCardsDeck.cards.OrderBy(x => x.category).ThenBy(x => x.cardType));

        }

        foreach (CardData data in cardOrder) {
                CardDisplay spawned = Instantiate(cardViewerPrefab, cardViewer).GetComponent<CardDisplay>();
                Card card = new Card(data);
                //card.InitFromData(data);
                spawned.card = card;
                spawned.gameObject.SetActive(true);
                spawned.UpdateDisplay();
            }

        cardViewerPrefab.SetActive(false);
    }

    public void PopulateDeckList() {
        foreach (Transform t in deckViewer) {
            Destroy(t.gameObject);
        }

        foreach (int data in deckList.cards)
        {
            CardDisplay spawned = Instantiate(deckViewerPrefab, deckViewer).GetComponent<CardDisplay>();
            spawned.gameObject.SetActive(true);
            Card card = new Card(Persistence.instance.GetCardDataFromId(data));
            //card.InitFromData(data);
            spawned.card = card;
            spawned.UpdateDisplay();
        }

        deckViewerPrefab.SetActive(false);
    }

    public void VerifyDeckList()
    {
        try
        {
            deckList = Persistence.instance.GetDeckListStringFromDeckCode(importField.text);
            deckVerifyText.text = "Deck: '" + deckList.deckName + "' successfully loaded!";
            PopulateDeckList();
            deckNameField.text = deckList.deckName;
            //verified = true;
        }
        catch
        {
            deckVerifyText.text = "<color=#550000>Deck load failed!";
            deckList = new Persistence.DeckListString();
            deckList.cards = new List<int>();
            //verified = false;
            foreach (Transform t in deckViewer)
            {
                Destroy(t.gameObject);
            }
        }
    }

    public void Export() {
        deckList.deckName = deckNameField.text;

        exportField.text = Persistence.instance.GetDeckCodeFromDeckListString(deckList);
    }

    public void BackToMenu() {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void SearchFilter(string filter) {
        

        //turn off all the cards
        foreach (Transform t in cardViewer) {
            CardDisplay display = t.gameObject.GetComponent<CardDisplay>();

            Card card = display.card;

            bool nameMatch = card.cardName.ToLower().Contains(filter.ToLower());
            bool textMatch = card.GenerateCardText().ToLower().Contains(filter.ToLower());
            bool energyMatch = card.energyCost.ToString().ToLower().Contains(filter.ToLower());
            bool typeMatch = card.cardType.ToString().ToLower().Contains(filter.ToLower());
            bool categoryMatch = card.category.ToString().ToLower().Contains(filter.ToLower());

            t.gameObject.SetActive(nameMatch || textMatch || energyMatch || typeMatch || categoryMatch || filter == "");
        }
    }
}
