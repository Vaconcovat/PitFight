using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class Player : MonoBehaviour
{


    [BoxGroup("Stats")] public int health = 30;
    [BoxGroup("Stats")] public int block = 0;
    [BoxGroup("Stats")] public int energy = 5;
    [BoxGroup("Stats")] public int strength = 0;
    [BoxGroup("Stats")] public int agility = 0;
    [BoxGroup("Stats")] public int stamina = 5;
    [BoxGroup("Stats")] public int intelligence = 5;






    public string _name = "Player1";

    [BoxGroup("Components")] public Hand hand;
    [BoxGroup("Components")] public DrawPile drawPile;
    [BoxGroup("Components")] public DiscardPile discardPile;
    [BoxGroup("Components")] public BurnPile burnPile;
    [BoxGroup("Components")] public Viewer viewer;
    [BoxGroup("Components")] public Library library;
    [BoxGroup("Components")] public Queue queue;
    [BoxGroup("Components")] public Powers powers;
    [BoxGroup("Components")] public Transform hand_hidePoint, cardResolvePoint;


    public bool canPlay = false;
    public bool human = true;

    public Player enemy;
    public CharacterData character;

    Vector3 handStartPoint;
    // Start is called before the first frame update
    void Start()
    {
        Log.Write("Player " + _name + " Starting with " + health.ToString() + " health.");

        handStartPoint = hand.transform.position;
        //StartDraw(4);
    }

    

    // Update is called once per frame
    void Update()
    {
        if (!human) return;

        if (Input.GetKeyDown(KeyCode.Space)) {
            //StartDraw(1);
            StartCoroutine(StartDrawCoroutine(1));
        }

        

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, 100f, (int)(1 << 8))) {

                //Debug.Log(hit.collider.gameObject);
                if (hit.transform.GetComponentInParent<Card>() != null) {
                    //we've hit a card
                    Card selected = hit.transform.GetComponentInParent<Card>();

                    //if we're currently in a select mode, do that instead
                    if (cardSelectingPile.Count > 0)
                    {
                        Log.Write("Selecting");
                        if (cardSelectingPile.Contains(selected.currentPile))
                        {
                            //valid selection
                            SubmitCard(selected);
                        }
                        else {
                            //invalid selection
                            UIManager.Popup("Invalid card");
                        }
                    }
                    else {
                        //we're not card selecting, resume play
                        if (selected.currentPile == hand)
                        {
                            //if it's in our hand, play it
                            if (GameManager.instance.activePlayer != this)
                            {
                                Log.Write("It's not your turn!");
                                UIManager.Popup("It's not your turn!");
                                return;
                            }

                            if (!canPlay)
                            {
                                Log.Write("Can't play cards right now!");
                                return;
                            }
                            if (selected.cardType == Card.CardType.Sudden)
                            {
                                Log.Write("Can't play sudden cards!");
                                UIManager.Popup("Can't play sudden cards!");
                                return;
                            }
                            selected.Play(Input.GetMouseButtonDown(1));
                        }
                        if (selected.currentPile == viewer && (GameManager.instance.currentPhase == GameManager.GamePhase.Discover || GameManager.instance.currentPhase == GameManager.GamePhase.Setup))
                        {
                            Log.Write("Discovered " + selected.cardName);
                            selected.Discard();
                            FinishDiscover();
                        }
                    }
                    
                }
            }
        }
    }

    public void HideHand(bool hide) {
        if (hide)
        {
            hand.transform.position = hand_hidePoint.position;
        }
        else {
            hand.transform.position = handStartPoint;
        }
        hand.UpdateCards();
    }

    public void StartDraw(int numCards) {
        Log.Write("Drawing " + numCards + " cards...");

        for (int i = 0; i < numCards; ++i) {
            Draw();
        }
    }

    [System.Serializable]
    public struct Interrupts {
        public Card drawAdditionalInterrupt;
        public Card vampireInterrupt;
        public Card infernoInterrupt;
    }
    public Interrupts interrupts;

    public IEnumerator StartDrawCoroutine(int numCards) {
        Log.Write("Drawing " + numCards + " cards...");

        if (interrupts.drawAdditionalInterrupt != null && GameManager.instance.currentPhase == GameManager.GamePhase.Play && numCards > 1) {
            interrupts.drawAdditionalInterrupt.MoveToPile(GameManager.instance.playPile);
            yield return GameManager.instance.fullWait;
            numCards += 1;
            Log.Write("Drawing an additional card");
            interrupts.drawAdditionalInterrupt.MoveToPile(interrupts.drawAdditionalInterrupt.owner.powers);
            
        }
        UIManager.Popup("Draw " + numCards);
        for (int i = 0; i < numCards; ++i)
        {
            yield return GameManager.instance.shortWait;
            yield return StartCoroutine(Draw_Coroutine());
        }
    }

    void Draw() {

        if (drawPile.cards.Count == 0) {
            Log.Write("Draw pile empty, shuffling discard.");

            discardPile.Shuffle();
            discardPile.Transfer(drawPile);
        }

        if (drawPile.cards.Count == 0) {
            //out of cards.
            Overdraw();
            return;
        }

        Card drawn = drawPile.cards[drawPile.cards.Count - 1];
        hand.cards.Add(drawn);
        drawPile.cards.RemoveAt(drawPile.cards.Count - 1);

        hand.UpdateCards();
        drawn.OnDraw(false);
    }

    public void Overdraw() {
        Log.Write("Overdraw.");
        if(human) UIManager.Popup("Overdraw! (1 Damage)");
        TakeDamage(1);
    }

    IEnumerator Draw_Coroutine() {
        //yield return new WaitForSeconds(0.5f);

        if (drawPile.cards.Count == 0)
        {
            Log.Write("Draw pile empty, shuffling discard.");

            discardPile.Shuffle();
            yield return GameManager.instance.shortWait;
            discardPile.SetCardReveal(false);
            discardPile.Transfer(drawPile);
            yield return GameManager.instance.shortWait;


        }

        if (drawPile.cards.Count == 0)
        {
            //out of cards.
            Overdraw();
            yield break;
        }

        Card drawn = drawPile.cards[drawPile.cards.Count - 1];
        hand.cards.Add(drawn);
        drawPile.cards.RemoveAt(drawPile.cards.Count - 1);
        drawPile.UpdateCards();
        hand.UpdateCards();
        drawn.OnDraw();
    }

    public void ResetBlock() {
        Log.Write("Reset block...");
        block = 0;
        if (human)
        {
            UIManager.Popup("0", UIManager.instance.blockText.transform.position, Color.blue);
        }
        else {
            UIManager.Popup("0", UIManager.instance.opponentBlock.transform.position, Color.blue);
        }
            UIManager.UpdateUI();
    }

    

    public IEnumerator ResetCoroutine() {
        Log.Write("Resetting...");
        
        energy = stamina;
        if (human)
        {
            UIManager.Popup(stamina.ToString(), UIManager.instance.energyText.transform.position, Color.cyan);
        }
        else {
            UIManager.Popup(stamina.ToString(), UIManager.instance.opponentEnergy.transform.position, Color.cyan);

        }
        yield return GameManager.instance.shortWait;

        Log.Write("Discarding Hand...");
        //int cardsTodiscard = hand.cards.Count;
        List<Card> cardsInHand = new List<Card>(hand.cards);

        for (int i = cardsInHand.Count - 1; i >= 0; --i) {
            Card c = cardsInHand[i];

            if (c.attributes.FirstOrDefault(x => x.attribute == Card.CardAttribute.Attribute.Fading) != null)
            {
                //we are fading, burn
                Log.Write(c.cardName + " is fading, burning");
                //c.MoveToPile(GameManager.instance.playPile);
                c.SetReveal(true);
                yield return GameManager.instance.medWait;
                c.Burn();
                yield return GameManager.instance.medWait;

            }
            else if (c.attributes.FirstOrDefault(x => x.attribute == Card.CardAttribute.Attribute.Hold) != null)
            {
                //do nothing, hold the card
                Log.Write(c.cardName + " has hold, will not be discarded");
                //c.MoveToPile(GameManager.instance.playPile);
                c.SetReveal(true);
                yield return GameManager.instance.medWait;
                UIManager.Popup("Hold");
                c.MoveToPile(hand);
                yield return GameManager.instance.medWait;

            }
            else {
                //no other attributes, discard
                c.Discard();
            }

            yield return GameManager.instance.shortWait;
        }

        Log.Write("Drawing new hand...");
        yield return StartCoroutine(StartDrawCoroutine(intelligence));

        UIManager.UpdateUI();
    }

    public void Discover() {
        StartCoroutine(StartDiscover());
    }

    public IEnumerator StartDiscover() {
        Log.Write("Starting Discovery...");
        yield return GameManager.instance.medWait;
        if(human) UIManager.instance.discoveryTitle.SetActive(true);

        int cardsInLibrary = library.cards.Count;
        if (cardsInLibrary == 0) {
            UIManager.Popup("Empty Library!", Color.black, UIManager.instance.centrePoint, 100f, 0f);
            if (!human) FinishDiscover();
            yield break;
        } 
        for (int i = 0; i < 3 && i < cardsInLibrary; ++i) {
            library.cards[library.cards.Count - 1].MoveToPile(viewer);
            yield return GameManager.instance.shortWait;
        }
        yield return new WaitUntil(() => viewer.cards.Count == 0); // make sure this coroutine waits
    }

    public void FinishDiscover() {
        if (human) UIManager.instance.skipDiscoverButton.gameObject.SetActive(false);
        UIManager.instance.discoveryTitle.SetActive(false);
        
            StartCoroutine(FinishDiscoverCoroutine());
    }

    IEnumerator FinishDiscoverCoroutine() {
        Log.Write("Finishing Discovery...");
        int cardsinviewer = viewer.cards.Count;

        for (int i = 0; i < cardsinviewer; ++i)
        {
            viewer.cards[0].MoveToPile(library, false);
            yield return GameManager.instance.shortWait;
        }
        if(GameManager.instance.currentPhase == GameManager.GamePhase.Discover) GameManager.instance.ChangePhase(GameManager.GamePhase.Start);
    }

    public IEnumerator PlaySuddens() {
        Log.Write("Checking for sudden cards...");
        List<Card> suddens = new List<Card>();

        foreach (Card c in hand.cards) {
            if (c.cardType == Card.CardType.Sudden) {
                suddens.Add(c);
            }
        }

        if (suddens.Count > 0) {
            Log.Write("Found " + suddens.Count + " Sudden cards to trigger...");

            foreach (Card c in suddens)
            {
                yield return c.StartCoroutine(c.ResolveCoroutine());
            }
        }
        yield return GameManager.instance.medWait;

    }

    public IEnumerator StartQueue() {
        Log.Write("Starting Queue...");
        yield return GameManager.instance.medWait;
        List<Card> cardsToresolve = new List<Card>(queue.cards);
        for (int i = 0; i < cardsToresolve.Count; ++i)
        {
            Card c = cardsToresolve[i];
            if (c.currentDelay > 0)
            {
                //c.MoveToPile(GameManager.instance.playPile);
                yield return GameManager.instance.shortWait;
                c.currentDelay -= 1;
                c.UpdateDisplay();
                yield return GameManager.instance.medWait;
                //c.MoveToPile(c.owner.queue);
            }
            else {
                yield return c.StartCoroutine(c.ResolveCoroutine());
            }
        }
        Log.Write("Queue Finished...");
    }

    public void Attack(int value) {
        //deal damage
        Log.Write(_name + " is attacking for (" + value.ToString() + " + " + strength.ToString() + " = " + (value + strength).ToString() + ")");
        DealDamage(value + strength);
        UIManager.Popup("Attack " + value.ToString() + ((strength != 0) ? ("+" + strength.ToString()) : ("")));
    }

    public void DealDamage(int value) {
        if (value <= 0)
        {
            Log.Write("Damage is 0 or lower");
        }
        else {
            enemy.TakeDamage(value);

            Log.Write(_name + " has dealt " + value + " damage!");
        }
    }

    public void TakeDamage(int value) {
        if (block == 0) {
            LoseHealth(value);
        }
        else if (block >= value)
        {
            block -= value;
            if (human){
                UIManager.Popup("-" + value, UIManager.instance.blockText.transform.position, Color.blue, 80f, -40f, 5f);
                UIManager.Popup("Blocked!", UIManager.instance.blockText.transform.position, Color.yellow, 60f, 50f, 5f);
            }
            else
            {
                UIManager.Popup("-" + value, UIManager.instance.opponentBlock.transform.position, Color.blue, 80f, -40f, 5f);
                UIManager.Popup("Blocked!", UIManager.instance.opponentBlock.transform.position, Color.yellow, 60f, 50f, 5f);

            }
        }
        else {
            int damageIn = value - block;
            if (human)
            { 
                UIManager.Popup("-" + block, UIManager.instance.blockText.transform.position, Color.blue, 100f, -40f, 5f);
            }
            else
            {
                UIManager.Popup("-" + block, UIManager.instance.opponentBlock.transform.position, Color.blue, 100f, -40f, 5f);
            }
            block = 0;
            LoseHealth(damageIn);
        }
    }

    public void LoseHealth(int value) {
        if (interrupts.vampireInterrupt != null && value > 0) {
            enemy.Heal(1);
        }

        int previousHealth = health;
        health -= value;
        Log.Write(_name + " has lost " + value + " health. (" + previousHealth + " -> " + health + " )");
        if (human){
            UIManager.Popup("-" + value, UIManager.instance.healthText.transform.position, Color.red, 100f, -60f, 5f);
        }
        else
        {
            UIManager.Popup("-" + value, UIManager.instance.opponentHealth.transform.position, Color.red, 100f, -60f, 5f);
        }
        UIManager.UpdateUI();

        if (health <= 0) {
            if (human)
            {
                UIManager.Popup("Defeat", Color.red, UIManager.instance.centrePoint, 200f, 0f);
                GameManager.instance.EndGame();
            }
            else {
                UIManager.Popup("Victory", Color.green, UIManager.instance.centrePoint, 200f, 0f);
                GameManager.instance.EndGame();

            }
        }

    }

    public void Block(int value) {
        Log.Write(_name + " is gaining ( " + value + " + " + agility + " = " + (value + agility).ToString() + " ) block");
        UIManager.Popup("Block " + value.ToString() + ((agility != 0)?("+" + agility.ToString()):("")));

        GainBlock(value + agility);
    }

    public void GainBlock(int value) {
        block += value;
        if (human){
            UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.blockText.transform.position, Color.blue);
        }
        else
        {
            UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.opponentBlock.transform.position, Color.blue);
        }
        UIManager.UpdateUI();
    }

    public void GainStrength(int value) {
        int previousStat = strength;
        strength += value;
        if (human) UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.strText.transform.position, Color.blue);

        UIManager.UpdateUI();
        Log.Write(_name + " Strength: " + previousStat + " -> " + strength);
    }

    public void GainAgility(int value)
    {
        int previousStat = agility;
        agility += value;
        if (human) UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.agiText.transform.position, Color.blue);


        UIManager.UpdateUI();
        Log.Write(_name + " Agility: " + previousStat + " -> " + agility);
    }

    public void GainStamina(int value)
    {
        int previousStat = stamina;
        stamina += value;
        if (human) UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.staText.transform.position, Color.blue);


        UIManager.UpdateUI();
        Log.Write(_name + " Stamina: " + previousStat + " -> " + stamina);
    }

    public void GainIntelligence(int value)
    {
        int previousStat = intelligence;
        intelligence += value;
        if (human) UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.intText.transform.position, Color.blue);

        UIManager.UpdateUI();
        Log.Write(_name + " Intelligence: " + previousStat + " -> " + intelligence);
    }

    public void GainEnergy(int value) {
        energy += value;
        if (human){
            UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.energyText.transform.position, Color.cyan);
        }
        else
        {
            UIManager.Popup(((value > 0) ? ("+") : ("")) + value, UIManager.instance.opponentEnergy.transform.position, Color.cyan);
        }
        UIManager.UpdateUI();
        Log.Write(_name + " has gained  " + value + " energy");
    }

    public void Heal(int value) {
        int previousHealth = health;
        health += value;
        Log.Write(_name + " has healed " + value + " health. (" + previousHealth + " -> " + health + " )");
        if (human)
        {
            UIManager.Popup("+" + value, UIManager.instance.healthText.transform.position, Color.green);
        }
        else {
            UIManager.Popup("+" + value, UIManager.instance.opponentHealth.transform.position, Color.green);
        }
        UIManager.UpdateUI();
    }

    //public void Stun(int value, Player target)
    //{
        
    //    StartCoroutine(StunCoroutine(value, target));
    //}

    public IEnumerator StunCoroutine(int value, Player target) {
        Log.Write(_name + " is stunning " + target._name + " for " + value);
        UIManager.Popup("Stun " + value);
        for (int i = 0; i < value; ++i) {
            yield return StartCoroutine(SpawnCard(GameManager.instance.stunnedCard, target, target.discardPile));
        }

    }

    public IEnumerator SpawnCard(CardData data, Player target, Pile destination)
    {
        Log.Write(_name + "has created card " + data.cardName);
        Card card = GameManager.instance.SpawnCard(data, target);
        card.transform.position = GameManager.instance.playPile.transform.position;
        card.MoveToPile(GameManager.instance.playPile);
        card.SetFacing(true);
        card.SetReveal(true);
        if (interrupts.infernoInterrupt != null && card.cardName == GameManager.instance.fireCard.cardName) {
            interrupts.infernoInterrupt.MoveToPile(GameManager.instance.playPile);
            yield return StartCoroutine(StartChoice("Decline", "Pay 2", "Pay 2 energy to give this card Clone 1", true, energy >= 2));
            int result = GetChoiceResult();
            if (result == 0)
            {
                //cancelled, do nothing?
            }
            else if (result == 1)
            {
                //declined, do nothing
            }
            else {
                //accepted
                energy -= 2;
                card.keywords.Add(new Card.CardKeyword { keyword = Card.CardKeyword.Keyword.Clone, value = 1 });
                card.UpdateDisplay();
            }
            interrupts.infernoInterrupt.MoveToPile(powers);
        }

        yield return GameManager.instance.medWait;

        card.MoveToPile(destination);
        

    }

    public IEnumerator StartScry(int numberOfCards)
    {
        Log.Write("Starting Scry...");
        //show the scry title
        //UIManager.instance.ShowPersistentText("Scry " + numberOfCards + ": Discard any number of these cards from your draw pile", "Done");

        //put the top x cards of your draw pile into your viewer
        for (int i = 0; i < numberOfCards; ++i)
        {
            //make sure there's a card
            if (drawPile.cards.Count > 0)
            {
                //put it in the viewer
                drawPile.cards[drawPile.cards.Count - 1].MoveToPile(viewer);
                
            }


            
        }

        Log.Write("starting card selection " + choice_result + " | " + viewer.cards.Count);

        while (GetChoiceResult(false) != 0 && viewer.cards.Count != 0)
        {
            Log.Write("waiting for done result or all cards discarded: " + choice_result + " | " + viewer.cards.Count);
            yield return StartCardSelect("Discard cards from your draw pile (shown top -> bottom)", viewer, "Done", false);
            if (selectedCard != null)
            {
                selectedCard.MoveToPile(discardPile);
                selectedCard = null;
            }
        }
        Log.Write("Scry Finished " + choice_result + " | " + viewer.cards.Count);

        cardSelectingPile = new List<Pile>();
        int numCards = viewer.cards.Count;
        for (int i = numCards - 1; i >= 0; i--) {
            Card c = viewer.cards[i];
            c.SetReveal(true);
            c.MoveToPile(drawPile, true);
            yield return GameManager.instance.shortWait;
        }
        choice_result = -1;
    }

    int choice_result = -1;

    public int GetChoiceResult(bool set = true) {
        int result = choice_result;
        if(set) choice_result = -1;
        return result;
    }

    public IEnumerator StartChoice(string choice1, string choice2, string choiceText, bool valid1 = true, bool valid2 = true) {
        Log.Write("Choice: " + choiceText + " (" + choice1 + ") (" + choice2 + ")");
        canPlay = false;
        UIManager.instance.StartChoice(choice1, choice2, choiceText, valid1, valid2);

        //wait until choice complete
        yield return new WaitUntil(() => choice_result != -1);
        

        //choice complete
        Log.Write("Choice Complete, result: " + choice_result);
    }

    public void SubmitChoice(int result) {
        choice_result = result;

    }


    public Card selectedCard = null;
    //bool cardSelecting = false;
    public List<Pile> cardSelectingPile = new List<Pile>();

    public IEnumerator StartCardSelect(string selectText, Pile validPile, string button = "", bool resetChoice = true) {
        Log.Write("Starting card select from " + validPile.name + ": " + selectText);
        canPlay = false;
        cardSelectingPile = new List<Pile>() {validPile };
        //check if there are valid cards
        if (validPile.cards.Count == 0) {
            selectedCard = null;
            cardSelectingPile = new List<Pile>();
            UIManager.Popup("No valid cards");
            if(resetChoice)choice_result = -1;
            yield break;
        }

        if (human)
        {
            UIManager.instance.ShowPersistentText(selectText, button);

            //do a better ui here
            UIManager.Popup(selectText);

            yield return new WaitUntil(() => (selectedCard != null || choice_result == 0));
            UIManager.instance.HidePersistentText();
            if (resetChoice) choice_result = -1;
        }
        else {
            SubmitCard(validPile.cards[0]);
        }
        
        //UIManager.Popup("Bounce " + selectedCard.cardName);

    }

    public IEnumerator StartCardSelect(string selectText, List<Pile> validPiles, string button = "", bool resetChoice = true)
    {
        Log.Write("Starting card select from multiple piles: " + selectText);
        canPlay = false;
        cardSelectingPile = new List<Pile>(validPiles);
        //check if there are valid cards
        if (validPiles.All(x => x.cards.Count == 0))
        {
            selectedCard = null;
            cardSelectingPile = new List<Pile>();
            UIManager.Popup("No valid cards");
            if (resetChoice) choice_result = -1;
            yield break;
        }

        if (human)
        {
            UIManager.instance.ShowPersistentText(selectText, button);

            //do a better ui here
            UIManager.Popup(selectText);

            yield return new WaitUntil(() => (selectedCard != null || choice_result == 0));
            UIManager.instance.HidePersistentText();
            if (resetChoice) choice_result = -1;
        }
        else
        {
            SubmitCard(validPiles[0].cards[0]);
        }

        //UIManager.Popup("Bounce " + selectedCard.cardName);

    }

    public void SubmitCard(Card result) {
        Log.Write("Submit card: " + result.cardName);
        selectedCard = result;
        cardSelectingPile = new List<Pile>();
    }
}
