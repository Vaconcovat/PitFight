using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_MainMenu : MonoBehaviour
{
    public Toggle preDiscovery, mulligan, randomDeck;

    public Button startGameButton;

    public UI_PlayerSetup playerSetup, opponentSetup;

    public TMP_InputField healthInput;

    public CardDisplay cursorDisplay;

    Transform cursor;

    void Start() {
        cursor = cursorDisplay.transform.parent;
    }

    public void Update() {
        startGameButton.interactable = ((playerSetup.verified && opponentSetup.verified) || randomDeck.isOn);
        cursor.transform.position = Input.mousePosition;

    }

    public void StartGame() {
        Persistence.instance.skipPreDiscover = !preDiscovery.isOn;
        Persistence.instance.skipMulligan = !mulligan.isOn;

        Persistence.instance.playerCharacter = playerSetup.characters[playerSetup.currentCharacter];
        Persistence.instance.opponentCharacter = opponentSetup.characters[opponentSetup.currentCharacter];

        if (randomDeck.isOn)
        {
            Persistence.instance.randomGeneratedDeck = true;
        }
        else {
            Persistence.instance.playerDeck = Persistence.instance.GetDeckListStringFromDeckCode(playerSetup.deckCodeInput.text);
            Persistence.instance.opponentDeck = Persistence.instance.GetDeckListStringFromDeckCode(opponentSetup.deckCodeInput.text);
        }

        Persistence.instance.startingHealthOverride = int.Parse(healthInput.text);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Board");
    }

    public void ShowCursor(CardDisplay reference) {
        cursorDisplay.gameObject.SetActive(true);
        cursorDisplay.card = reference.card;
        cursorDisplay.UpdateDisplay();
    }

    public void HideCursor() {
        cursorDisplay.gameObject.SetActive(false);
    }

    public void GoToDeckbuilder() {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Deckbuilder");

    }
}
