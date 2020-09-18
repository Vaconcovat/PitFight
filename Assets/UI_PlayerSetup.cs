using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_PlayerSetup : MonoBehaviour
{
    public List<DeckData> presetDecks;
    public TMP_InputField deckCodeInput;
    public TextMeshProUGUI verifyResult;
    public bool verified = false;

    public List<CharacterData> characters;
    public int currentCharacter = 0;

    public TextMeshProUGUI characterName, characterdescription;
    public Image characterImage;

    public Button previousButton, nextButton;

    public List<CardDisplay> cardDisplays;

    void Start() {
        UpdateCharacterDisplay();
    }

    public void Next() {
        currentCharacter++;
        UpdateCharacterDisplay();
    }

    public void Previous() {
        currentCharacter--;
        UpdateCharacterDisplay();
    }

    public void UpdateCharacterDisplay() {
        CharacterData character = characters[currentCharacter];

        characterName.text = character.characterName;
        characterdescription.text = character.characterDescription;
        characterImage.sprite = character.characterArt;

        //turn off all the displays
        foreach (CardDisplay display in cardDisplays) {
            display.gameObject.SetActive(false);
        }

        //turn on each display
        for (int i = 0; i < character.startingCards.Count; ++i) {
            cardDisplays[i].gameObject.SetActive(true);
            Card card = new Card();
            card.InitFromData(character.startingCards[i]);
            cardDisplays[i].card = card;
            cardDisplays[i].UpdateDisplay();
        }

        previousButton.interactable = (currentCharacter > 0);
        nextButton.interactable = (currentCharacter < characters.Count - 1);
    }

    public void SetDeckListFromPreset(int preset) {
        deckCodeInput.SetTextWithoutNotify(Persistence.instance.GetDeckCodeFromDeckListString(Persistence.instance.GetDeckListStringFromDeckData(presetDecks[preset])));
        VerifyDeckList();
    }

    public void DeckCodeChanged() {
        verified = false;
        verifyResult.text = "Deck code not yet verified";

    }

    public void VerifyDeckList() {
        try
        {
            string loadedDeckName = Persistence.instance.GetDeckListStringFromDeckCode(deckCodeInput.text).deckName;
            verifyResult.text = "Deck: '" + loadedDeckName + "' successfully loaded!";
            verified = true;
        }
        catch {
            verifyResult.text = "Deck load failed!";
            verified = false;

        }
    }
}
