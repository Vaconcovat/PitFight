using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Test_DeckListTest : MonoBehaviour
{
    public DeckData testDeck;

    public Persistence.DeckListString testDeckList;

    public Persistence.DeckListString decodedDeckList;


    public TMP_InputField deckcodeOut, deckcodeIn;

    public void GetDeckCode() {
        testDeckList = Persistence.instance.GetDeckListStringFromDeckData(testDeck);
        deckcodeOut.text = Persistence.instance.GetDeckCodeFromDeckListString(Persistence.instance.GetDeckListStringFromDeckData(testDeck));
    }

    public void SetDeckCode() {
        decodedDeckList = Persistence.instance.GetDeckListStringFromDeckCode(deckcodeIn.text);
    }
}
