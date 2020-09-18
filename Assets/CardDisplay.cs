﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class CardDisplay : MonoBehaviour
{
    public Color fastColour, slowColour, powerColour, suddenColour, unplayableColour;

    public Color baseBackground, queueBackground, burnbackground;

    public Card card;

    public Image background, border;
    public TextMeshProUGUI title;
    public TextMeshProUGUI energyCost;
    public TextMeshProUGUI cardType;
    public TextMeshProUGUI cardText;
    public TextMeshProUGUI cardDelay;
    public Image cardArt;


    public void UpdateDisplay() {
        title.text = card.cardName;


        energyCost.text = card.energyCost.ToString();
        if (card.owner != null) {
            if (card.energyCost > card.owner.energy && card.currentPile == card.owner.hand)
            {
                energyCost.color = Color.red;
            }
            else
            {
                energyCost.color = Color.white;

            }
        }
        

        if (card.cardType == Card.CardType.Slow)
        {
            if (card.slowDelay > 0)
            {
                cardType.text = "Slow + " + card.slowDelay.ToString();
            }
            else
            {
                cardType.text = "Slow";
            }
        }
        else {
            cardType.text = card.cardType.ToString();
        }

        cardText.text = card.GenerateCardText();
        if (card.art != null) {
            cardArt.sprite = card.art;
        }

        switch (card.cardType)
        {
            case Card.CardType.Fast:
                background.color = fastColour;
                break;
            case Card.CardType.Reaction:
                break;
            case Card.CardType.Sudden:
                background.color = suddenColour;
                energyCost.text = "!";

                break;
            case Card.CardType.Power:
                background.color = powerColour;

                break;
            case Card.CardType.Slow:
                background.color = slowColour;

                break;
            case Card.CardType.Unplayable:
                background.color = unplayableColour;
                energyCost.text = "-";
                break;
        }

        if (card.currentDelay > 0)
        {
            cardDelay.gameObject.SetActive(true);
            cardDelay.text = card.currentDelay.ToString();
        }
        else {
            cardDelay.text = "";
            cardDelay.gameObject.SetActive(false);
        }

        if (card.owner != null) {
            if (card.currentPile == card.owner.burnPile)
            {
                border.color = burnbackground;
            }
            else if (card.currentPile == card.owner.queue)
            {
                border.color = queueBackground;
            }
            else
            {
                border.color = baseBackground;
            }
        }
        
    }
}