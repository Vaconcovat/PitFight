using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class Pile : MonoBehaviour
{
    public List<Card> cards;

    public bool inOrder = true;
    public bool faceUp = false;

    public Vector3 cardoffset;

    public float scaleReductionFactor = 0f;
    public int scaleReductionThreshold = 3;

    public void Draw() {

    }

    public void Shuffle() {
        Log.Write("This pile is shuffling - " + name);

        cards.Shuffle();
        UpdateCards();
    }

    public void Transfer(Pile target) {
        Log.Write("Transfer cards from " + name + " to " + target.name);

        target.cards.AddRange(cards);
        cards.Clear();

        UpdateCards();
        target.UpdateCards();
    }

    public void Hide(bool hide) {
        if (hide)
        {
            transform.position = new Vector3(0, 0, -1000);
        }
        else {
            transform.position = startPos;
        }
    }

    [Button]
    public virtual void UpdateCards()
    {
        for (int i = 0; i < cards.Count; ++i)
        {
            cards[i].transform.parent = transform;
            cards[i].transform.localRotation = Quaternion.identity;
            cards[i].transform.rotation = transform.rotation;
            cards[i].desiredPosSpace = Space.Self;
            cards[i].desiredPosition = i * (cardoffset * ((cards.Count > scaleReductionThreshold)?(1 - (scaleReductionFactor * (cards.Count - scaleReductionThreshold))):1));
            cards[i].currentPile = this;
            cards[i].SetFacing(faceUp);

            if (scaleReductionFactor > 0) {

                if(cards.Count > scaleReductionThreshold) cards[i].transform.localScale = Vector3.one * (1 - (scaleReductionFactor * (cards.Count - scaleReductionThreshold)));
            }
            else {
                cards[i].transform.localScale = Vector3.one;
            }

            cards[i].UpdateDisplay();
        }

        UIManager.UpdateUI();
    }

    Vector3 startPos;
    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCardReveal(bool reveal) {
        for (int i = 0; i < cards.Count; ++i)
        {
            cards[i].SetReveal(reveal);
        }
    }
}

public static class IListExtensions
{
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}

