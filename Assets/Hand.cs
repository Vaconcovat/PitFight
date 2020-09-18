using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : Pile
{


    public override void UpdateCards()
    {
        for (int i = 0; i < cards.Count; ++i)
        {


            cards[i].transform.parent = transform;
            cards[i].desiredPosSpace = Space.Self;

            //if (cards.Count % 2 == 0)
            //{
            //    //even number of cards

            //    cards[i].desiredPosition = (((i - (cards.Count / 2) + 0.5f) * 1.0f) / (cards.Count * 1f)) * cardoffset;
            //}
            //else
            //{
            //    //odd number of cards
            //    cards[i].desiredPosition = (((i - (cards.Count / 2)) * 1.0f) / (cards.Count * 1f)) * cardoffset;

            //}

            cards[i].desiredPosition = (((i * 1.0f) / (cards.Count * 1f)) * cardoffset);


            cards[i].currentPile = this;
            cards[i].SetFacing(faceUp);
        }
    }
}
